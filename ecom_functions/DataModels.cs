using Newtonsoft.Json;
using System.Collections.Generic;

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
    [JsonProperty("stock")]
    public int StockCount { get; set; }
}
