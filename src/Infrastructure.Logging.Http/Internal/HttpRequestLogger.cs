using System.Collections.Concurrent;
using System.Text;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Infrastructure.Logging.Http.Internal
{
    internal class HttpRequestLogger : IRequestLogger
    {
        private static readonly ConcurrentDictionary<string, bool> _shouldGenerateLogs = new ConcurrentDictionary<string, bool>();

        private readonly Channel<RequestLog> _requestLogggerChannel;
        private readonly ConcurrentBag<int> _ignoreStatusCodes;

        public HttpRequestLogger(
            Channel<RequestLog> requestLogggerChannel,
            IOptions<RequestLogOptions> options)
        {
            _requestLogggerChannel = requestLogggerChannel;
            _ignoreStatusCodes = new ConcurrentBag<int>(options.Value.IgnoreStatusCodes);
        }

        public async Task Log(RequestDelegate nextRequest, HttpContext context)
        {
            var pathId = $"{context.Request.Method}: {context.Request.Path}";

            if (!ShouldGenerateLog(context, pathId))
            {
                await nextRequest(context);
                return;
            }

            var originalBodyStream = context.Response.Body;

            using var ms = new MemoryStream();

            var requestBody = await ReadRequestBody(context.Request);

            context.Response.Body = ms;

            await nextRequest(context);

            var responseBody = await ReadResponseBody(context.Response);

            await ms.CopyToAsync(originalBodyStream);

            if (!_ignoreStatusCodes.Contains(context.Response.StatusCode))
            {
                var log = RequestLog.Create(context, pathId, requestBody, responseBody);

                await _requestLogggerChannel.Writer.WriteAsync(log);
            }
        }

        public static async Task<string> ReadRequestBody(HttpRequest request)
        {
            if (!request.ContentLength.HasValue)
                return string.Empty;

            request.EnableBuffering();

            request.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
            var responseBody = await streamReader.ReadToEndAsync();
            request.Body.Seek(0, SeekOrigin.Begin);

            return responseBody;
        }

        public static async Task<string> ReadResponseBody(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            using var streamReader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
            var responseBody = await streamReader.ReadToEndAsync();
            response.Body.Seek(0, SeekOrigin.Begin);

            return responseBody;
        }

        public static bool ShouldGenerateLog(HttpContext context, string pathId)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null)
                return false;

            if (_shouldGenerateLogs.ContainsKey(pathId))
                return _shouldGenerateLogs[pathId];

            var should = endpoint.Metadata.OfType<RequestLogAttribute>().FirstOrDefault() != null;

            _shouldGenerateLogs.TryAdd(pathId, should);

            return should;
        }
    }
}
