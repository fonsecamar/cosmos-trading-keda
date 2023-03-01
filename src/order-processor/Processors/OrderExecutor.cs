using Azure.Messaging.EventHubs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.Processors
{
    public static class OrderExecutor
    {
        [FunctionName("OrderExecutor")]
        public static async Task Run([EventHubTrigger("ems-orderstoexecute", Connection = "ordersHubConnection")] EventData[] events,
            [EventHub("ems-executions", Connection = "ordersHubConnection")] IAsyncCollector<OrderExecution> outputExecutions,
            [EventHub("ems-ordersexecuted", Connection = "ordersHubConnection")] IAsyncCollector<Order> outputOrders,
            ILogger log)
        {
            await Parallel.ForEachAsync(events, async (eventData, token) =>
            {
                try
                {
                    var order = JsonConvert.DeserializeObject<Order>(Encoding.UTF8.GetString(eventData.EventBody));
                    int _quantity = order.quantity;

                    while (_quantity > 0)
                    {
                        var partialQuantity = _quantity <= 100 ? _quantity : Random.Shared.Next(100, _quantity);
                        _quantity -= partialQuantity;

                        var execution = new OrderExecution()
                        {
                            id = Guid.NewGuid().ToString(),
                            orderId = order.orderId,
                            quantity = partialQuantity,
                            customerId = order.customerId,
                            action = order.action,
                            price = order.price,
                            executedAt = DateTime.UtcNow,
                            symbol = order.symbol
                        };

                        await outputExecutions.AddAsync(execution);
                    }

                    order.status = "executed";
                    order.lastModifiedAt = DateTime.UtcNow;

                    await outputOrders.AddAsync(order);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message, ex);
                }
            });
        }
    }
}