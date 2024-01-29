using Microsoft.AspNetCore.Http;

namespace Infrastructure.Logging.Http
{
    public interface IRequestLogger
    {
        Task Log(RequestDelegate nextRequest, HttpContext context);
    }
}
