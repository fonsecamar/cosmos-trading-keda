using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using trading_model;

namespace order_executor.APIs
{
    public static class OrderCreate
    {
        [FunctionName("OrderCreate")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/create")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "orders",
                Connection = "CosmosDBConnection")] IAsyncCollector<Order> orderCollector,
            ILogger log)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var order = JsonConvert.DeserializeObject<Order>(requestBody);

                order.orderId = Guid.NewGuid().ToString();
                order.status = "created";
                order.createdAt = DateTime.UtcNow;

                await orderCollector.AddAsync(order);

                return new OkObjectResult(order);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message, ex);

                return new BadRequestResult();
            }
        }
    }
}
