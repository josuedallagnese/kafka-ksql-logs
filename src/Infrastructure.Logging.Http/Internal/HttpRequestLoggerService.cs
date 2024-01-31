using System.Threading.Channels;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Logging.Http.Internal
{
    internal class HttpRequestLoggerService : BackgroundService
    {
        private readonly Channel<RequestLog> _requestLogChannel;
        private readonly IProducer<string, RequestLog> _kafkaProducer;
        private readonly ILogger _logger;

        private readonly string _topicName;
        private readonly RequestLogMode _mode;

        public HttpRequestLoggerService(
            Channel<RequestLog> requestLogChannel,
            IProducer<string, RequestLog> kafkaProducer,
            IOptions<RequestLogOptions> options,
            ILogger<HttpRequestLoggerService> logger)
        {
            _requestLogChannel = requestLogChannel;
            _kafkaProducer = kafkaProducer;
            _logger = logger;

            _topicName = options.Value.GetTopicName();
            _mode = options.Value.Mode;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var item in _requestLogChannel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var message = new Message<string, RequestLog>()
                    {
                        Key = item.Timestamp,
                        Value = item
                    };

                    if (_mode == RequestLogMode.Batch)
                        _kafkaProducer.Produce(_topicName, message);
                    else
                        await _kafkaProducer.ProduceAsync(_topicName, message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error to send request logs to kafka");
                }
            }
        }
    }
}
