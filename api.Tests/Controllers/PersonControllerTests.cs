
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Dtos;
using StargateAPI.Business.Queries;
using StargateAPI.Controllers;
using System.Net;
using Xunit;

namespace api.Tests.Controllers;

public class PersonControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly PersonController _controller;

    public PersonControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new PersonController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetPersonByNameExistingPersonReturnsOk()
    {
        const string name = "Controller-Test";

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPersonByName>(q => q.Name == name),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPersonByNameResult
            {
                Success = true,
                ResponseCode = (int)HttpStatusCode.OK,
                Person = new PersonAstronaut
                {
                    Name = name
                }
            });

        var result = await _controller.GetPersonByName(name);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.OK, objectResult.StatusCode);

        var response = Assert.IsType<GetPersonByNameResult>(
            objectResult.Value);

        Assert.Equal(name, response.Person?.Name);

        Assert.Equal(name, response.Person?.Name);
        _mediatorMock.Verify(
            m => m.Send(
                It.Is<GetPersonByName>(q => q.Name == name),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonByNameUnknownPersonReturnsNotFound()
    {
        const string name = "Controller-Unknown";

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<GetPersonByName>(q => q.Name == name),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPersonByNameResult
            {
                Success = false,
                ResponseCode = (int)HttpStatusCode.NotFound,
                Person = null
            });

        var result = await _controller.GetPersonByName(name);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.NotFound, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetPeopleReturnsOk()
    {
        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetPeople>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetPeopleResult
            {
                Success = true,
                ResponseCode = (int)HttpStatusCode.OK
            });

        var result = await _controller.GetPeople();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.OK, objectResult.StatusCode);

        _mediatorMock.Verify(
            m => m.Send(
                It.IsAny<GetPeople>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreatePersonValidNameReturnsSuccess()
    {
        const string name = "Controller-Create";

        _mediatorMock
            .Setup(m => m.Send(
                It.Is<CreatePerson>(c => c.Name == name),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePersonResult
            {
                Success = true,
                ResponseCode = (int)HttpStatusCode.OK
            });

        var result = await _controller.CreatePerson(name);

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.OK, objectResult.StatusCode);

        _mediatorMock.Verify(
            m => m.Send(
                It.Is<CreatePerson>(c => c.Name == name),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetPersonByNameMediatorThrowsForRequestMiddlewareToHandle()
    {
        _mediatorMock
            .Setup(m => m.Send(
                It.IsAny<GetPersonByName>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.GetPersonByName("Controller-Error"));
    }
}
