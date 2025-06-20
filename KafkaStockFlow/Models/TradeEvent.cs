namespace KafkaStockFlow.Models
{
    public class TradeEvent
    {
        public string TradeId { get; set; } = Guid.NewGuid().ToString();
        public string Symbol { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public double Price { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
