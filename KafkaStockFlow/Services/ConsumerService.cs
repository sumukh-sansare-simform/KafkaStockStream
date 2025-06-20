using Confluent.Kafka;
using KafkaStockFlow.Models;
using Microsoft.Extensions.Logging;

namespace KafkaStockFlow.Services
{
    public class ConsumerService
    {
        private readonly IConsumer<string, TradeEvent> _consumer;
        private readonly string _topic;
        private readonly ILogger<ConsumerService> _logger;
        private readonly string _folderPath;

        public ConsumerService(
            IConsumer<string, TradeEvent> consumer,
            string topic,
            ILogger<ConsumerService> logger,
            string folderPath)
        {
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _topic = topic ?? throw new ArgumentNullException(nameof(topic));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));

            // Ensure the folder exists or create it
            if (!Directory.Exists(_folderPath))
            {
                Directory.CreateDirectory(_folderPath);
            }
        }

        public void StartConsuming(CancellationToken cancellationToken)
        {
            _consumer.Subscribe(_topic);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var result = _consumer.Consume(cancellationToken);
                    if (result != null)
                    {
                        var trade = result.Message.Value;

                        // Log consumed message
                        _logger.LogInformation("Consumed trade: {Symbol} {Side} {Quantity} @ {Price}",
                            trade.Symbol, trade.Side, trade.Quantity, trade.Price);

                        // Write the trade to the file based on current date
                        WriteTradeToFile(trade);

                        // Manual commit for demonstration
                        _consumer.Commit(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error during consume: {Message}", ex.Message);
            }
            finally
            {
                _consumer.Close();
            }
        }

        // Method to write a TradeEvent to a file based on the current date
        private void WriteTradeToFile(TradeEvent trade)
        {
            try
            {
                // Get the current date to create a date-wise file name
                string dateString = DateTime.UtcNow.ToString("yyyy-MM-dd");
                string filePath = Path.Combine(_folderPath, $"consumed_trades_{dateString}.csv");

                // If file does not exist, create it and add header
                if (!File.Exists(filePath))
                {
                    using var writer = new StreamWriter(filePath);
                    writer.WriteLine("Symbol,Side,Quantity,Price,Timestamp"); // Add header for CSV
                }

                // Append the trade data to the file
                using var appendWriter = new StreamWriter(filePath, append: true);
                appendWriter.WriteLine($"{trade.Symbol},{trade.Side},{trade.Quantity},{trade.Price},{trade.Timestamp:O}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error writing trade to file: {Message}", ex.Message);
            }
        }
    }
}
