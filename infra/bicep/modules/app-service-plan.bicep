// ENH-INFRA-004 — FinOps Resource Tagging on App Service Plan

@description('App Service Plan resource name.')
param planName string

@description('Azure region.')
param location string = resourceGroup().location

@description('SKU name (e.g. P2v3).')
param sku string = 'P2v3'

@description('Number of instances (manual scale; use autoscale rules for production).')
@minValue(1)
@maxValue(10)
param capacity int = 2

// ENH-INFRA-004 — FinOps tags
param environmentName string
param costCenter      string
param owner           string

resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name:     planName
  location: location

  tags: {
    Environment: environmentName
    CostCenter:  costCenter
    Owner:       owner
    ManagedBy:   'Bicep'
  }

  kind: 'linux'

  sku: {
    name:     sku
    capacity: capacity
  }

  properties: {
    reserved: true   // Required for Linux plan
    zoneRedundant: capacity >= 3  // ZRS requires ≥3 instances
  }
}

output planId string = appServicePlan.id
