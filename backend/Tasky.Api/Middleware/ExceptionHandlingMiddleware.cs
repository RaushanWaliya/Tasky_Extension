using System.Text.Json;
using Tasky.Api.Exceptions;

namespace Tasky.Api.Middleware;

/// <summary>
/// Catches every error in one place and turns it into a clean JSON answer:
/// { status, code, message, errors?, traceId }. Controllers therefore need no try/catch.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The browser closed the request. Nothing to answer.
        }
        catch (AppException ex)
        {
            _logger.LogInformation("{Code}: {Message}", ex.Code, ex.Message);
            object? errors = ex is InputValidationException validation ? validation.Errors : null;
            await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error");
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "server_error",
                "Something went wrong on the server.", null);
        }
    }

    private static async Task WriteAsync(HttpContext context, int status, string code, string message, object? errors)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status,
            code,
            message,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
