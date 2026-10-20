using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Data;

namespace api.Tests.Business.Commands;

public class CreateAstronautDutyTests
{
    private static async Task<(SqliteConnection Connection, StargateContext Context)> CreateContextAsync()
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
    public async Task HandleFirstDutyCreatesCurrentDutyAndAstronautDetail()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "CreateDuty-First" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            person.Name,
            "Captain",
            "Pilot",
            new DateTime(2024, 5, 6, 15, 30, 0)), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(201, result.ResponseCode);
        Assert.NotNull(result.Id);

        var duty = await context.AstronautDuties.SingleAsync(duty => duty.PersonId == person.Id);
        Assert.Equal(person.Id, duty.PersonId);
        Assert.Equal("Captain", duty.Rank);
        Assert.Equal("Pilot", duty.DutyTitle);
        Assert.Equal(new DateTime(2024, 5, 6), duty.DutyStartDate);
        Assert.Null(duty.DutyEndDate);

        var detail = await context.AstronautDetails.SingleAsync(detail => detail.PersonId == person.Id);
        Assert.Equal(new DateTime(2024, 5, 6), detail.CareerStartDate);
        Assert.Null(detail.CareerEndDate);
        Assert.Equal("Captain", detail.CurrentRank);
        Assert.Equal("Pilot", detail.CurrentDutyTitle);
    }

    [Fact]
    public async Task HandleNewDutyEndsPreviousDutyTheDayBeforeAndUpdatesCurrentDetail()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "CreateDuty-Next" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        context.AstronautDetails.Add(new AstronautDetail
        {
            PersonId = person.Id,
            CurrentRank = "Captain",
            CurrentDutyTitle = "Pilot",
            CareerStartDate = new DateTime(2020, 1, 1)
        });
        var previousDuty = new AstronautDuty
        {
            PersonId = person.Id,
            Rank = "Captain",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2020, 1, 1)
        };
        context.AstronautDuties.Add(previousDuty);
        await context.SaveChangesAsync();

        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            person.Name,
            "Major",
            "Commander",
            new DateTime(2024, 5, 6)), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2024, 5, 5), previousDuty.DutyEndDate);

        var currentDuty = await context.AstronautDuties
            .SingleAsync(duty => duty.DutyStartDate == new DateTime(2024, 5, 6));
        Assert.Null(currentDuty.DutyEndDate);

        var detail = await context.AstronautDetails.SingleAsync(detail => detail.PersonId == person.Id);
        Assert.Equal(new DateTime(2020, 1, 1), detail.CareerStartDate);
        Assert.Null(detail.CareerEndDate);
        Assert.Equal("Major", detail.CurrentRank);
        Assert.Equal("Commander", detail.CurrentDutyTitle);
    }

    [Fact]
    public async Task HandleRetirementSetsCareerEndAndCurrentDutyEndDate()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "CreateDuty-Retired" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        var previousDuty = new AstronautDuty
        {
            PersonId = person.Id,
            Rank = "Captain",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2020, 1, 1)
        };
        context.AstronautDuties.Add(previousDuty);
        context.AstronautDetails.Add(new AstronautDetail
        {
            PersonId = person.Id,
            CurrentRank = "Captain",
            CurrentDutyTitle = "Pilot",
            CareerStartDate = new DateTime(2020, 1, 1)
        });
        await context.SaveChangesAsync();

        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            person.Name,
            "Retired",
            "RETIRED",
            new DateTime(2024, 5, 6)), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(new DateTime(2024, 5, 5), previousDuty.DutyEndDate);

        var detail = await context.AstronautDetails.SingleAsync(detail => detail.PersonId == person.Id);
        Assert.Equal(new DateTime(2024, 5, 5), detail.CareerEndDate);
        Assert.Equal("Retired", detail.CurrentRank);
        Assert.Equal("RETIRED", detail.CurrentDutyTitle);

        var retirementDuty = await context.AstronautDuties
            .SingleAsync(duty => duty.DutyTitle == "RETIRED");
        Assert.Null(retirementDuty.DutyEndDate);
    }

    [Fact]
    public async Task HandleRetirementAsFirstDutySetsCareerEndDayBeforeStart()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "CreateDuty-RetirementFirst" };
        context.People.Add(person);
        await context.SaveChangesAsync();

        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            person.Name,
            "Retired",
            "RETIRED",
            new DateTime(2024, 5, 6)), CancellationToken.None);

        Assert.True(result.Success);
        var detail = await context.AstronautDetails.SingleAsync(detail => detail.PersonId == person.Id);
        Assert.Equal(new DateTime(2024, 5, 5), detail.CareerEndDate);
    }

    [Fact]
    public async Task HandleMissingPersonReturnsNotFoundWithoutCreatingDuty()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var existingDutyCount = await context.AstronautDuties.CountAsync();
        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            "CreateDuty-Missing",
            "Captain",
            "Pilot",
            new DateTime(2024, 5, 6)), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(404, result.ResponseCode);
        Assert.Equal(existingDutyCount, await context.AstronautDuties.CountAsync());
    }

    [Fact]
    public async Task HandleDutyStartingBeforeOrOnPreviousDutyReturnsBadRequest()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var person = new Person { Name = "CreateDuty-InvalidDate" };
        context.People.Add(person);
        await context.SaveChangesAsync();
        context.AstronautDuties.Add(new AstronautDuty
        {
            PersonId = person.Id,
            Rank = "Captain",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2024, 5, 6)
        });
        await context.SaveChangesAsync();

        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(CreateDuty(
            person.Name,
            "Major",
            "Commander",
            new DateTime(2024, 5, 6)), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.ResponseCode);
        Assert.Equal(1, await context.AstronautDuties.CountAsync(duty => duty.PersonId == person.Id));
    }

    [Fact]
    public async Task HandleMissingRequiredValuesReturnsBadRequest()
    {
        var (connection, context) = await CreateContextAsync();
        await using var connectionScope = connection;
        await using var contextScope = context;

        var existingDutyCount = await context.AstronautDuties.CountAsync();
        var handler = new CreateAstronautDutyHandler(context);
        var result = await handler.Handle(new CreateAstronautDuty
        {
            Name = " ",
            Rank = "Captain",
            DutyTitle = "Pilot",
            DutyStartDate = new DateTime(2024, 5, 6)
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(400, result.ResponseCode);
        Assert.Equal(existingDutyCount, await context.AstronautDuties.CountAsync());
    }

    private static CreateAstronautDuty CreateDuty(
        string name,
        string rank,
        string dutyTitle,
        DateTime dutyStartDate) =>
        new()
        {
            Name = name,
            Rank = rank,
            DutyTitle = dutyTitle,
            DutyStartDate = dutyStartDate
        };
}
