using System.Text.Json;
using Confluent.Kafka;
using KafkaStockFlow.Models;
using Microsoft.Extensions.Logging;

namespace KafkaStockFlow.Services
{
    public class ProducerService
    {
        private readonly IProducer<string, TradeEvent> _producer;
        private readonly string _topic;
        private readonly ILogger<ProducerService> _logger;
        private readonly ProducerConfig _producerConfig;

        public ProducerService(IProducer<string, TradeEvent> producer, string topic, ILogger<ProducerService> logger, ProducerConfig producerConfig)
        {
            _producer = producer;
            _topic = topic;
            _logger = logger;
            _producerConfig = producerConfig;
        }

        // Ingest from Alpha Vantage and send messages in batches to Kafka
        public async Task IngestFromAlphaVantageAsync(string symbol, string apiKey, CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=1min&apikey={apiKey}";

            var tradeBatch = new List<Message<string, TradeEvent>>(); // Batch collection

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await httpClient.GetStringAsync(url, cancellationToken);
                    var doc = JsonDocument.Parse(response);

                    if (doc.RootElement.TryGetProperty("Time Series (1min)", out var timeSeries))
                    {
                        foreach (var property in timeSeries.EnumerateObject())
                        {
                            var time = DateTime.Parse(property.Name);
                            var tradeJson = property.Value;

                            double price = double.Parse(tradeJson.GetProperty("1. open").GetString()!);
                            var trade = new TradeEvent
                            {
                                Symbol = symbol,
                                Side = (new Random().Next(2) == 0) ? "Buy" : "Sell",
                                Quantity = new Random().Next(1, 100),
                                Price = price,
                                Timestamp = time
                            };

                            // Prepare message
                            var message = new Message<string, TradeEvent>
                            {
                                Key = trade.Symbol,
                                Value = trade
                            };

                            tradeBatch.Add(message);

                            // If batch reaches a certain size, produce to Kafka
                            if (tradeBatch.Count >= 10)
                            {
                                await ProduceBatchAsync(tradeBatch);
                                tradeBatch.Clear();
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No time series data found in API response.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error fetching/parsing Alpha Vantage data: {Message}", ex.Message);
                }

                // Wait before polling again (Alpha Vantage free tier: 5 requests/minute)
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        // Produce a batch of messages to Kafka with partitioning logic
        public async Task ProduceBatchAsync(List<Message<string, TradeEvent>> tradeBatch)
        {
            try
            {
                var deliveryReports = new List<Task<DeliveryResult<string, TradeEvent>>>();

                foreach (var tradeMessage in tradeBatch)
                {
                    // Determine the partition based on the symbol hash
                    int partition = GetPartitionForKey(tradeMessage.Key);

                    // Produce messages asynchronously to the determined partition
                    deliveryReports.Add(_producer.ProduceAsync(new TopicPartition(_topic, new Partition(partition)), tradeMessage));
                }

                // Wait for all delivery results
                await Task.WhenAll(deliveryReports);
                _logger.LogInformation("Batch of {Count} trades successfully delivered", tradeBatch.Count);
            }
            catch (ProduceException<string, TradeEvent> ex)
            {
                _logger.LogError("Batch delivery failed: {Reason}, Error: {ErrorDetails}", ex.Error.Reason, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error during produce batch: {Message}", ex.Message);
            }
        }

        // Method to assign partition based on the symbol hash
        private static int GetPartitionForKey(string key)
        {
            // Ensure the key is not null and apply hash-based partitioning logic
            const int partitionCount = 3;  // Set this to your topic's partition count
            int partition = Math.Abs(key.GetHashCode()) % partitionCount;
            return partition;
        }
    }
}
