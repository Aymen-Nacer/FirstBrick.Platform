using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FirstBrick.Shared.ErrorHandling;

/// <summary>
/// Catches every unhandled exception thrown from the HTTP pipeline and returns
/// an RFC 7807 <see cref="ProblemDetails"/> response. Known exception types are
/// mapped to sensible status codes; everything else is a 500.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Client disconnected — no point writing a body the caller will never read.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            return true;

        var (status, title) = Map(exception);
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://httpstatuses.io/{status}",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}",
            Detail = _env.IsDevelopment() ? exception.Message : null
        };
        problem.Extensions["traceId"] = traceId;

        if (status >= 500)
        {
            _logger.LogError(exception,
                "Unhandled {ExceptionType} on {Method} {Path} (traceId={TraceId})",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, traceId);
        }
        else
        {
            _logger.LogWarning(exception,
                "Request rejected with {Status} on {Method} {Path} (traceId={TraceId})",
                status, httpContext.Request.Method, httpContext.Request.Path, traceId);
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }

    private static (int Status, string Title) Map(Exception ex) => ex switch
    {
        BadHttpRequestException     => (StatusCodes.Status400BadRequest,       "Bad request"),
        ArgumentException           => (StatusCodes.Status400BadRequest,       "Invalid argument"),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized,     "Unauthorized"),
        KeyNotFoundException        => (StatusCodes.Status404NotFound,         "Not found"),
        TimeoutException            => (StatusCodes.Status504GatewayTimeout,   "Upstream timeout"),
        NotImplementedException     => (StatusCodes.Status501NotImplemented,   "Not implemented"),
        _                           => (StatusCodes.Status500InternalServerError, "Internal server error")
    };
}
