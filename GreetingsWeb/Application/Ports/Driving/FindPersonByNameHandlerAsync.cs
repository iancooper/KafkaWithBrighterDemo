using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using GreetingsWeb.Adapters.Driving;
using GreetingsWeb.Application.Entities;
using GreetingsWeb.Policies;
using Paramore.Brighter;
using Paramore.Darker;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;

namespace GreetingsWeb.Application.Ports.Driving
{
    public class FindPersonByNameHandlerAsync(IAmARelationalDbConnectionProvider relationalDbConnectionProvider)
        : QueryHandlerAsync<FindPersonByName, FindPersonResult>
    {
        [QueryLogging(0)]
        [RetryableQuery(1, Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<FindPersonResult> ExecuteAsync(FindPersonByName query, CancellationToken cancellationToken = new CancellationToken())
        {
            await using var connection = await relationalDbConnectionProvider .GetConnectionAsync(cancellationToken);
            var people = await connection.QueryAsync<Person>("select * from Person where name = @name", new {name = query.Name});
            var person = people.SingleOrDefault();

            return new FindPersonResult(person);
        }
    }
}
