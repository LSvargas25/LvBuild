using System.Net;
using System.Text.Json;
using LvApplication.Common.Exceptions;

namespace LvApi.Middleware;

public partial class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            LogUnhandledException(_logger, exception, context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Unhandled exception occurred while processing {Method} {Path}"
    )]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        PathString path
    );

    /// <summary>
    /// Every error has the same JSON shape: <c>{ statusCode, code, message }</c>. The message is
    /// in Spanish and meant for the end user; <c>code</c> is stable so clients can branch on it
    /// (not_found, validation, forbidden, conflict, unexpected).
    /// </summary>
    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, code, message) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "not_found", exception.Message),
            ValidationAppException => (HttpStatusCode.BadRequest, "validation", exception.Message),
            ForbiddenException => (HttpStatusCode.Forbidden, "forbidden", exception.Message),
            ConflictException => (HttpStatusCode.Conflict, "conflict", exception.Message),
            _ => (
                HttpStatusCode.InternalServerError,
                "unexpected",
                "Ocurrió un error inesperado. Intente de nuevo más tarde."
            ),
        };

        return WriteErrorAsync(context, statusCode, code, message);
    }

    private static Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string code,
        string message
    )
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var payload = JsonSerializer.Serialize(
            new
            {
                statusCode = (int)statusCode,
                code,
                message,
            }
        );

        return context.Response.WriteAsync(payload);
    }
}
