using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sagan
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Sagan: Cosmos Pump");

            const int totalItems = 1; // 10000;
            const int dataSizeBytes = 23000;

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            var client = new CosmosClient(config["Cosmos:ConnectionString"]);
            var container = client.GetContainer(config["Cosmos:DatabaseName"], config["Cosmos:ContainerName"]);

            string data = "";
            var random = new Random();
            for (int i = 0; i < dataSizeBytes; i++)
            {
                data += (char)random.Next(32, 128);
            }

            var item = new Item
            {
                Data = data,
                DateTime = DateTime.UtcNow
            };

            for (int i = 0;  i < totalItems; i++)
            {
                item.ItemCount = i;
                item.Id = Guid.NewGuid().ToString("N");

                var response = await container.CreateItemAsync(item);

                Console.WriteLine($"Item {i} request charge = {response.RequestCharge}");
            }
        }
    }

    class Item
    {
        public string Id { get; set; }
        public string Data { get; set; }
        public int ItemCount { get; set; }
        public DateTime DateTime { get; set; }
    }
}
