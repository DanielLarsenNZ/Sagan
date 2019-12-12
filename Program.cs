using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sagan
{
    class Program
    {
        const string CosmosConnectionStringKey = "Cosmos_ConnectionString";
        const string CosmosDatabaseNameKey = "Cosmos_DatabaseName";
        const string CosmosContainerNameKey = "Cosmos_ContainerName";
        const string AppInsightsInstrumentationKey = "APPINSIGHTS_INSTRUMENTATIONKEY";

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Sagan: Cosmos Pump");
            Console.WriteLine("Usage: Sagan.exe (total-items) (max-parallel) (data-size-bytes)");

            // Defaults
            int totalItems = 1000;
            int maxParallel = 5;
            int dataSizeBytes = 25000;

            if (args.Length == 3) int.TryParse(args[2], out dataSizeBytes);
            if (args.Length >= 2) int.TryParse(args[1], out maxParallel);
            if (args.Length >= 1) int.TryParse(args[0], out totalItems);

            var now = DateTime.UtcNow;

            Console.WriteLine($"Sagan: {now}");
            Console.WriteLine($"Sagan: Total Items = {totalItems}");
            Console.WriteLine($"Sagan: Max degree of parallelism = {maxParallel}");
            Console.WriteLine($"Sagan: Document data size (bytes) = {dataSizeBytes}");

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            CheckConfig(config);

            var insights = InsightsHelper.InitializeTelemetryClient(config[AppInsightsInstrumentationKey]);

            // Construct a random string of data
            string data = "";
            var random = new Random();
            for (int i = 0; i < dataSizeBytes; i++)
            {
                data += (char)random.Next(65, 90);
            }

            // Load a list of Items
            var items = new List<Item>();
            for (int i = 1; i <= totalItems; i++)
            {
                items.Add(new Item
                {
                    Data = data,
                    DateTime = now,
                    ItemCount = i,
                    Id = Guid.NewGuid().ToString("N")
                });
            }

            using (var client = new CosmosClient(config[CosmosConnectionStringKey]))
            {
                var container = client.GetContainer(config[CosmosDatabaseNameKey], config[CosmosContainerNameKey]);
                string dependencyTypeName = $"Microsoft.Azure.Cosmos ({config[CosmosDatabaseNameKey]}/{config[CosmosContainerNameKey]})";

                // Concurrent bags for recording charges and exceptions
                var charges = new ConcurrentBag<double>();
                var exceptions = new ConcurrentBag<Exception>();

                var stopwatch = new Stopwatch();
                var cosmosStopwatch = new Stopwatch();
                stopwatch.Start();

                await Task.WhenAll(
                    // Partitioner splits items into n partitions where n = maxParallel
                    from partition in Partitioner.Create(items).GetPartitions(maxParallel)
                    select Task.Run(async delegate
                    {
                        using (partition)
                            while (partition.MoveNext())
                            {
                                using (insights.StartOperation<RequestTelemetry>("createitem"))
                                {

                                    ItemResponse<Item> response = null;
                                    var startTime = DateTime.UtcNow;
                                    bool success = false;
                                    try
                                    {
                                        cosmosStopwatch.Reset();
                                        cosmosStopwatch.Start();
                                        response = await container.CreateItemAsync(partition.Current);
                                        success = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        // log and continue
                                        insights.TrackException(ex);
                                        Console.Error.WriteLine(ex.Message);
                                        exceptions.Add(ex);
                                        continue;
                                    }
                                    finally
                                    {
                                        cosmosStopwatch.Stop();
                                        insights.TrackDependency(
                                            dependencyTypeName,
                                            "CreateItem",
                                            $"CreateItemAsync id = {partition.Current.Id}, pk = {partition.Current.Id}",
                                            startTime,
                                            cosmosStopwatch.Elapsed,
                                            success);
                                    }

                                    charges.Add(response.RequestCharge);
                                    insights.TrackEvent(
                                        "Sagan/CreateItem",
                                        metrics: new Dictionary<string, double>
                                        {
                                            {
                                                "RequestCharge", response.RequestCharge
                                            }
                                        });

                                    Console.WriteLine($"Item {partition.Current.ItemCount} request charge = {response.RequestCharge}");
                                }
                            }
                    }));

                stopwatch.Stop();

                double totalRequestCharge = charges.Sum();

                Console.WriteLine("============================================================");
                Console.WriteLine($"Sagan: {now}");

                foreach (var ex in exceptions) Console.WriteLine(ex.Message);

                Console.WriteLine($"{charges.Count} documents created in {stopwatch.Elapsed.TotalSeconds} seconds = {totalItems / stopwatch.Elapsed.TotalSeconds} TPS");
                Console.WriteLine($"Total request charge = {totalRequestCharge} = {totalRequestCharge / stopwatch.Elapsed.TotalSeconds} RU/s");
                Console.WriteLine($"Exceptions: {exceptions.Count}");
                Console.WriteLine("Quitting in 5 seconds...");
                // Flush insights and wait before closing
                insights.Flush();
                Task.Delay(5000).Wait();
            }
        }

        private static void CheckConfig(IConfiguration config)
        {
            if (
                string.IsNullOrEmpty(config[CosmosConnectionStringKey]) ||
                string.IsNullOrEmpty(config[CosmosDatabaseNameKey]) ||
                string.IsNullOrEmpty(config[CosmosContainerNameKey]) ||
                string.IsNullOrEmpty(config[AppInsightsInstrumentationKey]))
                throw new InvalidOperationException($"Expecting App settings or Env vars named {CosmosConnectionStringKey}, {CosmosDatabaseNameKey}, {CosmosContainerNameKey}, {AppInsightsInstrumentationKey}.");
        }
    }

    class Item
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Data { get; set; }
        public int ItemCount { get; set; }
        public DateTime DateTime { get; set; }
    }
}
