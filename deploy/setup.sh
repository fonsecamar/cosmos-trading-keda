SUBSCRIPTION_ID=$1
RESOURCE_GROUP=$2
LOCATION=$3
SUFFIX=$4

echo 'Subscription Id     :' $SUBSCRIPTION_ID
echo 'Resource Group      :' $RESOURCE_GROUP
echo 'Location            :' $LOCATION
echo 'Deploy Suffix       :' $SUFFIX

echo 'Validate variables above and press any key to continue setup...'
read -n 1

#Start infrastructure deployment
cd ../infrastructure
echo "Directory changed: '$(pwd)'"

az account set --subscription $SUBSCRIPTION_ID
az account show

echo 'Validate current subscription and press any key to continue setup...'
read -n 1

RGCREATED=$(az group create \
                --name $RESOURCE_GROUP \
                --location $LOCATION \
                --query "properties.provisioningState" \
                -o tsv)

if [ "$RGCREATED" != "Succeeded" ] 
then
    echo 'Resource group creation failed! Exiting...'
    exit
fi

INFRADEPLOYED=$(az deployment group create \
                    --name CosmosDemoDeployment \
                    --resource-group $RESOURCE_GROUP \
                    --template-file ./main.bicep \
                    --parameters suffix=$SUFFIX \
                    --query "properties.provisioningState" \
                    -o tsv)

if [ "$INFRADEPLOYED" != "Succeeded" ] 
then
    echo 'Infrastructure deployment failed! Exiting...'
    exit
fi

echo 'Press any key to continue setup...'
read -n 1

cd ../src
echo "Directory changed: '$(pwd)'"

az acr login --name acrdemo$SUFFIX

docker build -f ./order-manager/Dockerfile -t acrdemo$SUFFIX.azurecr.io/ordermanager .
docker build -f ./order-processor/Dockerfile -t acrdemo$SUFFIX.azurecr.io/orderprocessor .

docker push acrdemo$SUFFIX.azurecr.io/ordermanager:latest
docker push acrdemo$SUFFIX.azurecr.io/orderprocessor:latest

#az acr build --registry acrdemo$SUFFIX --image orderprocessor:latest --file ./order-processor/Dockerfile .
#az acr build --registry acrdemo$SUFFIX --image orderprocessor:latest --file ./order-processor/Dockerfile .

echo 'Press any key to continue setup...'
read -n 1

az aks get-credentials --resource-group $RESOURCE_GROUP --name aksdemo$SUFFIX

kubectl create namespace keda
kubectl create namespace ordermanager
kubectl create namespace orderprocessor
kubectl create namespace ingress-nginx

helm repo add kedacore https://kedacore.github.io/charts
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo update

helm upgrade keda kedacore/keda --install --namespace keda
helm upgrade ingress-nginx ingress-nginx/ingress-nginx \
    --namespace ingress-nginx \
    --install \
    --set controller.replicaCount=2 \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-load-balancer-health-probe-request-path"=/healthz \
    --set controller.metrics.enabled=true \
    --set controller.podAnnotations."prometheus\.io/scrape"="true" \
    --set controller.podAnnotations."prometheus\.io/port"="10254" \
    --set controller.service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=ingressdemo$SUFFIX

kubectl apply --kustomize github.com/kubernetes/ingress-nginx/deploy/prometheus/

echo 'Press any key to continue setup...'
read -n 1

blobKey=$(az storage account keys list -g $RESOURCE_GROUP -n blobdemo$SUFFIX --query [0].value -o tsv)
AzureWebJobsStorage="DefaultEndpointsProtocol=https;AccountName=blobdemo$SUFFIX;AccountKey=$blobKey;EndpointSuffix=core.windows.net"
CosmosDBConnection=$(az cosmosdb keys list -g $RESOURCE_GROUP -n cosmosdemo$SUFFIX --type connection-strings --query connectionStrings[0].connectionString -o tsv)
eventHubConnection=$(az eventhubs namespace authorization-rule keys list -g $RESOURCE_GROUP --namespace-name eventhubdemo$SUFFIX -n RootManageSharedAccessKey --query primaryConnectionString -o tsv)

cd ./marketdata-generator/
echo "Directory changed: '$(pwd)'"

# File to modify
FILE_TO_REPLACE=settings.json

# Pattern for your tokens -- e.g. ${token}
TOKEN_PATTERN='(?<=\$\{)\w+(?=\})'

# Find all tokens to replace
TOKENS=$(grep -oP ${TOKEN_PATTERN} ${FILE_TO_REPLACE} | sort -u)

# Loop over tokens and use sed to replace
for token in $TOKENS
do
  echo "Replacing \${${token}} with ${!token}"
  sed -i "s|\${${token}}|${!token}|" ${FILE_TO_REPLACE}
done

cd ../order-manager/
echo "Directory changed: '$(pwd)'"

# File to modify
FILE_TO_REPLACE=local.settings.json

# Pattern for your tokens -- e.g. ${token}
TOKEN_PATTERN='(?<=\$\{)\w+(?=\})'

# Find all tokens to replace
TOKENS=$(grep -oP ${TOKEN_PATTERN} ${FILE_TO_REPLACE} | sort -u)

# Loop over tokens and use sed to replace
for token in $TOKENS
do
  echo "Replacing \${${token}} with ${!token}"
  sed -i "s|\${${token}}|${!token}|" ${FILE_TO_REPLACE}
done

func kubernetes deploy \
    --image-name acrdemo$SUFFIX.azurecr.io/ordermanager:latest \
    --min-replicas 1 \
    --name ordermanager \
    --namespace ordermanager \
    --show-service-fqdn \
    --service-type ClusterIP

cd ../order-processor/
echo "Directory changed: '$(pwd)'"

# File to modify
FILE_TO_REPLACE=local.settings.json

# Pattern for your tokens -- e.g. ${token}
TOKEN_PATTERN='(?<=\$\{)\w+(?=\})'

# Find all tokens to replace
TOKENS=$(grep -oP ${TOKEN_PATTERN} ${FILE_TO_REPLACE} | sort -u)

# Loop over tokens and use sed to replace
for token in $TOKENS
do
  echo "Replacing \${${token}} with ${!token}"
  sed -i "s|\${${token}}|${!token}|" ${FILE_TO_REPLACE}
done

func kubernetes deploy \
    --image-name acrdemo$SUFFIX.azurecr.io/orderprocessor:latest \
    --min-replicas 1 \
    --name orderprocessor \
    --namespace orderprocessor \
    --show-service-fqdn \
    --service-type ClusterIP

cd ../../deploy/
echo "Directory changed: '$(pwd)'"

kubectl apply -f ./deploy-ingress-prometheus.yaml
kubectl apply -f ./scaled-object.yaml
kubectl apply -f ./deploy-ingress-api.yaml

echo ""
echo "***************************************************"
echo "*************  Deploy completed!  *****************"
echo ""
echo "Use the URI below for your API calls"
echo "URI: ==> ingressdemo$SUFFIX.$LOCATION.cloudapp.azure.com" 
echo ""
echo "Next steps:"
echo "1. Start Stream Analytics job"
echo "2. Run marketdata-generator"
echo "3. Call APIs"
echo "***************************************************"