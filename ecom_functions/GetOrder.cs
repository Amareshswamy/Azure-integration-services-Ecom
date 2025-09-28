using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;

public class GetOrder
{
    private readonly ILogger<GetOrder> _logger;
    private readonly Container _ordersContainer;

    public GetOrder(ILogger<GetOrder> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _ordersContainer = cosmosClient.GetContainer("ECommerceDB", "orders");
    }

    [Function("GetOrder")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderId}")] HttpRequestData req,
        string orderId)
    {
        _logger.LogInformation($"GetOrder function received a request for orderId: {orderId}");

        try
        {
            // For this guest checkout flow, we use the hardcoded partition key.
            // In a real app with user logins, you would get the customerId from the user's token
            // to ensure they can only access their own orders.
            var customerId = "GUEST-CHECKOUT";

            // ReadItemAsync is the most efficient way to get a single document by its ID and partition key.
            var orderResponse = await _ordersContainer.ReadItemAsync<Order>(orderId, new PartitionKey(customerId));

            // Create a successful response and return the order
            var httpResponse = req.CreateResponse(HttpStatusCode.OK);
            await httpResponse.WriteAsJsonAsync(orderResponse.Resource);
            return httpResponse;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // If the order is not found, return a 404 error
            _logger.LogWarning($"Order with ID '{orderId}' not found.");
            return req.CreateResponse(HttpStatusCode.NotFound);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, $"Error fetching order {orderId}.");
            return req.CreateResponse(HttpStatusCode.InternalServerError);
        }
    }
}