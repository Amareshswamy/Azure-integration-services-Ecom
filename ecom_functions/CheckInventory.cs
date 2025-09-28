using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class CheckInventory
{
    private readonly ILogger<CheckInventory> _logger;
    private readonly Container _ordersContainer;
    private readonly Container _productsContainer;

    public CheckInventory(ILogger<CheckInventory> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _ordersContainer = cosmosClient.GetContainer("ECommerceDB", "orders");
        _productsContainer = cosmosClient.GetContainer("ECommerceDB", "products");
    }

    [Function("CheckInventory")]
    public async Task<CheckInventoryOutput> Run(
        [ServiceBusTrigger("new-orders", Connection = "ServiceBusConnectionString")] string myQueueItem)
    {
        _logger.LogInformation($"CheckInventory received message: {myQueueItem}");

        try
        {
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            string orderId = data.orderId;
            string customerId = data.customerId;

            var orderResponse = await _ordersContainer.ReadItemAsync<Order>(orderId, new PartitionKey(customerId));
            Order order = orderResponse.Resource;

            bool allItemsInStock = true;
            foreach (var item in order.LineItems)
            {
                var productResponse = await _productsContainer.ReadItemAsync<Product>(item.ProductId, new PartitionKey(item.ProductId));
                if (productResponse.Resource.StockCount < item.Quantity)
                {
                    allItemsInStock = false;
                    _logger.LogError($"OUT OF STOCK for ProductId: {item.ProductId}.");
                    break;
                }
            }

            if (allItemsInStock)
            {
                // --- THIS LOOP WAS ADDED ---
                _logger.LogInformation($"All items in stock. Decrementing stock counts for order {orderId}.");
                foreach (var item in order.LineItems)
                {
                    await _productsContainer.PatchItemAsync<Product>(
                        item.ProductId,
                        new PartitionKey(item.ProductId),
                        new[] { PatchOperation.Increment("/stock", -item.Quantity) }
                    );
                }
                // --------------------------

                _logger.LogInformation("Updating order status to 'AwaitingPayment'.");
                await _ordersContainer.PatchItemAsync<Order>(
                    orderId,
                    new PartitionKey(customerId),
                    new[] { PatchOperation.Replace("/status", "AwaitingPayment") }
                );

                _logger.LogInformation($"Inventory check successful for order {orderId}. Message sent to payment queue.");

                return new CheckInventoryOutput
                {
                    PaymentMessage = JsonConvert.SerializeObject(new { orderId, customerId })
                };
            }
            else // If any item was out of stock
            {
                await _ordersContainer.PatchItemAsync<Order>(
                    orderId,
                    new PartitionKey(customerId),
                    new[] { PatchOperation.Replace("/status", "Failed_OutOfStock") }
                );

                return new CheckInventoryOutput
                {

                    GridEvent = new EventGridEvent(
                        subject: $"orders/{orderId}",
                        eventType: "OrderFailed",
                        dataVersion: "1.0",
                        data: new { orderId, customerId, failureReason = "Item out of stock" }
                    )
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred in CheckInventory: {ex.Message}");
            throw;
        }
    }
}

// The output class is updated to handle both Service Bus and Event Grid
public class CheckInventoryOutput
{
    [ServiceBusOutput("process-payment", Connection = "ServiceBusConnectionString")]
    public string PaymentMessage { get; set; }

    [EventGridOutput(TopicEndpointUri = "EventGridTopicUri", TopicKeySetting = "EventGridTopicKey")]
    public EventGridEvent GridEvent { get; set; }
}