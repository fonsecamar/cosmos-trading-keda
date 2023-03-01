@description('Cosmos DB account name, max length 44 characters, lowercase')
param cosmosAccountName string = 'cosmosdemo${suffix}'

@description('Event Hub namespace name, max length 44 characters, lowercase')
param eventHubNamespace string = 'eventhubdemo${suffix}'

@description('Stream Analytics Job name')
param streamAnalyticsJobName string = 'asademo${suffix}'

@description('Name of the azure container registry (must be globally unique)')
param acrName string = 'acrdemo${suffix}'

@description('Name of the azure kubernetes service')
param aksName string = 'aksdemo${suffix}'

@description('Storage account name, max length 44 characters, lowercase')
param storageAccountName string = 'blobdemo${suffix}'

@description('Location for resource deployment')
param location string = resourceGroup().location

@description('Suffix for resource deployment')
param suffix string = uniqueString(resourceGroup().id)

module cosmosdb 'cosmos.bicep' = {
  scope: resourceGroup()
  name: 'cosmosDeploy'
  params: {
    accountName: cosmosAccountName
    location: location
  }
}

module eventhub 'eventhub.bicep' = {
  name: 'eventHubDeploy'
  params: {
    eventHubNamespace: eventHubNamespace
    location: location
  }
}

module streamanalytics 'streamanalytics.bicep' = {
  name: 'streamAnalyticsDeploy'
  params: {
    streamAnalyticsJobName: streamAnalyticsJobName
    location: location
    cosmosOutputAccountName: cosmosdb.outputs.cosmosAccountName
    cosmosOutputDatabaseName: cosmosdb.outputs.cosmosDatabaseName
    cosmosOutputContainerName: cosmosdb.outputs.cosmosMarketDataContainerName
    marketdataInputEventHubNamespaceName: eventHubNamespace
    marketdataInputEventHubName: eventhub.outputs.marketdataHubName
    marketdataInputEventHubConsumerGroupName: eventhub.outputs.asaConsumerGroup
  }
}

module acr 'containerregistry.bicep' = {
  name: 'acrDeploy'
  params: {
    acrName: acrName
    location: location
  }
}

module aks 'aks.bicep' = {
  name: 'aksDeploy'
  params: {
    aksName: aksName
    location: location
    acrName: acr.outputs.acrName
  }
}

module blob 'blob.bicep' = {
  name: 'blobDeploy'
  params: {
    storageAccountName: storageAccountName
    location: location
  }
}
