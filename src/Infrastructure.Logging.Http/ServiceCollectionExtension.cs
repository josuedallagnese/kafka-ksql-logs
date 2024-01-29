using System.Net.Mime;
using System.Threading.Channels;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Infrastructure.Logging.Http.Internal;
using Infrastructure.Logging.Http.Kafka;
using Infrastructure.Logging.Http.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Logging.Http
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddRequestLogging(this IServiceCollection services, IConfiguration configuration, string configurationSectionName = "RequestLogging")
        {
            var configurationSection = configuration.GetRequiredSection(configurationSectionName);

            services.AddOptions<RequestLogOptions>(configurationSectionName)
                .Bind(configurationSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.Configure<RequestLogOptions>(configurationSection);

            services.AddSingleton(_ => Channel.CreateUnbounded<RequestLog>(new UnboundedChannelOptions
            {
                SingleReader = true
            }));

            services.AddSingleton<IRequestLogger, HttpRequestLogger>();
            services.AddHostedService<HttpRequestLoggerService>();

            services.AddSingleton(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ProducerBuilder<string, RequestLog>>>();
                var options = sp.GetRequiredService<IOptions<RequestLogOptions>>();

                var requestLoggerProducerBuilder = new ProducerBuilder<string, RequestLog>(options.Value.Kafka)
                    .SetKeySerializer(Serializers.Utf8)
                    .SetLogHandler((_, message) => logger.LogInformation($"Facility: {message.Facility} - {message.Level} Message: {message.Message}"))
                    .SetErrorHandler((_, e) => logger.LogError($"Error to send request logging to kafka: {e.Reason}"));

                if (options.Value.Mode == RequestLogMode.Batch)
                    requestLoggerProducerBuilder.SetValueSerializer(new KafkaValueSerializer().AsSyncOverAsync());
                else
                    requestLoggerProducerBuilder.SetValueSerializer(new KafkaValueSerializer());

                return requestLoggerProducerBuilder.Build();
            });

            return services;
        }

        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<RequestLoggingMiddleware>();

            builder.UseExceptionHandler(app =>
            {
                app.Run(async context =>
                {
                    var exception = context.Features.Get<IExceptionHandlerPathFeature>();

                    var errorResponse = new
                    {
                        error = "Sorry about that. Wait a moment and try again...",
                        detail = exception.Error.ToString()
                    };

                    context.Response.ContentType = MediaTypeNames.Application.Json;
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                    await context.Response.WriteAsJsonAsync(errorResponse);
                });
            });

            return builder;
        }
    }
}
