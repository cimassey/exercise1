using System.ComponentModel.DataAnnotations;
using System.Net;
using MediatR;
using Microsoft.EntityFrameworkCore;
using StargateAPI.Business.Data;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Commands
{
    public class CreateAstronautDuty : IRequest<CreateAstronautDutyResult>
    {
        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Rank { get; set; }

        [Required]
        public required string DutyTitle { get; set; }

        public DateTime DutyStartDate { get; set; }
    }

    public class CreateAstronautDutyHandler : IRequestHandler<CreateAstronautDuty, CreateAstronautDutyResult>
    {
        private readonly StargateContext _context;

        public CreateAstronautDutyHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<CreateAstronautDutyResult> Handle(CreateAstronautDuty request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Name)
                || string.IsNullOrWhiteSpace(request.Rank)
                || string.IsNullOrWhiteSpace(request.DutyTitle)
                || request.DutyStartDate.Date == DateTime.MinValue)
            {
                return Failure(HttpStatusCode.BadRequest, "Name, rank, duty title, and a valid start date are required.");
            }

            var person = await _context.People
                .SingleOrDefaultAsync(
                    candidate => EF.Functions.Collate(candidate.Name, "NOCASE") == request.Name,
                    cancellationToken);

            if (person is null)
            {
                return Failure(HttpStatusCode.NotFound, "Person not found.");
            }

            var dutyStartDate = request.DutyStartDate.Date;
            var previousDuty = await _context.AstronautDuties
                .Where(duty => duty.PersonId == person.Id)
                .OrderByDescending(duty => duty.DutyStartDate)
                .ThenByDescending(duty => duty.Id)
                .FirstOrDefaultAsync(cancellationToken);

            // Duty dates are day-based, and a new assignment must not overlap or precede the latest one.
            if (previousDuty is not null && dutyStartDate <= previousDuty.DutyStartDate.Date)
            {
                return Failure(HttpStatusCode.BadRequest, "The new duty must start after the person's previous duty.");
            }

            var isRetired = string.Equals(request.DutyTitle, "RETIRED", StringComparison.OrdinalIgnoreCase);
            // ACTS defines career end as the day before retirement; non-retirement assignments keep the career open.
            var careerEndDate = isRetired ? dutyStartDate.AddDays(-1) : (DateTime?)null;
            var astronautDetail = await _context.AstronautDetails
                .SingleOrDefaultAsync(detail => detail.PersonId == person.Id, cancellationToken);

            if (astronautDetail == null)
            {
                _context.AstronautDetails.Add(new AstronautDetail
                {
                    PersonId = person.Id,
                    CurrentDutyTitle = request.DutyTitle,
                    CurrentRank = request.Rank,
                    CareerStartDate = dutyStartDate,
                    CareerEndDate = careerEndDate
                });
            }
            else
            {
                astronautDetail.CurrentDutyTitle = request.DutyTitle;
                astronautDetail.CurrentRank = request.Rank;
                astronautDetail.CareerEndDate = careerEndDate;
            }

            var newAstronautDuty = new AstronautDuty()
            {
                PersonId = person.Id,
                Rank = request.Rank,
                DutyTitle = request.DutyTitle,
                DutyStartDate = dutyStartDate,
                DutyEndDate = null
            };

            if (previousDuty is not null)
            {
                // Keep consecutive duties non-overlapping while leaving the newly created duty current.
                previousDuty.DutyEndDate = dutyStartDate.AddDays(-1);
            }

            _context.AstronautDuties.Add(newAstronautDuty);

            await _context.SaveChangesAsync(cancellationToken);

            return new CreateAstronautDutyResult()
            {
                Id = newAstronautDuty.Id,
                ResponseCode = (int)HttpStatusCode.Created
            };
        }

        private static CreateAstronautDutyResult Failure(HttpStatusCode statusCode, string message) =>
            new()
            {
                Success = false,
                Message = message,
                ResponseCode = (int)statusCode
            };
    }

    public class CreateAstronautDutyResult : BaseResponse
    {
        public int? Id { get; set; }
    }
}
