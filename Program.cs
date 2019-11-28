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
        static async Task Main(string[] args)
        {
            Console.WriteLine("Sagan: Cosmos Pump");

            const int totalItems = 1000; // 10000;
            const int dataSizeBytes = 23000;
            const int partitionCount = 5;

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var insights = InsightsHelper.InitializeTelemetryClient(
                config //,
                       //"Examples.Pipeline.ServiceBusReceiver",
                       //$"cloudRoleInstance-{Environment.MachineName}"
                );


            string data = "";
            var random = new Random();
            for (int i = 0; i < dataSizeBytes; i++)
            {
                data += (char)random.Next(65, 90);
            }

            var now = DateTime.UtcNow;
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

            var charges = new ConcurrentBag<double>();

            using (var client = new CosmosClient(config["Cosmos:ConnectionString"]))
            {
                var container = client.GetContainer(config["Cosmos:DatabaseName"], config["Cosmos:ContainerName"]);

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                await Task.WhenAll(
                    from partition in Partitioner.Create(items).GetPartitions(partitionCount)
                    select Task.Run(async delegate
                    {
                        using (partition)
                            while (partition.MoveNext())
                            {
                                ItemResponse<Item> response = null;
                                try
                                {
                                    response = await container.CreateItemAsync(partition.Current);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine(ex.Message);
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
                    }));
                stopwatch.Stop();

                double totalRequestCharge = charges.Sum();

                Console.WriteLine($"{totalItems} created in {stopwatch.Elapsed.TotalSeconds} seconds = {totalItems / stopwatch.Elapsed.TotalSeconds} TPS");
                Console.WriteLine($"Total request charge = {totalRequestCharge} = {totalRequestCharge / stopwatch.Elapsed.TotalSeconds} RU/s");
            }
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
