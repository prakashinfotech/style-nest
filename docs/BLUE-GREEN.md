# Blue-Green Deployment Runbook

**Feature:** ENH-INFRA-006  
**SOW ref:** FR-OPS-004  
**Auto-rollback trigger:** HTTP error rate > 1% during canary window

---

## Overview

StyleNest uses a **blue-green deployment** strategy to achieve zero-downtime releases with automatic rollback protection.

| Term | Meaning |
|------|---------|
| **Blue** | Stable, currently-serving revision |
| **Green** | New candidate revision under test |
| **Canary window** | Period during which traffic is split while error rate is monitored |
| **Promote** | Switch 100% traffic to green; deactivate blue |
| **Rollback** | Restore 100% traffic to blue; deactivate green |

Error rate is measured as `(non-200 health probe responses) / (total probes) × 100`.  
If this exceeds **1%** during any canary window, the deploy job fails and the `auto-rollback` job fires automatically.

---

## Architecture

```
Internet
   │
   ▼
nginx Router  :5000          ← traffic-weighted entry point (local)
   │
   ├──── blue_gateway   ──► gateway-api-blue  → auth/catalog/cart/… -blue
   └──── green_gateway  ──► gateway-api-green → auth/catalog/cart/… -green
```

In **Azure Container Apps (production)** the traffic split is managed natively via revision weights — no nginx needed.

---

## Local Blue-Green Testing

### Start the blue-green stack

```bash
docker compose -f docker-compose.blue-green.yml up --build -d
```

Wait for all containers to be healthy (≈2–3 minutes):

```bash
docker compose -f docker-compose.blue-green.yml ps
```

### Check active slot

```bash
./scripts/blue-green-swap.sh status
```

### Promote green (switch traffic)

```bash
./scripts/blue-green-swap.sh promote-green
```

This:
1. Verifies green gateway health (`/health`)
2. Writes the nginx upstream config to point at green
3. Signals nginx to reload (graceful, zero connection drops)
4. Probes the router and rolls back automatically if it fails

### Monitor with auto-rollback

```bash
# Monitor for 5 minutes; rollback if error rate > 1%
./scripts/blue-green-swap.sh monitor --timeout=300 --threshold=1

# Custom window
./scripts/blue-green-swap.sh monitor --timeout=600 --threshold=2
```

### Manual rollback

```bash
./scripts/blue-green-swap.sh rollback
```

### Tear down

```bash
docker compose -f docker-compose.blue-green.yml down -v
```

---

## Production Deployment (GitHub Actions)

### Trigger manually

```
GitHub → Actions → "Blue-Green Deploy" → Run workflow
```

Parameters:

| Input | Default | Description |
|-------|---------|-------------|
| `services` | all 8 | Comma-separated service list |
| `error_threshold` | `1` | % error rate that triggers rollback |
| `canary_window_seconds` | `120` | Hold time at each traffic split step |

### Pipeline stages

```
build-green
    │
    ▼
deploy-green-revision  (0% traffic — green is idle)
    │
    ▼
smoke-test-green       (10% canary + monitoring window)
    │
    ├─ PASS ──► promote-green  (50% → 100% → deactivate blue)
    └─ FAIL ──► auto-rollback  (0% green → 100% blue → deactivate green)
```

### Traffic split timeline

```
t=0      Deploy green revision (0% traffic)
t+2min   Start smoke test: 10% → green, 90% → blue
t+4min   Check error rate; if OK → shift to 50/50
t+6min   Check error rate; if OK → shift 100% → green
t+8min   Deactivate blue revision
```

### Rollback at any stage

If `smoke-test-green` fails (error rate exceeded or health check timeout), the `auto-rollback` job fires:

1. Sets green revision traffic weight to **0%**
2. Restores blue revision to **100%**
3. Deactivates the green revision
4. Posts a failure annotation on the workflow run

The deployed code never reaches more than **10% of traffic** before the error rate check fires.

---

## Monitoring during rollout

| Signal | Source | Alert threshold |
|--------|--------|-----------------|
| HTTP error rate | `/health` probe response | > 1% |
| Revision health | ACA revision running state | ≠ Running |
| Response time p95 | Azure Monitor → Container Apps | > 2s |

Azure Monitor alert rule (configured separately):
```
metricName:   Requests
dimension:    StatusCodeClass = 5xx
threshold:    > 1% of total for 5-minute window
action group: PagerDuty / Slack webhook
```

---

## Environment variables / secrets required

| Secret | Description |
|--------|-------------|
| `ACR_REGISTRY` | Azure Container Registry hostname, e.g. `stylenestacr.azurecr.io` |
| `AZURE_CREDENTIALS` | Service principal JSON for `az login` |
| `AZURE_RESOURCE_GROUP` | Resource group containing the Container Apps environment |
| `ACA_ENVIRONMENT` | Azure Container Apps environment name |

---

## Troubleshooting

### Green revision stuck in "Provisioning"

```bash
az containerapp revision show \
  --name stylenest-catalog-api \
  --resource-group <rg> \
  --revision <revision-name> \
  --query "properties.runningState"
```

Check logs:
```bash
az containerapp logs show \
  --name stylenest-catalog-api \
  --resource-group <rg> \
  --revision <revision-name>
```

### Nginx not reloading after slot swap

```bash
docker exec stylenest-router cat /etc/nginx/active/slot
docker exec stylenest-router nginx -t           # test config syntax
docker exec stylenest-router nginx -s reload    # manual reload
```

### Both slots down

```bash
# Restart entire blue-green stack
docker compose -f docker-compose.blue-green.yml restart
```

---

*Last updated: ENH-INFRA-006 implementation — see `scripts/blue-green-swap.sh` and `.github/workflows/blue-green-deploy.yml` for implementation details.*
