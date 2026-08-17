// ENH-INFRA-001 — TLS 1.3 floor on SQL Server
// ENH-INFRA-003 — Diagnostic Settings → Log Analytics
// ENH-INFRA-004 — FinOps Resource Tagging

@description('SQL Server logical server name.')
param sqlServerName string

@description('SQL Database name.')
param databaseName string = 'StyleNestDb'

@description('Azure region for deployment.')
param location string = resourceGroup().location

@description('SQL admin login name.')
param adminLogin string

@secure()
@description('SQL admin password. Store in Key Vault; reference via Key Vault reference in parameter file.')
param adminPassword string

@description('Resource ID of the Log Analytics workspace for diagnostics (ENH-INFRA-003).')
param logAnalyticsWorkspaceId string

// ── ENH-INFRA-004: FinOps tagging ────────────────────────────────────────────
param environmentName string
param costCenter      string
param owner           string

// ── SQL Server ────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name:     sqlServerName
  location: location

  // ENH-INFRA-004 — FinOps tags
  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  properties: {
    administratorLogin:         adminLogin
    administratorLoginPassword: adminPassword

    // ── ENH-INFRA-001: TLS 1.3 floor ────────────────────────────────────
    minimalTlsVersion: '1.3'

    // Deny public access — all traffic via private endpoint
    publicNetworkAccess: 'Disabled'
  }
}

// ── SQL Database (Business Critical for HA) ───────────────────────────────────
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name:   databaseName
  parent: sqlServer
  location: location

  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  sku: {
    name:     'BusinessCritical'
    tier:     'BusinessCritical'
    capacity: 4
  }

  properties: {
    zoneRedundant:     true
    backupStorageRedundancy: 'GeoZone'
  }
}

// ── Transparent Data Encryption (always ON for production) ────────────────────
resource tde 'Microsoft.Sql/servers/databases/transparentDataEncryption@2023-05-01-preview' = {
  name:   'current'
  parent: sqlDatabase

  properties: {
    state: 'Enabled'
  }
}

// ── ENH-INFRA-003: Diagnostic Settings → Log Analytics ───────────────────────
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name:  '${sqlServerName}-db-diag'
  scope: sqlDatabase

  properties: {
    workspaceId: logAnalyticsWorkspaceId

    logs: [
      {
        category: 'SQLInsights'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'AutomaticTuning'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'QueryStoreRuntimeStatistics'
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

// ── Advanced Threat Protection ─────────────────────────────────────────────────
resource advancedThreatProtection 'Microsoft.Sql/servers/advancedThreatProtectionSettings@2023-05-01-preview' = {
  name:   'Default'
  parent: sqlServer

  properties: {
    state: 'Enabled'
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output sqlServerId       string = sqlServer.id
output sqlServerFqdn     string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseId     string = sqlDatabase.id
