using Confluent.Kafka;
using System;
using System.IO;
using System.Text.Json;

namespace Kafka.Sample.Producer
{
    public class GenericJsonSerializer<TEntity> : ISerializer<TEntity>, IDeserializer<TEntity>
       where TEntity : class
    {
        private readonly JsonSerializerOptions _options;

        public GenericJsonSerializer()
        {
            _options = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public TEntity Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
        {
            return JsonSerializer.Deserialize<TEntity>(data.ToArray(), _options);
        }

        public byte[] Serialize(TEntity data, SerializationContext context)
        {
            using var ms = new MemoryStream();

            string jsonString = JsonSerializer.Serialize(data, _options);
            var writer = new StreamWriter(ms);

            writer.Write(jsonString);
            writer.Flush();
            ms.Position = 0;

            return ms.ToArray();
        }
    }
}
