using Microsoft.AspNetCore.Http;

namespace Infrastructure.Logging.Http.Middlewares
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _nextRequest;
        private readonly IRequestLogger _requestLogger;

        public RequestLoggingMiddleware(RequestDelegate nextRequest, IRequestLogger requestLogger)
        {
            _nextRequest = nextRequest;
            _requestLogger = requestLogger;
        }

        public Task Invoke(HttpContext context) => _requestLogger.Log(_nextRequest, context);
    }
}
