using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using KafkaStockFlow.Kafka;
using KafkaStockFlow.Models;
using Microsoft.Extensions.Logging;

namespace KafkaStockFlow.Services;

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

    public async Task IngestFromAlphaVantageAsync(string symbol, string apiKey, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_INTRADAY&symbol={symbol}&interval=1min&apikey={apiKey}";

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

                        await ProduceTradeAsync(trade);
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

    // ProduceTradeAsync method implementation
    public async Task ProduceTradeAsync(TradeEvent trade)
    {
        if (trade == null)
        {
            _logger.LogError("TradeEvent is null");
            return;
        }

        if (string.IsNullOrEmpty(trade.Symbol) || string.IsNullOrEmpty(trade.Side))
        {
            _logger.LogError("TradeEvent properties are not properly set");
            return;
        }

        var message = new Message<string, TradeEvent>
        {
            Key = trade.Symbol,
            Value = trade
        };

        try
        {
            var deliveryResult = await _producer.ProduceAsync(_topic, message);
            _logger.LogInformation("Delivered '{Symbol}' to '{PartitionOffset}'", deliveryResult.Value.Symbol, deliveryResult.TopicPartitionOffset);
        }
        catch (ProduceException<string, TradeEvent> ex)
        {
            _logger.LogError("Delivery failed: {Reason}, Error: {ErrorDetails}", ex.Error.Reason, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError("Unexpected error during produce: {Message}", ex.Message);
        }
    }
}