using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;
using Paramore.Brighter.Sqlite;
using Transmogrifier.Application.Entities;
using Transmogrifier.Application.Ports.Driven;

namespace Transmogrifier.Application.Ports.Driving
{
    public class MakeTransmogrificationHandlerAsync : RequestHandlerAsync<MakeTransmogrification>
    {
        private readonly IAmATransactionConnectionProvider _transactionProvider;
        private readonly IAmACommandProcessor _postBox;
        private readonly ILogger<MakeTransmogrificationHandlerAsync> _logger;

        public MakeTransmogrificationHandlerAsync(IAmATransactionConnectionProvider transactionProvider,
            IAmACommandProcessor postBox,
            ILogger<MakeTransmogrificationHandlerAsync> logger)
        {
            _transactionProvider = transactionProvider;
            _postBox = postBox;
            _logger = logger;
        }

        [RequestLoggingAsync(0, HandlerTiming.Before)]
        [UsePolicyAsync(step:1, policy: Policies.Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<MakeTransmogrification> HandleAsync(MakeTransmogrification makeTransmogrification, CancellationToken cancellationToken = default)
        {
            var posts = new List<Guid>();
            
            //We use the unit of work to grab connection and transaction, because Outbox needs
            //to share them 'behind the scenes'

            var conn = await _transactionProvider.GetConnectionAsync(cancellationToken);
            var tx = await _transactionProvider.GetTransactionAsync(cancellationToken);
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
                        "insert into Transmogrification (Description, Recipient_Id) values (@Description, @RecipientId)",
                        new { transmogrification.Description, transmogrification.RecipientId },
                        tx);

                    //Now write the message we want to send to the Db in the same transaction.
                    posts.Add(await _postBox.DepositPostAsync(
                        new TransmogrificationMade(person, transmogrification),
                        _transactionProvider,
                        cancellationToken: cancellationToken));

                    //commit both new greeting and outgoing message
                    await _transactionProvider.CommitAsync(cancellationToken);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception thrown handling Add Description request");
                //it went wrong, rollback the entity change and the downstream message
                await _transactionProvider.RollbackAsync(cancellationToken);
                return await base.HandleAsync(makeTransmogrification, cancellationToken);
            }
            finally
            {
                _transactionProvider.Close();
            }

            //Send this message via a transport. We need the ids to send just the messages here, not all outstanding ones.
            //Alternatively, you can let the Sweeper do this, but at the cost of increased latency
            await _postBox.ClearOutboxAsync(posts, cancellationToken:cancellationToken);

            return await base.HandleAsync(makeTransmogrification, cancellationToken);
        }
    }
}
