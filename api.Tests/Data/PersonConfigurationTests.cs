using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using Xunit;

namespace api.Tests.Controllers;

public class PersonConfigurationTests
{
    [Fact]
    public async Task CreatePersonWithDifferentCaseNameThrows()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StargateContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new StargateContext(options);
        await context.Database.EnsureCreatedAsync();

        context.People.Add(new Person { Name = "CaseTest-7f39" });
        await context.SaveChangesAsync();

        context.People.Add(new Person { Name = "CASETEST-7F39" });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }
}