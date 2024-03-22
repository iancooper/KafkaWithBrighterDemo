using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Paramore.Brighter;
using Paramore.Darker;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using TransmogrifierAPI.Adapters.Driving;
using TransmogrifierAPI.Policies;

namespace TransmogrifierAPI.Application.Ports.Driving
{
    public class FindTransmogrificationsForPersonHandlerAsync(
        IAmARelationalDbConnectionProvider relationalDbConnectionProvider)
        : QueryHandlerAsync<FindTransmogrificationsForPerson, FindPersonTransmogrifications>
    {
        [QueryLogging(0)]
        [RetryableQuery(1, Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<FindPersonTransmogrifications> ExecuteAsync(FindTransmogrificationsForPerson query,
            CancellationToken cancellationToken = new CancellationToken())
        {
            //Retrieving parent and child is a bit tricky with Dapper. From raw SQL We wget back a set that has a row-per-child. We need to turn that
            //into one entity per parent, with a collection of children. To do that we bring everything back into memory, group by parent id and collate all
            //the children for that group.

            var sql = @"select p.Id, p.Name, t.Id, t.Description 
                        from Person p
                        inner join Transmogrification t on t.Recipient_Id = p.Id
                        where p.Name = @Name";
            
            await using var connection = await relationalDbConnectionProvider.GetConnectionAsync(cancellationToken);
            var people = await connection.QueryAsync<Person, Transmogrification, Person>(
                sql,
                (person, transmogrification) =>
                {
                    person.Transmogrifications.Add(transmogrification);
                    return person;
                }, splitOn: "Id");

            if (!people.Any())
            {
                return new FindPersonTransmogrifications(query.Name, Array.Empty<string>());
            }

            var peopleGreetings = people
                .GroupBy(p => p.Name)
                .Select(grp =>
                    {
                        var groupedPerson = grp.First();
                        groupedPerson.Transmogrifications = grp.Select(p => p.Transmogrifications.Single()).ToList();
                        return groupedPerson;
                    }
                );

            var person = peopleGreetings.First();

            return new FindPersonTransmogrifications(person.Name, person.Transmogrifications.Select(t => t.Description));
        }
    }
}
