using Azure.Messaging.EventGrid;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CreateShipment
{
    public class CreateShipment
    {
        private readonly ILogger<CreateShipment> _logger;
        private readonly Microsoft.Azure.Cosmos.Container _ordersContainer;

        public CreateShipment(ILogger<CreateShipment> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _ordersContainer = cosmosClient.GetContainer("ECommerceDB", "orders");
        }

        [Function("CreateShipment")]
        public async Task<ShipmentOutput> Run(
            [ServiceBusTrigger("prepare-shipment", Connection = "ServiceBusConnectionString")] string myQueueItem)
        {
            _logger.LogInformation($"CreateShipment received message: {myQueueItem}");

            dynamic data = JsonConvert.DeserializeObject(myQueueItem);
            string orderId = data.orderId;
            string customerId = data.customerId;

            try
            {
                // 1. Simulate calling an external shipping provider API (e.g., FedEx, UPS)
                var trackingNumber = $"1Z{Guid.NewGuid().ToString().Replace("-", "").ToUpper().Substring(0, 16)}";
                var carrier = "DTDC";
                _logger.LogInformation($"Generated tracking number {trackingNumber} for order {orderId}.");

                var shippingInfo = new { trackingNumber, carrier };

                // 2. Update the order in Cosmos DB with the shipping info and "Shipped" status
                await _ordersContainer.PatchItemAsync<Order>(
                    orderId,
                    new PartitionKey(customerId),
                    new[]
                    {
                    PatchOperation.Replace("/status", "Shipped"),
                    PatchOperation.Add("/shippingInfo", shippingInfo)
                    });

                _logger.LogInformation($"Successfully updated order {orderId} to Shipped.");

                // 3. Publish an "OrderShipped" event to Event Grid
                return new ShipmentOutput
                {
                    GridEvent = new EventGridEvent(
                        subject: $"orders/{orderId}",
                        eventType: "OrderShipped",
                        dataVersion: "1.0",
                        data: new { orderId, customerId, trackingNumber, carrier }
                    )
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateShipment for order {orderId}: {ex.Message}");
                throw;
            }
        }
    }

    // Helper class for the function's output
    public class ShipmentOutput
    {
        [EventGridOutput(TopicEndpointUri = "EventGridTopicEndpoint", TopicKeySetting = "EventGridTopicKey")]
        public EventGridEvent GridEvent { get; set; }
    }
    public class Order
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string OrderId { get; set; }
        public string CustomerId { get; set; }
       
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public object ShippingInfo { get; set; }
    }
}