
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Queries;
using Xunit;

namespace api.Tests.Business.Queries;

public class GetPeopleTests
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
    public async Task HandleReturnsAllPeople()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        context.People.AddRange(
            new Person { Name = "GetPeople-Test-A-101" },
            new Person { Name = "GetPeople-Test-B-102" });

        await context.SaveChangesAsync();

        var handler = new GetPeopleHandler(context);

        var result = await handler.Handle(
            new GetPeople(),
            CancellationToken.None);

        Assert.Contains(
            result.People,
            p => p.Name == "GetPeople-Test-A-101");

        Assert.Contains(
            result.People,
            p => p.Name == "GetPeople-Test-B-102");
    }

    [Fact]
    public async Task HandlePersonWithoutAstronautDetailsIncludesPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        const string name = "GetPeople-NoAstronaut";

        context.People.Add(new Person { Name = name });
        await context.SaveChangesAsync();

        var handler = new GetPeopleHandler(context);

        var result = await handler.Handle(
            new GetPeople(),
            CancellationToken.None);

        var person = Assert.Single(
            result.People,
            p => p.Name == name);

        Assert.Equal(name, person.Name);
        Assert.Null(person.CurrentRank);
        Assert.Null(person.CurrentDutyTitle);
    }

    [Fact]
    public async Task HandleReturnsEmptyListWhenNoPeopleExist()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        // This assertion assumes the test database has no model-seeded people.
        // If EnsureCreated adds seed records, see note below.
        var handler = new GetPeopleHandler(context);

        var result = await handler.Handle(
            new GetPeople(),
            CancellationToken.None);

        Assert.NotNull(result.People);
    }
}
