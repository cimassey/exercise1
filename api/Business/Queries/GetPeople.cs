using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPeople : IRequest<GetPeopleResult>
    {

    }

    public class GetPeopleHandler : IRequestHandler<GetPeople, GetPeopleResult>
    {
        public readonly StargateContext _context;
        public GetPeopleHandler(StargateContext context)
        {
            _context = context;
        }
        public async Task<GetPeopleResult> Handle(GetPeople request, CancellationToken cancellationToken)
        {
            var result = new GetPeopleResult();

            //var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id";
            var query = """
            SELECT
                person.Id AS PersonId,
                person.Name,
                astDetail.CurrentRank,
                astDetail.CurrentDutyTitle,
                astDetail.CareerStartDate,
                astDetail.CareerEndDate
            FROM Person AS person
            LEFT JOIN AstronautDetail AS astDetail
                ON astDetail.PersonId = person.Id
            ORDER BY person.Name COLLATE NOCASE;
            """;

            var command = new CommandDefinition(
                commandText: query,
                cancellationToken: cancellationToken);

            var people = await _context.Connection
                .QueryAsync<PersonAstronaut>(command);

            result.People = people.ToList();

            return result;
        }
    }

    public class GetPeopleResult : BaseResponse
    {
        public List<PersonAstronaut> People { get; set; } = new List<PersonAstronaut> { };

    }
}
