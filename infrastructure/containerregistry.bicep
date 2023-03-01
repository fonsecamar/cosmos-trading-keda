@minLength(5)
@maxLength(50)
@description('Name of the azure container registry (must be globally unique)')
param acrName string

@description('Location for all resources.')
param location string = resourceGroup().location

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: acrName
  location: location
  tags: {
    displayName: 'Container Registry'
    'container.registry': acrName
  }
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: false
  }
}

output acrLoginServer string = acr.properties.loginServer
output acrName string = acr.name
