using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SmartDesk.Application.Common;

namespace SmartDesk.Api.Middleware;

/// <summary>
/// Centralised error handling (spec section 5.5). Every domain exception maps to one HTTP status code
/// and one RFC 7807 ProblemDetails body, so both React and a future Flutter client can use a single
/// error handler. Stack traces are never returned outside Development.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IHostEnvironment environment)
{
    /// <summary>499 "client closed request"; ASP.NET Core has no constant for it.</summary>
    private const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            AgentValidationException => (StatusCodes.Status422UnprocessableEntity, "Agent output validation failed"),
            DownstreamUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Service unavailable"),
            OperationCanceledException => (ClientClosedRequest, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        // Expected domain outcomes are information, not incidents; only true failures are logged as errors.
        if (status >= 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            logger.LogInformation("{Status} on {Path}: {Message}", status, context.Request.Path, ex.Message);

        if (context.Response.HasStarted) return;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            // A 500's real message may contain internals, so it is replaced outside Development.
            Detail = status >= 500 && !environment.IsDevelopment()
                ? "An unexpected error occurred. Please contact support with the trace identifier."
                : ex.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}
