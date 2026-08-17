// ENH-INFRA-011 — Key Vault Firewall + Private Endpoint (MSI tokens only, no public KV access)
// ENH-INFRA-003 — Diagnostic Settings → Log Analytics
// ENH-INFRA-004 — FinOps Resource Tagging

@description('Name of the Key Vault resource.')
param keyVaultName string

@description('Azure region for deployment.')
param location string = resourceGroup().location

@description('Azure AD tenant ID for Key Vault access policies.')
param tenantId string = subscription().tenantId

@description('Resource ID of the Log Analytics workspace for diagnostics (ENH-INFRA-003).')
param logAnalyticsWorkspaceId string

@description('Subnet resource ID for the private endpoint (ENH-INFRA-011).')
param privateEndpointSubnetId string

@description(
  'Array of AAD object IDs (service principals / MSIs) that may access the vault. ' +
  'Public access is fully disabled — all access goes through private endpoint.')
param allowedObjectIds array = []

// ── ENH-INFRA-004: FinOps tagging ────────────────────────────────────────────
param environmentName string
param costCenter      string
param owner           string

// ── Key Vault ─────────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name:     keyVaultName
  location: location

  // ENH-INFRA-004 — FinOps tags
  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  properties: {
    sku: {
      family: 'A'
      name:   'premium'      // Premium SKU required for HSM-backed keys (ENH-AUTH-006)
    }
    tenantId:              tenantId
    enableRbacAuthorization: true  // RBAC model — no legacy access policies
    enableSoftDelete:      true
    softDeleteRetentionInDays: 90
    enablePurgeProtection: true

    // ── ENH-INFRA-011: Firewall — deny all public traffic ────────────────
    publicNetworkAccess: 'Disabled'
    networkAcls: {
      bypass:        'AzureServices'
      defaultAction: 'Deny'
      ipRules:       []
      virtualNetworkRules: []
    }
  }
}

// ── ENH-INFRA-011: Private Endpoint ───────────────────────────────────────────
resource privateEndpoint 'Microsoft.Network/privateEndpoints@2023-09-01' = {
  name:     '${keyVaultName}-pe'
  location: location

  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  properties: {
    subnet: {
      id: privateEndpointSubnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${keyVaultName}-pe-conn'
        properties: {
          privateLinkServiceId: keyVault.id
          groupIds:             ['vault']
        }
      }
    ]
  }
}

// ── ENH-INFRA-011: Private DNS Zone Group ─────────────────────────────────────
resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2023-09-01' = {
  name:   'default'
  parent: privateEndpoint

  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'privatelink-vaultcore-azure-net'
        properties: {
          // References the private DNS zone for Key Vault — must exist in the VNet
          privateDnsZoneId: resourceId('Microsoft.Network/privateDnsZones', 'privatelink.vaultcore.azure.net')
        }
      }
    ]
  }
}

// ── RBAC assignments for allowed MSI object IDs ───────────────────────────────
// Grants "Key Vault Secrets User" to each listed object ID (e.g. App Service MSIs)
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource secretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = [for (objectId, i) in allowedObjectIds: {
  name:  guid(keyVault.id, objectId, kvSecretsUserRoleId)
  scope: keyVault

  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId:      objectId
    principalType:    'ServicePrincipal'
  }
}]

// ── ENH-INFRA-003: Diagnostic Settings → Log Analytics ───────────────────────
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  name:  '${keyVaultName}-diag'
  scope: keyVault

  properties: {
    workspaceId: logAnalyticsWorkspaceId

    logs: [
      {
        category: 'AuditEvent'
        enabled:  true
        retentionPolicy: { days: 90, enabled: true }
      }
      {
        category: 'AzurePolicyEvaluationDetails'
        enabled:  true
        retentionPolicy: { days: 30, enabled: true }
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
output keyVaultId    string = keyVault.id
output keyVaultUri   string = keyVault.properties.vaultUri
