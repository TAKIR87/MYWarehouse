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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            InvalidOperationException ex => (HttpStatusCode.BadRequest,       ex.Message),
            KeyNotFoundException ex      => (HttpStatusCode.NotFound,          ex.Message),
            ArgumentException ex         => (HttpStatusCode.BadRequest,        ex.Message),
            UnauthorizedAccessException  => (HttpStatusCode.Unauthorized,      "Нет доступа"),
            _                            => (HttpStatusCode.InternalServerError, "Внутренняя ошибка сервера")
        };

        // ─── Логирование по уровням ────────────────────────────────────────────
        if (statusCode == HttpStatusCode.InternalServerError)
        {
            // Неожиданные ошибки — CRITICAL: падение БД, OutOfMemory и т.д.
            _logger.LogCritical(exception,
                "КРИТИЧЕСКАЯ ОШИБКА [{TraceId}] {Method} {Path} → {StatusCode}: {Message}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                (int)statusCode,
                exception.Message);
        }
        else if (statusCode == HttpStatusCode.BadRequest)
        {
            // Бизнес-ошибки (нехватка остатка, дубль артикула) — WARNING
            _logger.LogWarning(
                "Бизнес-ошибка [{TraceId}] {Method} {Path} → {StatusCode}: {Message}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                (int)statusCode,
                exception.Message);
        }
        else if (statusCode == HttpStatusCode.NotFound)
        {
            // Ресурс не найден — INFO (штатная ситуация)
            _logger.LogInformation(
                "Ресурс не найден [{TraceId}] {Method} {Path}: {Message}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                exception.Message);
        }
        else
        {
            // Всё остальное (401, 403 и т.д.) — ERROR
            _logger.LogError(exception,
                "Ошибка [{TraceId}] {Method} {Path} → {StatusCode}: {Message}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                (int)statusCode,
                exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode  = (int)statusCode;

        var response = new
        {
            status  = (int)statusCode,
            message,
            path    = context.Request.Path.ToString(),
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}