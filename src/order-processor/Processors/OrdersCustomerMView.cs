using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    public static class OrdersCustomerMView
    {
        static OrdersCustomerMView()
        {
            cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"), new CosmosClientOptions() { AllowBulkExecution = true });
            container = cosmosClient.GetContainer("trading", "customerPortfolio");
        }

        static CosmosClient cosmosClient;
        static Container container;

        [FunctionName("OrdersCustomerMView")]
        public static async Task RunAsync([CosmosDBTrigger(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection",
                LeaseContainerName = "leases",
                LeaseContainerPrefix = "customer-portfolio-",
                FeedPollDelay = 5000,
                MaxItemsPerInvocation = 100,
                CreateLeaseContainerIfNotExists = true)]IReadOnlyList<Order> input,
            ILogger log)
        {
            await Parallel.ForEachAsync(input, async (order, token) =>
            {
                if (order.status != "executed")
                    return;

                try
                {
                    CustomerPortfolio portfolio = null;

                    PartitionKey partitionKey = new PartitionKeyBuilder()
                         .Add(order.customerId)
                         .Add(order.assetClass)
                         .Build();

                    using (var response = await container.ReadItemStreamAsync($"{order.customerId}_{order.symbol}", partitionKey))
                    {
                        if (response.StatusCode != HttpStatusCode.NotFound)
                        {
                            JsonSerializer serializer = new JsonSerializer();
                            using (StreamReader streamReader = new StreamReader(response.Content))
                            using (var reader = new JsonTextReader(streamReader))
                            {
                                portfolio = serializer.Deserialize<CustomerPortfolio>(reader);
                            }
                        }
                    }

                    if (portfolio == null)
                    {
                        portfolio = new CustomerPortfolio()
                        {
                            symbol = order.symbol,
                            customerId = order.customerId,
                            assetClass = order.assetClass,
                            createdAt = order.createdAt,
                            quantity = order.action == "sell" ? -order.quantity : order.quantity,
                            price = order.price
                        };

                        await container.CreateItemAsync(portfolio, partitionKey);
                    }
                    else
                    {
                        var operations = new List<PatchOperation>()
                        {
                            PatchOperation.Increment("/quantity", order.action == "sell" ? -order.quantity : order.quantity),
                            PatchOperation.Add("/lastModifiedAt", order.lastModifiedAt.Value)
                        };

                        if (order.action == "buy")
                            operations.Add(PatchOperation.Set("/price", Math.Round((portfolio.position + order.price * order.quantity) / (portfolio.quantity + order.quantity), 2)));
                        else if (portfolio.quantity == order.quantity)
                            operations.Add(PatchOperation.Add("/ttl", 86400));

                        await container.PatchItemAsync<CustomerPortfolio>(portfolio.id, partitionKey, operations);
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}