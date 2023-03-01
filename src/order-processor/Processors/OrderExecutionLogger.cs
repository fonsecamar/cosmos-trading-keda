using Azure.Messaging.EventHubs;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    public static class OrderExecutionLogger
    {
        static OrderExecutionLogger()
        {
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "orderExecutions");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("OrderExecutedLogger")]
        public static async Task Run([EventHubTrigger("ems-executions", Connection = "ordersHubConnection")] EventData[] events, ILogger log)
        {
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    var execution = JsonConvert.DeserializeObject<OrderExecution>(Encoding.UTF8.GetString(eventData.EventBody));

                    await container.CreateItemAsync(execution, new PartitionKey(execution.orderId));
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}
