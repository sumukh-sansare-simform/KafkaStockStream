using Confluent.Kafka;
using System.Text.Json;

namespace KafkaStockFlow.Kafka
{
    public class JsonSerializerHelper
    {
        public class JsonSerializer<T> : ISerializer<T>
        {
            public byte[] Serialize(T data, SerializationContext context)
            {
                return JsonSerializer.SerializeToUtf8Bytes(data);
            }
        }

        public class JsonDeserializer<T> : IDeserializer<T>
        {
            public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
            {
                return JsonSerializer.Deserialize<T>(data);
            }
        }

    }
}
