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
        private readonly IOptions<RequestLogOptions> _options;
        private readonly ILogger _logger;

        public HttpRequestLoggerService(
            Channel<RequestLog> requestLogChannel,
            IProducer<string, RequestLog> kafkaProducer,
            IOptions<RequestLogOptions> options,
            ILogger<HttpRequestLoggerService> logger)
        {
            _requestLogChannel = requestLogChannel;
            _kafkaProducer = kafkaProducer;
            _options = options;
            _logger = logger;
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

                    if (_options.Value.Mode == RequestLogMode.Batch)
                        _kafkaProducer.Produce(_options.Value.ApplicationName, message);
                    else
                        await _kafkaProducer.ProduceAsync(_options.Value.ApplicationName, message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error to send request logs to kafka");
                }
            }
        }
    }
}
