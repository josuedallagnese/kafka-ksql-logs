using System.Text.Json;
using Confluent.Kafka;
using Infrastructure.Logging.Http.Internal;

namespace Infrastructure.Logging.Http.Kafka
{
    internal class KafkaValueSerializer : IAsyncSerializer<RequestLog>
    {
        private readonly JsonSerializerOptions _options;

        public KafkaValueSerializer()
        {
            _options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<byte[]> SerializeAsync(RequestLog data, SerializationContext _)
        {
            using var ms = new MemoryStream();

            await JsonSerializer.SerializeAsync(ms, data, _options);

            ms.Position = 0;

            return ms.ToArray();
        }
    }
}
