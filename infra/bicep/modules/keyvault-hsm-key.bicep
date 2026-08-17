// ENH-AUTH-006 — RSA-3072+ Production Keys via Azure Key Vault HSM Pool

@description('Name of the existing Key Vault (Premium SKU) to create the key in.')
param keyVaultName string

@description('Name of the RSA-HSM key.')
param keyName string = 'stylenest-jwt-rsa3072'

@description('Key size in bits. RSA-HSM supports 2048, 3072, 4096. Min 3072 per TSD §5.3.')
@allowed([3072, 4096])
param keySize int = 3072

@description('Key type. Must be RSA-HSM for hardware-backed keys.')
@allowed(['RSA-HSM'])
param keyType string = 'RSA-HSM'

@description('Key operations permitted for this key.')
param keyOps array = ['sign', 'verify', 'wrapKey', 'unwrapKey']

@description('ISO 8601 date-time of key expiry (optional, e.g. 2027-01-01T00:00:00Z). Leave empty for no expiry.')
param expiresOn string = ''

// ── Key Vault reference ───────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

// ── RSA-HSM Key ───────────────────────────────────────────────────────────────
// Stored in Key Vault HSM (requires Premium SKU)
// Provides hardware-backed key material that never leaves the HSM boundary
resource jwtRsaHsmKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  name:   keyName
  parent: keyVault

  properties: {
    kty:     keyType   // RSA-HSM — hardware-backed
    keySize: keySize   // 3072-bit minimum per TSD §5.3

    keyOps: keyOps

    attributes: {
      enabled:   true
      exportable: false   // Private key is non-exportable from HSM
      exp:        empty(expiresOn) ? null : dateTimeToEpoch(expiresOn)
    }

    // Key rotation policy — auto-rotate 30 days before expiry
    rotationPolicy: {
      attributes: {
        expiryTime: 'P2Y'   // 2-year key lifetime
      }
      lifetimeActions: [
        {
          trigger: {
            timeBeforeExpiry: 'P30D'    // Rotate 30 days before expiry
          }
          action: {
            type: 'Rotate'
          }
        }
        {
          trigger: {
            timeBeforeExpiry: 'P7D'    // Notify 7 days before expiry
          }
          action: {
            type: 'Notify'
          }
        }
      ]
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output keyId          string = jwtRsaHsmKey.id
output keyName        string = jwtRsaHsmKey.name
output keyVaultKeyUri string = jwtRsaHsmKey.properties.keyUriWithVersion
