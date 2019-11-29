# Sagan

A Cosmos DB pump. Pumping document particles into the Cosmos.

## Prequisites

* .NET Core
* PowerShell
* Azure Subscription
* `az` CLI

## Getting started

1. Run `./deploy.ps1` - Deploy Azure resources and save settings to env vars
1. Run `./package.ps1` - Build and Publish exe and run `Sagan.exe`

## Usage

    Sagan.exe (total-items) (max-parallel) (data-size-bytes)

Sagan will create `(total-items)` Documents of at least `(data-size-bytes)` each in Cosmos DB with
`(max-parallel)` degree of parallelism. Once the run has completed a report will be generated with statistics.



## App Settings

Either in Environment variables or appsettings.json.

    Cosmos_ConnectionString
    Cosmos_DatabaseName
    Cosmos_ContainerName
    APPINSIGHTS_INSTRUMENTATIONKEY

## Links and references

Implementing a simple ForEachAsync, part 2: <https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/>

Tuning query performance with Azure Cosmos DB: <https://docs.microsoft.com/en-us/azure/cosmos-db/sql-api-query-metrics>

ResourceThrottleRetryPolicy.cs: <https://github.com/Azure/azure-cosmos-dotnet-v3/blob/6f610954032d913eef13727669d0b7e0f061116c/Microsoft.Azure.Cosmos/src/ResourceThrottleRetryPolicy.cs#L105>
