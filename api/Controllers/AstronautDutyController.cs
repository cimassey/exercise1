using MediatR;
using Microsoft.AspNetCore.Mvc;
using StargateAPI.Business.Commands;
using StargateAPI.Business.Queries;

namespace StargateAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AstronautDutyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AstronautDutyController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> GetAstronautDutiesByName(string name, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetAstronautDutiesByName()
            {
                Name = name
            }, cancellationToken);

            return this.GetResponse(result);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateAstronautDuty(
            [FromBody] CreateAstronautDuty request,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(request, cancellationToken);
            if (!result.Success)
            {
                return this.GetResponse(result);
            }

            // Link the created result to the existing name-based duty retrieval endpoint.
            return CreatedAtAction(
                nameof(GetAstronautDutiesByName),
                new { name = request.Name },
                result);
        }
    }
}