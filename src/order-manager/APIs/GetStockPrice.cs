using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using trading_model;

namespace order_executor
{
    public static class GetStockPrice
    {
        [FunctionName("GetStockPrice")]
        public static async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "stock/{symbol}")] HttpRequest req,
            [CosmosDB(
                databaseName: "trading",
                containerName: "marketdata",
                PartitionKey = "{symbol}",
                Id = "{symbol}",
                Connection = "CosmosDBConnection")] StockPrice stock,
            ILogger log)
        {
            if (stock == null)
                return new NotFoundResult();

            return new OkObjectResult(stock);
        }
    }
}
