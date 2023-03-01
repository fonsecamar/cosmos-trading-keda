using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using trading_model;

namespace order_executor.APIs
{
    public static class GetOrderExecution
    {
        [FunctionName("GetOrderExecution")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/execution/{orderId}")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "orderExecutions",
                SqlQuery = "select * from c where c.orderId = {orderId}",
                Connection = "CosmosDBConnection")] IEnumerable<OrderExecution> orderExecutions,
            ILogger log)
        {
            if (orderExecutions == null || orderExecutions.Count() == 0)
                return new NotFoundResult();

            return new OkObjectResult(orderExecutions);
        }
    }
}
