using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Commands;
using Xunit;

namespace api.Tests.Business.Commands;

public class CreatePersonTests
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
    public async Task HandleValidPersonCreatesPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        const string name = "CreatePerson-Test";

        var handler = new CreatePersonHandler(context);
        var result = await handler.Handle(
            new CreatePerson { Name = name },
            CancellationToken.None);

        var person = await context.People
            .SingleOrDefaultAsync(p => p.Name == name);

        Assert.NotNull(person);
        Assert.Equal(name, person.Name);
        Assert.Equal(person.Id, result.Id);
    }

    [Fact]
    public async Task HandleDuplicateNameReturnsExistingPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        const string name = "CreatePerson-Duplicate";

        var handler = new CreatePersonHandler(context);

        var firstResult = await handler.Handle(
            new CreatePerson { Name = name },
            CancellationToken.None);

        var secondResult = await handler.Handle(
            new CreatePerson { Name = name },
            CancellationToken.None);

        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(1, await context.People.CountAsync(p => p.Name == name));
    }

    [Fact]
    public async Task HandleDuplicateNameWithDifferentCaseReturnsExistingPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var handler = new CreatePersonHandler(context);

        var firstResult = await handler.Handle(
            new CreatePerson { Name = "CreatePerson-MixedCase" },
            CancellationToken.None);

        var secondResult = await handler.Handle(
            new CreatePerson { Name = "CREATEPERSON-MIXEDCASE" },
            CancellationToken.None);

        Assert.Equal(firstResult.Id, secondResult.Id);
        Assert.Equal(
            1,
            await context.People.CountAsync(
                p => p.Name == "CreatePerson-MixedCase"));
    }
}