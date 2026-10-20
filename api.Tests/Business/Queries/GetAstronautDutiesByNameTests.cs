using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Queries;
using Xunit;

namespace api.Tests.Business.Queries;

public class GetAstronautDutiesByNameTests
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
    public async Task HandleExistingPersonReturnsPersonAndDuties()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "DutyTest-Astronaut" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        context.AstronautDuties.AddRange(
            new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "Colonel",
                DutyTitle = "Commander",
                DutyStartDate = new DateTime(2020, 1, 1),
                DutyEndDate = new DateTime(2022, 12, 31)
            },
            new AstronautDuty
            {
                PersonId = person.Id,
                Rank = "General",
                DutyTitle = "Chief",
                DutyStartDate = new DateTime(2023, 1, 1),
                DutyEndDate = null
            });

        await context.SaveChangesAsync();

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName { Name = person.Name },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(person.Id, result.Person.PersonId);
        Assert.Equal(person.Name, result.Person.Name);

        Assert.Equal(2, result.AstronautDuties.Count);
        Assert.Equal("Chief", result.AstronautDuties[0].DutyTitle);
        Assert.Equal("Commander", result.AstronautDuties[1].DutyTitle);
    }

    [Fact]
    public async Task HandlePersonWithNoDutiesReturnsEmptyDutyList()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "DutyTest-NoDuties" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName { Name = person.Name },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(person.Name, result.Person.Name);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task HandleNonexistentPersonReturnsNullPersonAndEmptyDutyList()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName { Name = "DutyTest-Unknown" },
            CancellationToken.None);

        Assert.Null(result.Person);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task HandleNonAstronautReturnsPersonAndEmptyDutyList()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "DutyTest-NonAstronaut" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName { Name = person.Name },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(person.Name, result.Person.Name);
        Assert.Null(result.Person.CurrentRank);
        Assert.Null(result.Person.CurrentDutyTitle);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task HandleNameWithDifferentCaseReturnsMatchingPerson()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "DutyTest-CaseSensitive" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName
            {
                Name = "DUTYTEST-CASESENSITIVE"
            },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(person.Id, result.Person.PersonId);
        Assert.Empty(result.AstronautDuties);
    }

    [Fact]
    public async Task HandleExistingPersonReturnsOnlyTheirDutiesInDescendingDateOrder()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "DutyTest-Astronaut" };
        var otherPerson = new Person { Name = "DutyTest-OtherAstronaut" };

        context.People.AddRange(person, otherPerson);
        await context.SaveChangesAsync();

        var olderDuty = new AstronautDuty
        {
            PersonId = person.Id,
            Rank = "Colonel",
            DutyTitle = "Commander",
            DutyStartDate = new DateTime(2020, 1, 1),
            DutyEndDate = new DateTime(2022, 12, 31)
        };

        var currentDuty = new AstronautDuty
        {
            PersonId = person.Id,
            Rank = "General",
            DutyTitle = "Chief",
            DutyStartDate = new DateTime(2023, 1, 1),
            DutyEndDate = null
        };

        var unrelatedDuty = new AstronautDuty
        {
            PersonId = otherPerson.Id,
            Rank = "Captain",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2024, 1, 1),
            DutyEndDate = null
        };

        context.AstronautDuties.AddRange(
            olderDuty,
            currentDuty,
            unrelatedDuty);

        await context.SaveChangesAsync();

        var handler = new GetAstronautDutiesByNameHandler(context);

        var result = await handler.Handle(
            new GetAstronautDutiesByName { Name = person.Name },
            CancellationToken.None);

        Assert.NotNull(result.Person);
        Assert.Equal(person.Id, result.Person.PersonId);
        Assert.Equal(person.Name, result.Person.Name);

        Assert.Collection(
            result.AstronautDuties,
            duty =>
            {
                Assert.Equal(currentDuty.Id, duty.Id);
                Assert.Equal("General", duty.Rank);
                Assert.Equal("Chief", duty.DutyTitle);
                Assert.Equal(new DateTime(2023, 1, 1), duty.DutyStartDate);
                Assert.Null(duty.DutyEndDate);
            },
            duty =>
            {
                Assert.Equal(olderDuty.Id, duty.Id);
                Assert.Equal("Colonel", duty.Rank);
                Assert.Equal("Commander", duty.DutyTitle);
                Assert.Equal(new DateTime(2020, 1, 1), duty.DutyStartDate);
                Assert.Equal(new DateTime(2022, 12, 31), duty.DutyEndDate);
            });
    }
}