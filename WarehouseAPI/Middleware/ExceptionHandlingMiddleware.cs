using System.Net;
using System.Text.Json;

namespace WarehouseAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанное исключение: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            InvalidOperationException ex => (HttpStatusCode.BadRequest,      ex.Message),
            KeyNotFoundException ex      => (HttpStatusCode.NotFound,         ex.Message),
            ArgumentException ex         => (HttpStatusCode.BadRequest,       ex.Message),
            UnauthorizedAccessException  => (HttpStatusCode.Unauthorized,     "Нет доступа"),
            _                            => (HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var response = new
        {
            status  = (int)statusCode,
            message,
            path    = context.Request.Path.ToString(),
            traceId = context.TraceIdentifier   // удобно для отладки
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}