using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace ECommerce.Functions
{
    public class GetProducts
    {
        private readonly ILogger<GetProducts> _logger;
        private readonly Container _productsContainer;

        public GetProducts(ILogger<GetProducts> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _productsContainer = cosmosClient.GetContainer("ECommerceDB", "products");
        }

        [Function("GetProducts")]
        public async Task<HttpResponseData> Run(
            // The HttpTrigger attribute was missing. It is now restored.
            [Microsoft.Azure.Functions.Worker.HttpTrigger(Microsoft.Azure.Functions.Worker.AuthorizationLevel.Anonymous, "get", Route = "products")] HttpRequestData req)
        {
            _logger.LogInformation("GetProducts function received a request.");

            try
            {
                var productList = new List<Product>();
                var query = new QueryDefinition("SELECT * FROM c");

                using (var feed = _productsContainer.GetItemQueryIterator<Product>(query))
                {
                    while (feed.HasMoreResults)
                    {
                        var response = await feed.ReadNextAsync();
                        foreach (var product in response)
                        {
                            productList.Add(product);
                        }
                    }
                }

                var httpResponse = req.CreateResponse(HttpStatusCode.OK);
                await httpResponse.WriteAsJsonAsync(productList);
                return httpResponse;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error fetching products from Cosmos DB.");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }

    // --- Data Models ---
    // It's a better practice to have these in a separate DataModels.cs file,
    // but they can be here as well.

    public class Order
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
        [JsonProperty("items")]
        public List<LineItem> LineItems { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public object ShippingInfo { get; set; }
    }

    public class LineItem
    {
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class Product
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public decimal UnitPrice { get; set; }
        [JsonProperty("stock")]
        public int StockCount { get; set; }
        public string ImageUrl { get; set; }
    }
}