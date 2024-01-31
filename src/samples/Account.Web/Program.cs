using System.Net;
using Account.Web.Models;
using Infrastructure.Logging;
using Infrastructure.Logging.Http;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRequestLogging(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseRequestLogging();

app.MapGet("/api/account/{id}", [RequestLog] (Guid id, [FromQuery] string name, [FromQuery] string mail, [FromQuery] bool error) =>
{
    if (error)
    {
        var innerException = new Exception("This is a error simulation");
        throw new Exception("Root error simulation", innerException);
    }

    var result = new UserResult()
    {
        Id = id,
        Name = name,
        Mail = mail
    };

    return result;

}).Produces<UserResult>();

app.MapPost("/api/account", [RequestLog] (CreateUserCommand command, [FromHeader(Name = "api-key")] string key) =>
{
    var result = new UserResult()
    {
        Id = Guid.NewGuid(),
        Name = command.Name,
        Mail = command.Mail
    };

    var error = DateTime.Now.Second > 30;

    return Results.Created($"/api/account/{result.Id}?name={WebUtility.UrlEncode(result.Name)}&mail={WebUtility.UrlEncode(result.Mail)}&error={error}", result);
});

app.Run();
