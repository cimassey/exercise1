using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using StargateAPI.Business.Data;
using StargateAPI.Middleware;

namespace api.Tests.Middleware;

public class ProcessLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncPersistsSuccessfulRequest()
    {
        var (connection, provider) = await CreateProviderAsync();
        await using var connectionScope = connection;
        await using var providerScope = provider;

        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        context.Request.Method = "GET";
        context.Request.Path = "/Person";

        var middleware = CreateMiddleware(provider.GetRequiredService<IServiceScopeFactory>(), _ =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        await using var scope = provider.CreateAsyncScope();
        var log = await scope.ServiceProvider.GetRequiredService<StargateContext>()
            .ProcessLogs.SingleAsync();
        Assert.Equal("Information", log.Level);
        Assert.Equal("GET", log.Method);
        Assert.Equal("/Person", log.Path);
        Assert.Equal(StatusCodes.Status200OK, log.StatusCode);
        Assert.Null(log.Exception);
    }

    [Fact]
    public async Task InvokeAsyncPersistsExceptionAndReturnsSafeServerError()
    {
        var (connection, provider) = await CreateProviderAsync();
        await using var connectionScope = connection;
        await using var providerScope = provider;

        var context = new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() }
        };
        context.Request.Method = "POST";
        context.Request.Path = "/AstronautDuty";

        var middleware = CreateMiddleware(provider.GetRequiredService<IServiceScopeFactory>(), _ =>
            throw new InvalidOperationException("Database operation failed"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var responseReader = new StreamReader(context.Response.Body);
        var response = await responseReader.ReadToEndAsync();
        Assert.Contains("An unexpected server error occurred.", response);
        Assert.DoesNotContain("Database operation failed", response);

        await using var scope = provider.CreateAsyncScope();
        var log = await scope.ServiceProvider.GetRequiredService<StargateContext>()
            .ProcessLogs.SingleAsync();
        Assert.Equal("Error", log.Level);
        Assert.Equal("POST", log.Method);
        Assert.Equal("/AstronautDuty", log.Path);
        Assert.Equal(StatusCodes.Status500InternalServerError, log.StatusCode);
        Assert.Contains("Database operation failed", log.Exception);
    }

    private static ProcessLoggingMiddleware CreateMiddleware(
        IServiceScopeFactory scopeFactory,
        RequestDelegate next)
    {
        return new ProcessLoggingMiddleware(
            next,
            scopeFactory,
            NullLogger<ProcessLoggingMiddleware>.Instance);
    }

    private static async Task<(SqliteConnection Connection, ServiceProvider Provider)> CreateProviderAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<StargateContext>(options => options.UseSqlite(connection));
        var provider = services.BuildServiceProvider();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<StargateContext>();
        await context.Database.MigrateAsync();

        return (connection, provider);
    }
}
