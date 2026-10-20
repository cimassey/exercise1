using Dapper;
using MediatR;
using StargateAPI.Business.Data;
using StargateAPI.Business.Dtos;
using StargateAPI.Controllers;

namespace StargateAPI.Business.Queries
{
    public class GetPersonByName : IRequest<GetPersonByNameResult>
    {
        public required string Name { get; set; } = string.Empty;
    }

    public class GetPersonByNameHandler : IRequestHandler<GetPersonByName, GetPersonByNameResult>
    {
        private readonly StargateContext _context;
        public GetPersonByNameHandler(StargateContext context)
        {
            _context = context;
        }

        public async Task<GetPersonByNameResult> Handle(GetPersonByName request, CancellationToken cancellationToken)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(request.Name);
            
            //var query = $"SELECT a.Id as PersonId, a.Name, b.CurrentRank, b.CurrentDutyTitle, b.CareerStartDate, b.CareerEndDate FROM [Person] a LEFT JOIN [AstronautDetail] b on b.PersonId = a.Id WHERE '{request.Name}' = a.Name";
            // Convert to raw string literal so query reads like a query, use @Name to force Dapper to bind value and use parameterized query instead of raw user input
            // Update to also use COLLATE NOCASE to align with changes in api\Business\Data\Person.cs
            var query = """
                SELECT 
                    person.Id as PersonId, 
                    person.Name, 
                    astDetail.CurrentRank, 
                    astDetail.CurrentDutyTitle, 
                    astDetail.CareerStartDate, 
                    astDetail.CareerEndDate
                FROM [Person] AS person
                LEFT JOIN [AstronautDetail] AS astDetail
                    ON astDetail.PersonId = person.Id 
                WHERE person.Name = @Name COLLATE NOCASE
                """;

            // Allow cancelation token to reach Dapper db ops
            var command = new CommandDefinition(
                commandText: query,
                parameters: new { request.Name },
                cancellationToken: cancellationToken);

            // Change to QuerySingleOrDefaultAsync throw if returns multiple or return one or null, use command instead of raw query
            //var person = await _context.Connection.QueryAsync<PersonAstronaut>(query);
            var person = await _context.Connection.QuerySingleOrDefaultAsync<PersonAstronaut>(command);

            return new GetPersonByNameResult{ Person = person };
        }
    }

    public class GetPersonByNameResult : BaseResponse
    {
        public PersonAstronaut? Person { get; set; }
    }
}
