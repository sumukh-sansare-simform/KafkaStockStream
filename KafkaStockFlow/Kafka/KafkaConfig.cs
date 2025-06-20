using Microsoft.Extensions.Configuration;

namespace KafkaStockFlow.Kafka;

public class KafkaConfig
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string GroupId { get; set; } = string.Empty;
    public bool EnableIdempotence { get; set; }
    public string TransactionalId { get; set; } = string.Empty;
    public string SchemaRegistryUrl { get; set; } = string.Empty;

    public static KafkaConfig Load(IConfiguration config)
    {
        return config.GetSection("Kafka").Get<KafkaConfig>() ?? new KafkaConfig();
    }
}