using System.ComponentModel.DataAnnotations;
using Confluent.Kafka;

namespace Infrastructure.Logging.Http
{
    public class RequestLogOptions
    {
        /// <summary>
        /// The application name. Should compatible with Kafka Topic Names.
        /// </summary>
        [Required]
        public string ApplicationName { get; set; }

        /// <summary>
        /// Status Codes ignored when request logging collect request data.
        /// Default: 400,401,403
        /// </summary>
        public IEnumerable<int> IgnoreStatusCodes { get; set; }

        /// <summary>
        /// Configure <see cref="RequestLogMode"/>: send in batch ou realtime
        /// Default: Batch
        /// </summary>
        public RequestLogMode Mode { get; set; }

        /// <summary>
        /// Configure Kafka Producer to send logs to broker.
        /// <para>Base on <see cref="ProducerConfig"/></para>
        /// <para>More informations is available on <see cref="https://github.com/confluentinc/confluent-kafka-dotnet/wiki/Producer"/></para>
        /// <para>Default value when <see cref="RequestLogMode.Batch"/> that's improve performance, but without guarantees:</para>
        /// <para>
        ///     <br>Acks=Acks.None</br>
        ///     <br>EnableDeliveryReports=false</br>
        ///     <br>LingerMs=10000</br>
        /// </para>
        /// <para>If you use <see cref="RequestLogMode.Batch"/> see the section about Optimal LingerMs <seealso cref="https://github.com/confluentinc/confluent-kafka-dotnet/wiki/Producer#optimal-lingerms-setting"/></para>
        /// </summary>
        public ProducerConfig Kafka { get; set; }

        public RequestLogOptions()
        {
            Mode = RequestLogMode.Batch;

            Kafka = new ProducerConfig()
            {
                Acks = Acks.None,
                EnableDeliveryReports = false,
                LingerMs = 10000
            };

            IgnoreStatusCodes = new[]
            {
                400,
                401,
                403
            };
        }

        public string GetTopicName() => $"request-logging_{ApplicationName}";
    }
}
