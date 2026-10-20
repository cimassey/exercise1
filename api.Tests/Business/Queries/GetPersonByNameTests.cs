
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using Xunit;

namespace api.Tests.Business.Queries;

public class GetPersonByNameTests
{
    private static async Task<(SqliteConnection Connection, StargateContext Context)>
        CreateContextAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(connection)
            .Options;

        var context = new StargateContext(options);
        await context.Database.EnsureCreatedAsync();

        return (connection, context);
    }

    [Fact]
    public async Task HandleExactNameMatchReturnsPerson()
    {
        var exactName = "ExactMatch-Test";
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        context.People.Add(new Person { Name = exactName });
        await context.SaveChangesAsync();

        var handler = new GetPersonByNameHandler(context);
        var result = await handler.Handle(
            new GetPersonByName { Name = exactName },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(exactName, result.Person.Name);
    }

    [Fact]
    public async Task HandleDifferentCaseNameReturnsPerson()
    {
        var mixedCaseAdd = "MixedCase-Test";
        var mixedCaseResult = "MIXEDCASE-TEST";
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        context.People.Add(new Person { Name = mixedCaseAdd });
        await context.SaveChangesAsync();

        var handler = new GetPersonByNameHandler(context);
        var result = await handler.Handle(
            new GetPersonByName { Name = mixedCaseResult },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(mixedCaseAdd, result.Person.Name);
    }

    [Fact]
    public async Task HandlePersonWithoutAstronautDetailsReturnsPerson()
    {
        var noAstronaut = "NoAstronaut-Test";
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        context.People.Add(new Person { Name = noAstronaut });
        await context.SaveChangesAsync();

        var handler = new GetPersonByNameHandler(context);
        var result = await handler.Handle(
            new GetPersonByName { Name = noAstronaut },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(noAstronaut, result.Person.Name);
    }

    [Fact]
    public async Task HandleUnknownNameReturnsNullPerson()
    {
        var unknownName = "Unknown123 Person456";
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var handler = new GetPersonByNameHandler(context);
        var result = await handler.Handle(
            new GetPersonByName { Name = unknownName },
            CancellationToken.None);

        Assert.Null(result.Person);
    }

    [Fact]
    public async Task HandleSqlInjectionInputReturnsNullPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        await context.SaveChangesAsync();

        var handler = new GetPersonByNameHandler(context);
        var result = await handler.Handle(
            new GetPersonByName { Name = "' OR 1=1 --" },
            CancellationToken.None);

        Assert.Null(result.Person);
    }
}
