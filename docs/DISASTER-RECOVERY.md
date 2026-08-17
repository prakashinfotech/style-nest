# Disaster Recovery Runbook — StyleNest Fashion

<!-- ENH-INFRA-005 | TSD §11 | NFR-AVAIL-002 (RTO ≤ 1h) | NFR-AVAIL-003 (RPO ≤ 15min) -->

## SLA Targets

| Metric | Target | Implementation |
|--------|--------|----------------|
| **RTO** (Recovery Time Objective) | ≤ 1 hour | Forced SQL failover < 30 min; ACA image already running in DR |
| **RPO** (Recovery Point Objective) | ≤ 15 min | Active Geo-Replication typical lag < 5 s; worst-case at forced failover |
| Availability | 99.9% | Zone-redundant primary + geo-redundant secondary |

---

## Architecture Overview

```
PRIMARY (East US)                       DR (Central India)
─────────────────                       ─────────────────
Azure Container Apps                    Azure Container Apps (warm standby)
  └─ 9 micro-services                    └─ 9 micro-services (same images)

SQL Server (Business Critical)  ──────► SQL Server DR (geo-replica)
  └─ StyleNestDb (Zone-Redundant)  async   └─ StyleNestDb (readable secondary)
       ZRS backups (35-day PITR)

Azure Cache for Redis (Premium)         Azure Cache for Redis DR (passive)
Azure Service Bus (Geo DR paired)       (auto-failover via alias)
Azure Key Vault (Premium)               Replicated secrets via export policy
```

---

## Disaster Scenarios

| Scenario | Primary Action | RTO Estimate |
|----------|---------------|--------------|
| Single AZ failure | Zone-redundant replicas auto-heal | 0 (automatic) |
| Full primary region outage | Execute DR Failover Runbook | ~45 min |
| SQL Server data corruption | PITR restore from backup | ~30–60 min |
| Accidental data deletion | PITR restore from backup | ~30–60 min |
| Key Vault unavailable | Fallback to PEM key in config | Immediate |

---

## DR Failover Runbook

### Prerequisites

- Azure CLI ≥ 2.50 installed and authenticated (`az login`)
- Contributor access on `rg-stylenest-production` and `rg-stylenest-production-dr`
- Access to `kv-stylenest-<suffix>` secrets for connection strings
- PagerDuty / Teams incident channel open

### Step 1 — Declare a Disaster (~ 5 min)

1. Confirm the primary region is unavailable via [Azure Status](https://status.azure.com).
2. Open a P1 incident in PagerDuty / Teams: `TATASTYLENEST-DR-<date>`.
3. Notify on-call DBA, Platform Lead, and Product Owner.
4. **Do NOT proceed to Step 2 until the Incident Commander approves failover.**

---

### Step 2 — Initiate SQL Geo-Failover (~ 20 min)

> ⚠️ This is a destructive operation. The primary will be demoted.
> Any transactions committed after the last sync point will be lost.

```bash
# Variables — adjust suffix to match your deployment
DR_RESOURCE_GROUP="rg-stylenest-production-dr"
DR_SQL_SERVER="sql-stylenest-<suffix>-dr"
DB_NAME="StyleNestDb"

# Verify secondary is reachable and replication lag
az sql db show \
  --resource-group $DR_RESOURCE_GROUP \
  --server $DR_SQL_SERVER \
  --name $DB_NAME \
  --query "{replicationRole:replicationLinks[0].role, replicationLag:replicationLinks[0].replicationLag}"

# Promote secondary to primary (forced failover — may lose last few seconds of data)
az sql db replica set-primary \
  --resource-group $DR_RESOURCE_GROUP \
  --server $DR_SQL_SERVER \
  --name $DB_NAME
```

Expected output: `"replicationRole": "Primary"` after ~5 minutes.

---

### Step 3 — Redirect Application Traffic (~ 10 min)

#### Option A — Azure Front Door / Traffic Manager (recommended)

```bash
# Flip Traffic Manager endpoint weights to route 100% to DR
az network traffic-manager endpoint update \
  --resource-group rg-stylenest-production \
  --profile-name tm-stylenest \
  --name primary-endpoint \
  --type azureEndpoints \
  --weight 0

az network traffic-manager endpoint update \
  --resource-group $DR_RESOURCE_GROUP \
  --profile-name tm-stylenest \
  --name dr-endpoint \
  --type azureEndpoints \
  --weight 100
```

#### Option B — DNS TTL update (fallback)

1. Log in to the DNS provider (Azure DNS / Route 53 / Cloudflare).
2. Update the `A` record for `api.stylenest.com` to point to the DR Container Apps FQDN.
3. Set TTL to 60 seconds for fast propagation.

---

### Step 4 — Update Connection Strings in DR Key Vault (~ 5 min)

```bash
DR_KV="kv-stylenest-<suffix>-dr"    # adjust suffix

# Point all services to the new (now-primary) SQL server
az keyvault secret set \
  --vault-name $DR_KV \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Server=$DR_SQL_SERVER.database.windows.net,1433;Database=$DB_NAME;Authentication=Active Directory Managed Identity;Encrypt=True;"

# Restart all Container Apps to pick up new secrets
for app in auth catalog order cart user seller admin gateway media; do
  az containerapp restart \
    --resource-group $DR_RESOURCE_GROUP \
    --name "app-stylenest-$app"
done
```

---

### Step 5 — Validate DR Environment (~ 10 min)

```bash
# Health check all services
BASE_URL="https://api-dr.stylenest.com"     # DR FQDN

declare -a SERVICES=("auth" "catalog" "order" "cart" "user")
for svc in "${SERVICES[@]}"; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/$svc/health")
  echo "$svc health: $STATUS"
done

# Smoke test: list products
curl -s "$BASE_URL/catalog/api/v1/products?pageSize=5" | jq '.totalCount'

# Smoke test: login
curl -s -X POST "$BASE_URL/auth/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@stylenest.com","password":"Admin@123456"}' | jq '.token'
```

All services should return HTTP 200. If any fail, check Container Apps logs:

```bash
az containerapp logs show \
  --resource-group $DR_RESOURCE_GROUP \
  --name app-stylenest-<service> \
  --tail 100
```

---

### Step 6 — Communicate Status (~ 5 min)

1. Post status update on `status.stylenest.com` (StatusPage).
2. Send internal incident update: `DR failover complete — traffic on Central India`.
3. Update PagerDuty incident with evidence (health check output).

---

### Step 7 — Post-Failover Monitoring (0–24 h)

- Monitor Application Insights for error rate spikes.
- Watch Redis hit rate; allow 10 min for cache warm-up after restart.
- Set alert on p95 latency > 500 ms (normal post-failover while JIT warms up).
- Confirm RPO: check `az sql db show ... --query replicationLinks[0].replicationLag` — should be < 5 s.

---

## Failback Runbook (after primary region recovery)

### Step 1 — Verify primary region is healthy

```bash
az resource list --resource-group rg-stylenest-production --query "[].{name:name, type:type}"
```

### Step 2 — Re-establish geo-replication

```bash
# Re-deploy sql-geo-replication Bicep module to rebuild the secondary
az deployment group create \
  --resource-group rg-stylenest-production \
  --template-file infra/bicep/modules/sql-geo-replication.bicep \
  --parameters primarySqlServerName=sql-stylenest-<suffix> \
               secondarySqlServerName=sql-stylenest-<suffix>-dr \
               drLocation=centralindia \
               adminLogin=stylenestadmin \
               adminPassword=<secret>
```

> Wait 15–30 minutes for full sync before proceeding.

### Step 3 — Failback SQL to primary

```bash
PRIMARY_RESOURCE_GROUP="rg-stylenest-production"
PRIMARY_SQL_SERVER="sql-stylenest-<suffix>"

az sql db replica set-primary \
  --resource-group $PRIMARY_RESOURCE_GROUP \
  --server $PRIMARY_SQL_SERVER \
  --name StyleNestDb
```

### Step 4 — Redirect traffic back to primary

Reverse the Traffic Manager endpoint weights from Step 3 of the failover runbook.

### Step 5 — Update connection strings back to primary

Mirror Step 4 of the failover runbook, targeting the primary Key Vault.

---

## Point-in-Time Restore (Data Corruption / Accidental Deletion)

SQL BusinessCritical tier retains automated backups for **35 days** with PITR.

```bash
# Find the latest clean backup timestamp from Application Insights / audit logs
RESTORE_TIME="2026-05-27T10:00:00Z"    # adjust to before the corruption event

az sql db restore \
  --resource-group rg-stylenest-production \
  --server sql-stylenest-<suffix> \
  --name StyleNestDb \
  --dest-name StyleNestDb-Restored \
  --time "$RESTORE_TIME"

# Validate data in the restored DB, then rename
# 1. Take StyleNestDb offline (set to single-user)
# 2. Rename StyleNestDb → StyleNestDb-Corrupt
# 3. Rename StyleNestDb-Restored → StyleNestDb
# Or use database copy + swap app connection strings
```

> ⚠️ Always restore to a **different database name** first. Validate before cutting over.

---

## Quarterly DR Drill Checklist

Perform the following drill in the **staging** environment every quarter.

| # | Task | Owner | Pass Criteria |
|---|------|-------|---------------|
| 1 | Trigger geo-failover on `StyleNestDb-staging` | Platform Lead | Failover completes < 30 min |
| 2 | All health checks pass on DR Container Apps | DevOps | HTTP 200 on all 5 services |
| 3 | End-to-end smoke test: login → add to cart → checkout | QA | No errors, order confirmed |
| 4 | Measure actual RPO (check replication lag before failover) | DBA | < 15 min |
| 5 | Measure actual RTO (time from `az sql db replica set-primary` to all health checks green) | DevOps | < 1 hour |
| 6 | Failback to primary and confirm geo-replication re-established | DBA | Secondary in sync < 30 min |
| 7 | Update this runbook if any step was wrong or outdated | Platform Lead | PR merged |
| 8 | Document drill results in `docs/test-reports/dr-drill-<YYYY-QN>.md` | DevOps | Report committed |

### Drill Schedule

| Quarter | Date (target) | Status |
|---------|---------------|--------|
| Q1 2027 | First Tuesday of January | Not started |
| Q2 2027 | First Tuesday of April | Not started |
| Q3 2027 | First Tuesday of July | Not started |
| Q4 2027 | First Tuesday of October | Not started |

---

## Monitoring & Alerts

| Alert | Threshold | Action |
|-------|-----------|--------|
| Replication lag > 5 min | Azure Monitor metric alert | Investigate geo-replication health |
| Primary SQL unavailable > 5 min | Azure Monitor availability alert | Start DR Assessment |
| All primary region Container Apps returning 5xx for > 2 min | Azure Front Door health probe | Initiate DR Failover Runbook |
| PITR backup age > 24 h | Azure Advisor recommendation | Verify backup policy on SQL DB |

---

## Key Contacts

| Role | Name | Contact |
|------|------|---------|
| Incident Commander | Platform Lead | platform-lead@stylenest.com |
| DBA On-Call | Database Team | db-oncall@stylenest.com |
| Network / DNS | Infra Team | infra@stylenest.com |
| Product Owner | Commerce Team | product@stylenest.com |

---

## Document History

| Date | Change | Author |
|------|--------|--------|
| 2026-05-27 | Initial creation — ENH-INFRA-005 | Platform Team |
