using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    public static class OrderExecutedSubscriber
    {
        static OrderExecutedSubscriber()
        {
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "orders");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("OrderExecutedSubscriber")]
        public static async Task RunAsync([EventHubTrigger("ems-ordersexecuted", Connection = "ordersHubConnection")] EventData[] events,
            ILogger log)
        {
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    var order = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(eventData.EventBody));

                    await container.PatchItemStreamAsync(order.id,
                        new PartitionKey(order.orderId),
                        new List<PatchOperation>()
                        {
                            PatchOperation.Replace("/status", order.status),
                            PatchOperation.Set("/lastModifiedAt", order.lastModifiedAt.Value)
                        }
                    );
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}
