$location = 'australiaeast'
$loc = 'aue'
$rg = 'sagan-rg'
$tags = 'project=Sagan'
$insights = 'sagan-insights'
$cosmos = 'sagan-cosmos'
$cosmosDb = 'sagan'
$cosmosDbThroughput = 400
$cosmosDbCollection = 'test1'

# Create Resource Group
az group create -n $rg --location $location --tags $tags


# APPLICATION INSIGHTS
# https://docs.microsoft.com/en-us/cli/azure/ext/application-insights/monitor/app-insights/component?view=azure-cli-latest
az extension add -n application-insights

$instrumentationKey = ( az monitor app-insights component create --app $insights --location $location -g $rg --tags $tags | ConvertFrom-Json ).instrumentationKey
Write-Host "iKey = $instrumentationKey"

# COSMOS DB
# https://docs.microsoft.com/en-us/cli/azure/cosmosdb?view=azure-cli-latest#az-cosmosdb-create
az cosmosdb create -n $cosmos -g $rg --tags $tags --locations regionName=$location
az cosmosdb database create -n $cosmos -g $rg --db-name $cosmosDb --throughput $cosmosDbThroughput
az cosmosdb collection create --collection-name $cosmosDbCollection --db-name $cosmosDb -n $cosmos -g $rg --partition-key-path '/id'

# Get the connection string
$cosmosConn = ( az cosmosdb list-connection-strings -n $cosmos -g $rg | ConvertFrom-Json ).connectionStrings[0].connectionString

Write-Host "Cosmos Conn = $cosmosConn"


# Tear down
# az group delete -n $rg --yes
