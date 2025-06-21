using Confluent.Kafka;
using KafkaStockFlow.Kafka;
using KafkaStockFlow.Models;
using KafkaStockFlow.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static KafkaStockFlow.Kafka.JsonSerializerHelper;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();

        var kafkaConfig = KafkaConfig.Load(config);

        // Fetch API keys and symbols from config
        string apiKey = config["AlphaVantage:ApiKey"];  // Alpha Vantage API Key
        string symbol = config["AlphaVantage:Symbol"];  // Stock Symbol

        // Setup DI and logging
        var services = new ServiceCollection()
            .AddLogging(builder => builder.AddConsole())
            .BuildServiceProvider();

        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var loggerProducer = loggerFactory.CreateLogger<ProducerService>();
        var loggerConsumer = loggerFactory.CreateLogger<ConsumerService>();

        // Producer config
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            EnableIdempotence = true, // Ensures idempotent writes
        };

        // Consumer config
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
            GroupId = kafkaConfig.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false // Disable auto-commit to manually control offsets
        };

        // Producer and Consumer builders
        var producer = new ProducerBuilder<string, TradeEvent>(producerConfig)
            .SetValueSerializer(new JsonSerializer<TradeEvent>())
            .Build();

        var consumer = new ConsumerBuilder<string, TradeEvent>(consumerConfig)
            .SetValueDeserializer(new JsonDeserializer<TradeEvent>())
            .Build();

        // Simple way to get the project root directory
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));

        // Combine with the desired folder name (e.g., "ConsumedTrades")
        string folderPath = Path.Combine(projectRoot, "ConsumedTrades");

        // Ensure the folder exists or create it
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        // Services
        var producerService = new ProducerService(producer, kafkaConfig.Topic, loggerProducer, producerConfig);
        var consumerService = new ConsumerService(consumer, kafkaConfig.Topic, loggerConsumer, folderPath);

        // Start continuously ingesting data from Alpha Vantage
        using var cts = new CancellationTokenSource();
        var ingestionTask = Task.Run(() => producerService.IngestFromAlphaVantageAsync(symbol, apiKey, cts.Token));

        // Allow some time for messages to be produced before consuming
        await Task.Delay(5000); // You can adjust this delay if necessary

        // Start consuming asynchronously
        var consumerTask = Task.Run(() => consumerService.StartConsuming(cts.Token));

        // Let the consumer run continuously
        Console.WriteLine("Press any key to stop the consumer...");
        Console.ReadKey();

        // Stop the consumer gracefully when the key is pressed
        cts.Cancel();
        await Task.WhenAll(ingestionTask, consumerTask);

        Console.WriteLine("Demo complete.");
    }
}
