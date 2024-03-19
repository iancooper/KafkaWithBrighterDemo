using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;
using Transmogrifier.Application.Entities;
using Transmogrifier.Application.Ports.Driven;

namespace Transmogrifier.Application.Ports.Driving
{
    public class MakeTransmogrificationHandlerAsync(
        IAmATransactionConnectionProvider transactionProvider,
        IAmACommandProcessor postBox,
        ILogger<MakeTransmogrificationHandlerAsync> logger)
        : RequestHandlerAsync<MakeTransmogrification>
    {
        [RequestLoggingAsync(0, HandlerTiming.Before)]
        [UsePolicyAsync(step:1, policy: Policies.Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<MakeTransmogrification> HandleAsync(MakeTransmogrification makeTransmogrification, CancellationToken cancellationToken = default)
        {
            var posts = new List<Guid>();
            
            //We use the unit of work to grab connection and transaction, because Outbox needs
            //to share them 'behind the scenes'

            var conn = await transactionProvider.GetConnectionAsync(cancellationToken);
            var tx = await transactionProvider.GetTransactionAsync(cancellationToken);
            try
            {
                var people = await conn.QueryAsync<Person>(
                    "select * from Person where name = @name",
                    new {name = makeTransmogrification.Name},
                    tx
                );
                var person = people.SingleOrDefault();

                if (person != null)
                {
                    var transmogrification = new Transmogrification(makeTransmogrification.Description, person);

                    //write the added child entity to the Db
                    await conn.ExecuteAsync(
                        "insert into Tramsmogrification (Description, Recipient_Id) values (@Description, @RecipientId)",
                        new { transmogrification.Description, RecipientId = transmogrification.RecipientId },
                        tx);

                    //Now write the message we want to send to the Db in the same transaction.
                    posts.Add(await postBox.DepositPostAsync(
                        new TransmogrificationMade(person, transmogrification),
                        transactionProvider,
                        cancellationToken: cancellationToken));

                    //commit both new greeting and outgoing message
                    await transactionProvider.CommitAsync(cancellationToken);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Exception thrown handling Add Description request");
                //it went wrong, rollback the entity change and the downstream message
                await transactionProvider.RollbackAsync(cancellationToken);
                return await base.HandleAsync(makeTransmogrification, cancellationToken);
            }
            finally
            {
                transactionProvider.Close();
            }

            //Send this message via a transport. We need the ids to send just the messages here, not all outstanding ones.
            //Alternatively, you can let the Sweeper do this, but at the cost of increased latency
            await postBox.ClearOutboxAsync(posts, cancellationToken:cancellationToken);

            return await base.HandleAsync(makeTransmogrification, cancellationToken);
        }
    }
}
