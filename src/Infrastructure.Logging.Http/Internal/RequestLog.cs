using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Logging.Http.Internal
{
    internal class RequestLog
    {
        public string Timestamp { get; set; }
        public string Path { get; set; }
        public int StatusCode { get; set; }
        public string QueryString { get; set; }
        public List<string> Headers { get; set; }
        public JsonDocument Request { get; set; }
        public JsonDocument Response { get; set; }

        public static RequestLog Create(HttpContext context, string pathId, string requestBody, string responseBody)
        {
            var headers = context.Request.Headers.Select(s => $"{s.Key}:{s.Value}").ToList();

            var log = new RequestLog()
            {
                Headers = headers,
                Path = pathId,
                QueryString = context.Request.QueryString.ToString(),
                Request = string.IsNullOrWhiteSpace(requestBody) ? null : JsonDocument.Parse(requestBody),
                Response = string.IsNullOrWhiteSpace(responseBody) ? null : JsonDocument.Parse(responseBody),
                StatusCode = context.Response.StatusCode,
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            return log;
        }
    }
}
