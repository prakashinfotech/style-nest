// ENH-INFRA-005 — Disaster Recovery: SQL Geo-Replication (Active Geo-Replication)
//
// Creates an Active Geo-Replication secondary database in the DR region (Central India).
// This satisfies:
//   TSD §11        — Disaster Recovery requirements
//   NFR-AVAIL-002  — RTO ≤ 1 hour
//   NFR-AVAIL-003  — RPO ≤ 15 minutes
//
// Active Geo-Replication provides:
//   - Continuous async replication to a readable secondary
//   - Sub-5-second RPO under normal conditions (far below the 15-min SLA)
//   - Forced failover completes in < 30 minutes (well within the 1-hour RTO SLA)
//   - Readable secondary for DR drill read workloads (no extra cost for reads)
//
// Failover process:
//   az sql db replica set-primary \
//     --resource-group <dr-rg> \
//     --server <dr-sql-server> \
//     --name StyleNestDb

@description('Name of the PRIMARY SQL logical server (in primary region).')
param primarySqlServerName string

@description('Name of the PRIMARY SQL database to replicate.')
param databaseName string = 'StyleNestDb'

@description('Name for the SECONDARY SQL logical server (in DR region).')
param secondarySqlServerName string

@description('DR region for the secondary SQL server. Default: Central India.')
param drLocation string = 'centralindia'

@description('SQL admin login (must match primary server).')
param adminLogin string

@secure()
@description('SQL admin password (must match primary server).')
param adminPassword string

@description('Resource ID of the Log Analytics workspace in the DR region.')
param logAnalyticsWorkspaceId string = ''

// ── ENH-INFRA-004: FinOps tagging ────────────────────────────────────────────
param environmentName string
param costCenter      string
param owner           string

// ── Secondary SQL Logical Server (DR region) ─────────────────────────────────
resource secondarySqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name:     secondarySqlServerName
  location: drLocation

  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
    DRRole:      'Secondary'
  }

  properties: {
    administratorLogin:         adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion:          '1.3'
    publicNetworkAccess:        'Disabled'
  }
}

// ── Advanced Threat Protection on secondary ───────────────────────────────────
resource secondaryAtp 'Microsoft.Sql/servers/advancedThreatProtectionSettings@2023-05-01-preview' = {
  name:   'Default'
  parent: secondarySqlServer
  properties: {
    state: 'Enabled'
  }
}

// ── Reference to primary SQL server (must already exist in deployment) ────────
resource primarySqlServer 'Microsoft.Sql/servers@2023-05-01-preview' existing = {
  name: primarySqlServerName
}

// ── Reference to primary database ─────────────────────────────────────────────
resource primaryDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' existing = {
  name:   databaseName
  parent: primarySqlServer
}

// ── Active Geo-Replication secondary database ─────────────────────────────────
// Creates a readable secondary replica of StyleNestDb in Central India.
// 'createMode: Secondary' + 'sourceDatabaseId' configures Active Geo-Replication.
resource geoReplicaDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name:     databaseName
  parent:   secondarySqlServer
  location: drLocation

  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
    DRRole:      'GeoReplica'
    RPOTarget:   'PT15M'
    RTOTarget:   'PT1H'
  }

  sku: {
    name:     'BusinessCritical'
    tier:     'BusinessCritical'
    capacity: 4
  }

  properties: {
    createMode:        'Secondary'
    sourceDatabaseId:  primaryDatabase.id
    zoneRedundant:     true
    // Readable secondary (default for Active Geo-Replication)
    secondaryType:     'Geo'
  }
}

// ── Transparent Data Encryption on secondary ─────────────────────────────────
resource secondaryTde 'Microsoft.Sql/servers/databases/transparentDataEncryption@2023-05-01-preview' = {
  name:   'current'
  parent: geoReplicaDatabase
  properties: {
    state: 'Enabled'
  }
}

// ── Diagnostic Settings on secondary (optional — only when LA workspace provided) ──
resource secondaryDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name:  '${secondarySqlServerName}-db-diag'
  scope: geoReplicaDatabase

  properties: {
    workspaceId: logAnalyticsWorkspaceId

    logs: [
      {
        category: 'SQLInsights'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'Errors'
        enabled:  true
        retentionPolicy: { days: 90, enabled: true }
      }
    ]

    metrics: [
      {
        category: 'Basic'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
    ]
  }
}

// ── Failover Group (optional — use for automatic failover with connection string transparency) ──
// Uncommenting this enables auto-failover at the cost of a slight write-latency overhead.
// Leave commented for manual failover (operator-triggered via runbook).
//
// resource failoverGroup 'Microsoft.Sql/servers/failoverGroups@2023-05-01-preview' = {
//   name:   'fg-stylenest-${environmentName}'
//   parent: primarySqlServer
//   properties: {
//     partnerServers: [{ id: secondarySqlServer.id }]
//     databases: [ primaryDatabase.id ]
//     readWriteEndpoint: {
//       failoverPolicy: 'Automatic'
//       failoverWithDataLossGracePeriodMinutes: 60
//     }
//   }
// }

// ── Outputs ───────────────────────────────────────────────────────────────────
output secondarySqlServerId   string = secondarySqlServer.id
output secondarySqlServerFqdn string = secondarySqlServer.properties.fullyQualifiedDomainName
output geoReplicaDatabaseId   string = geoReplicaDatabase.id
