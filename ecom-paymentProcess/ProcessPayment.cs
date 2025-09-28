using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

public class ProcessPayment
{
    private readonly ILogger<ProcessPayment> _logger;
    private readonly Container _ordersContainer;

    public ProcessPayment(ILogger<ProcessPayment> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _ordersContainer = cosmosClient.GetContainer("ECommerceDB", "orders");
        _logger.LogInformation("ProcessPayment function initialized successfully.");
    }

    [Function("ProcessPayment")]
    public async Task<PaymentOutput> Run(
        [ServiceBusTrigger("process-payment", Connection = "ServiceBusConnectionString")] string myQueueItem)
    {
        _logger.LogInformation($"--- Starting ProcessPayment execution ---");
        _logger.LogInformation($"Received message: {myQueueItem}");

        string orderId = null; // Initialize to null
        try
        {
            _logger.LogInformation("Step 1: Deserializing message...");
            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            orderId = data.orderId;
            string customerId = data.customerId;
            _logger.LogInformation($"Successfully deserialized. OrderId: {orderId}, CustomerId: {customerId}");

            _logger.LogInformation($"Step 2: Fetching order '{orderId}' from Cosmos DB...");
            var orderResponse = await _ordersContainer.ReadItemAsync<Order>(orderId, new PartitionKey(customerId));
            Order order = orderResponse.Resource;
            _logger.LogInformation($"Successfully fetched order '{orderId}'.");

            if (order == null)
            {
                _logger.LogError($"Order '{orderId}' not found in the database.");
                return null; // Stop processing if order not found
            }

            // --- Simulate Payment Gateway Call ---
            bool paymentSuccessful = true; // For testing, assume success

            if (paymentSuccessful)
            {
                _logger.LogInformation($"Step 3: Payment successful for order '{orderId}'.");

                _logger.LogInformation($"Step 4: Updating order status to 'Paid'...");
                await _ordersContainer.PatchItemAsync<Order>(orderId, new PartitionKey(customerId), new[] { PatchOperation.Replace("/status", "Paid") });
                _logger.LogInformation($"Successfully updated order status.");

                _logger.LogInformation($"Step 5: Preparing outputs for Service Bus and Event Grid...");
                var output = new PaymentOutput
                {
                    ShipmentMessage = JsonConvert.SerializeObject(new { orderId, customerId }),
                    GridEvent = new EventGridEvent(
                        subject: $"orders/{orderId}",
                        eventType: "OrderPaid",
                        dataVersion: "1.0",
                        data: new { orderId, customerId }
                    )
                };
                _logger.LogInformation("--- ProcessPayment execution finished successfully. ---");
                return output;
            }
            else
            {
                // Handle payment failure logic here...
                return null;
            }
        }
        catch (Exception ex)
        {
            // This will now log the specific error before the function fails.
            _logger.LogError(ex, $"An error occurred in ProcessPayment for orderId '{orderId ?? "unknown"}'. Exception: {ex.Message}");
            throw;


        }
    }

    public class PaymentOutput
    {
        [ServiceBusOutput("prepare-shipment", Connection = "ServiceBusConnectionString")]
        public string ShipmentMessage { get; set; }

        [EventGridOutput(TopicEndpointUri = "EventGridTopicEndpoint", TopicKeySetting = "EventGridTopicKey")]
        public EventGridEvent GridEvent { get; set; }
    }

}