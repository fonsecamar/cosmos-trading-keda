@description('Name of the azure kubernetes service')
param aksName string

@description('Azure Container Registry name for permission')
param acrName string

@description('Location for all resources.')
param location string = resourceGroup().location

resource aksCluster 'Microsoft.ContainerService/managedClusters@2022-11-02-preview' = {
  location: location
  name: aksName
  sku: {
    tier: 'Free'
    name: 'Basic'
  }
  tags: {
    displayname: 'AKS Cluster'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    kubernetesVersion: '1.25.5'
    enableRBAC: false
    dnsPrefix: aksName
    nodeResourceGroup: '${resourceGroup().name}-aks'
    identityProfile: {
      
    }
    agentPoolProfiles: [
      {
        name: 'agentpool'
        osDiskSizeGB: 0
        count: 3
        vmSize: 'Standard_B4ms'
        osType: 'Linux'
        type: 'VirtualMachineScaleSets'
        mode: 'System'
        enableAutoScaling: false
      }
    ]
  }
}

resource acr 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
  name: acrName
}

resource roleAssignmentACR 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, 'ACRAKS')
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d') 
    principalId: aksCluster.properties.identityProfile.kubeletidentity.objectId
    principalType: 'ServicePrincipal'
  }
}
