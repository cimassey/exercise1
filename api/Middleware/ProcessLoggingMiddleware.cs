using System.Net;
using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Middleware;

public sealed class ProcessLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessLoggingMiddleware> _logger;

    public ProcessLoggingMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<ProcessLoggingMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Exception? requestException = null;

        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            requestException = exception;
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }

        var statusCode = context.Response.StatusCode;
        var level = requestException is not null || statusCode >= 500
            ? LogLevel.Error
            : statusCode >= 400
                ? LogLevel.Warning
                : LogLevel.Information;
        var message = requestException is null
            ? $"Request completed with HTTP status {statusCode}."
            : "Unhandled exception while processing the request.";
        var path = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "/";

        if (requestException is not null)
        {
            _logger.LogError(requestException, "Unhandled exception processing {Method} {Path}", context.Request.Method, path);
        }
        else if (level == LogLevel.Error)
        {
            _logger.LogError("Request {Method} {Path} completed with HTTP status {StatusCode}", context.Request.Method, path, statusCode);
        }
        else if (level == LogLevel.Warning)
        {
            _logger.LogWarning("Request {Method} {Path} completed with HTTP status {StatusCode}", context.Request.Method, path, statusCode);
        }
        else
        {
            _logger.LogInformation("Request {Method} {Path} completed with HTTP status {StatusCode}", context.Request.Method, path, statusCode);
        }

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<StargateContext>();
            dbContext.ProcessLogs.Add(new ProcessLog
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level.ToString(),
                Method = context.Request.Method,
                Path = path,
                StatusCode = statusCode,
                Message = message,
                Exception = requestException?.ToString()
            });
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception loggingException)
        {
            _logger.LogCritical(loggingException, "Failed to persist the process log for {Method} {Path}", context.Request.Method, path);
            if (requestException is not null)
            {
                throw new AggregateException("The request failed and its process log could not be persisted.", requestException, loggingException);
            }

            throw;
        }

        if (requestException is null)
        {
            return;
        }

        if (context.Response.HasStarted)
        {
            ExceptionDispatchInfo.Capture(requestException).Throw();
        }

        context.Response.Clear();
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await context.Response.WriteAsJsonAsync(new BaseResponse
        {
            Success = false,
            Message = "An unexpected server error occurred.",
            ResponseCode = (int)HttpStatusCode.InternalServerError
        }, CancellationToken.None);
    }
}
