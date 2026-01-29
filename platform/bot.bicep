param botName string
param devopsBotAppId string
param devopsBotTenantId string = tenant().tenantId
param botDisplayName string = 'Devops Teams Notifications'
param environment string
param endpoint string
// Data residency
@allowed([
  'westeurope'
  'global'
  'westus'
  'centralindia'
])
param location string = 'westeurope'

resource botService 'Microsoft.BotService/botServices@2023-09-15-preview' = {
  name: botName
  kind: 'azurebot'
  location: location
  sku: {
    name: 'S1'
  }
  properties: {
    displayName: 'Bot for ${botDisplayName} ${environment}'
    msaAppId: devopsBotAppId
    msaAppType: 'SingleTenant'
    msaAppTenantId: devopsBotTenantId
    endpoint: endpoint
  }
}
