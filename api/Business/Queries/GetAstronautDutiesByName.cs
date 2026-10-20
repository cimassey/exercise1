using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetAstronautDutiesByName : IRequest<GetAstronautDutiesByNameResult>
    {
        public string Name { get; set; } = string.Empty;
    }

    public class GetAstronautDutiesByNameHandler : IRequestHandler<GetAstronautDutiesByName, GetAstronautDutiesByNameResult>
    {
        private readonly StargateContext _context;

        public GetAstronautDutiesByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetAstronautDutiesByNameResult> Handle(GetAstronautDutiesByName request, CancellationToken cancellationToken)
        {

            var result = new GetAstronautDutiesByNameResult();

            //var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE \'{request.Name}\' = a.Name";
            const string personQuery = """
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
                WHERE person.Name = @Name COLLATE NOCASE;
                """;

            var command = new CommandDefinition(
            personQuery,
            new { request.Name },
            cancellationToken: cancellationToken);

            var person = await _context.Connection
                .QueryFirstOrDefaultAsync<PersonAstronaut>(command);

            // If no matching person return an empty result rather than dereferencing a null person or querying with an invalid ID.
            if (person is null)
            {
                return result;
            }

            result.Person = person;

            // query = $"SELECT * FROM [AstronautDuty] WHERE {person.PersonId} = PersonId Order By DutyStartDate Desc";
            const string dutiesQuery = """
                SELECT
                    Id,
                    PersonId,
                    Rank,
                    DutyTitle,
                    DutyStartDate,
                    DutyEndDate
                FROM AstronautDuty
                WHERE PersonId = @PersonId
                ORDER BY DutyStartDate DESC;
                """;

            var dutiesCommand = new CommandDefinition(
                dutiesQuery,
                new { person.PersonId },
                cancellationToken: cancellationToken);

            var duties = await _context.Connection
                .QueryAsync<AstronautDuty>(dutiesCommand);

            result.AstronautDuties = duties.ToList();

            return result;

        }
    }

    public class GetAstronautDutiesByNameResult : BaseResponse
    {
        // Seems like PersonAstronaut can be null now for People not astronauts
        public PersonAstronaut? Person { get; set; }
        public List<AstronautDuty> AstronautDuties { get; set; } = new(); // type known doesn't need to be restatedS
    }
}
