# Cosmos DB NoSQL API - Trading Demo

## Introduction

This repository provides a code sample in .NET on how to use some Azure Cosmos DB features integrated with Azure Funcions running on Azure Kubernetes Services + KEDA.

## Requirements to deploy
> Setup shell was tested on WSL2 (Ubuntu 22.04.2 LTS)

* <a href="https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt#option-1-install-with-one-command" target="_blank">Install Azure CLI</a>

* <a href="https://docs.docker.com/desktop/windows/wsl/#download" target="_blank">Install Docker</a>

* <a href="https://kubernetes.io/docs/tasks/tools/install-kubectl-linux/#install-using-native-package-management" target="_blank">Install kubectl</a>

* <a href="https://helm.sh/docs/intro/install/#from-apt-debianubuntu" target="_blank">Install Heml</a>

* <a href="https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local?tabs=v4%2Clinux%2Ccsharp%2Cportal%2Cbash#install-the-azure-functions-core-tools" target="_blank">Install Azure Functions Core Tools</a>

* <a href="https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu#install-the-sdk" target="_blank">Install .NET SDK 7.0</a>

* <a href="https://git-scm.com/download/linux" target="_blank">Install Git</a>

## Setup environment

> The setup will provision and configure all the resources required.

* Sign in with Azure CLI

    ```bash
    az login
    ```

* Clone the repo
    ```bash
    git clone https://github.com/fonsecamar/cosmos-trading-keda.git
    cd cosmos-trading-keda/deploy/
    ```

* Run setup.sh with the appropriete parameters. Keep the API's URIs prompted when completed.

    ```bash
    #SAMPLE
    #./setup.sh 00000000-0000-0000-0000-000000000000 rg-my-demo SouthCentralUS myrandomsuffix

    ./setup.sh <subscription id> <resource grouop> <location> <resources suffix>
    ```
> Setup has some pause stages. Hit enter to continue when prompted. 
> 
> It takes around 5min to provision and configure resoures.
>
> Resources created:
> - 2 resource groups
> - Azure Blob Storage (ADLS Gen2)
> - Azure Cosmos DB account (1 database with 1000 RUs autoscale shared with 5 collections)
> - Azure Event Hub standard
> - Azure Steam Analytics job
> - Azure Container Registry basic
> - Azure Kubernetes Service (AKS) (1 node pool - 3 nodes Standard_B4ms)

## Running the sample

1. Start Azure Stream Analytics job

1. Run markerdata generator

    ```bash
    cd ../src/marketdata-generator
    dotnet run
    ```

1. Check Cosmos DB marketdata container (updated every 15 second by Azure Stream Analytics job).

4. Call GetStockPrice function

    ```bash
    #Setting variables
    SUFFIX=<your suffix>
    LOCATION=<your location>

    # Returns Stock Price by symbol
    curl --request GET "http://ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com/api/stock/MSFT"
    ```

1. Call CreateOrder function

    ```bash
    # Creates an Order
    curl --request POST "http://ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com/api/orders/create" \
    --header 'Content-Type: application/json' \
    --data-raw '{
        "customerId": 99999999,
        "quantity": 1000,
        "symbol": "MSFT",
        "price": 300,
        "action": "buy"
    }'
    ```

1. Call GetOrder function (use orderId from the previous response)

    ```bash
    # Returns Order by orderId
    curl --request GET "http://ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com/api/orders/{orderId}"
    ```

1. Call GetExecutions function (use the same orderId)

    ```bash
    -- Returns Order Executions by orderId
    curl --request GET "http://ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com/api/orders/execution/{orderId}"
    ```

1. Call GetCustomerPortfolio function (use customerId provided on step 1)

    ```bash
    -- Returns Customer Portfolio by customerId
    curl --request GET "http://ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com/api/customerPortfolio/{customerId}"
    ```

<br/>

# How to Contribute

If you find any errors or have suggestions for changes, please be part of this project!

1. Create your branch: `git checkout -b my-new-feature`
2. Add your changes: `git add .`
3. Commit your changes: `git commit -m '<message>'`
4. Push your branch to Github: `git push origin my-new-feature`
5. Create a new Pull Request 😄