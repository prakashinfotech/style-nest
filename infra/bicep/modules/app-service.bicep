// ENH-INFRA-001 — TLS 1.3 floor on App Services
// ENH-INFRA-003 — Diagnostic Settings → Log Analytics
// ENH-INFRA-004 — FinOps Resource Tagging

@description('Name of the App Service.')
param appServiceName string

@description('Resource ID of the App Service Plan.')
param appServicePlanId string

@description('Docker image reference (e.g. mcr.microsoft.com/dotnet/aspnet:10.0).')
param dockerImage string

@description('Azure region for deployment.')
param location string = resourceGroup().location

@description('Resource ID of the Log Analytics workspace for diagnostics (ENH-INFRA-003).')
param logAnalyticsWorkspaceId string

// ── ENH-INFRA-004: FinOps tagging ────────────────────────────────────────────
@description('Deployment environment (production | staging | development).')
param environmentName string

@description('Cost centre identifier for FinOps allocation.')
param costCenter string

@description('Owner email or team alias for this resource.')
param owner string

// ── App Service ───────────────────────────────────────────────────────────────
resource appService 'Microsoft.Web/sites@2023-01-01' = {
  name:     appServiceName
  location: location

  // ENH-INFRA-004 — Every resource tagged with Environment, CostCenter, Owner
  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  properties: {
    serverFarmId: appServicePlanId
    httpsOnly:    true

    siteConfig: {
      // ── ENH-INFRA-001: TLS 1.3 floor ───────────────────────────────────
      minTlsVersion:      '1.3'
      ftpsState:          'Disabled'     // no FTP/S — only deployment via CI/CD
      http20Enabled:      true
      alwaysOn:           true

      // Linux container hosting
      linuxFxVersion:     dockerImage
      appCommandLine:     ''

      // Security hardening
      clientAffinityEnabled:    false
      remoteDebuggingEnabled:   false
      detailedErrorLoggingEnabled: false
      requestTracingEnabled:    false
    }
  }
}

// ── ENH-INFRA-003: Diagnostic Settings → Log Analytics ───────────────────────
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name:  '${appServiceName}-diag'
  scope: appService

  properties: {
    workspaceId: logAnalyticsWorkspaceId

    logs: [
      {
        category: 'AppServiceHTTPLogs'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'AppServiceConsoleLogs'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'AppServiceAppLogs'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
      {
        category: 'AppServiceAuditLogs'
        enabled:  true
        retentionPolicy: { days: 90, enabled: true }
      }
    ]

    metrics: [
      {
        category: 'AllMetrics'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
      }
    ]
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output appServiceId          string = appService.id
output defaultHostName       string = appService.properties.defaultHostName
output principalId           string = appService.identity.principalId
