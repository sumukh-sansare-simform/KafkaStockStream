using Confluent.SchemaRegistry;

namespace KafkaStockFlow.Kafka;

public class SchemaRegistryService
{
    public ISchemaRegistryClient CreateClient(string url)
    {
        var config = new SchemaRegistryConfig { Url = url };
        return new CachedSchemaRegistryClient(config);
    }
}