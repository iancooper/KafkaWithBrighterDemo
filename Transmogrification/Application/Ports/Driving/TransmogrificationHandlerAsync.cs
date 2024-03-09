using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Inbox.Attributes;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;
using Transmogrification.Application.Entities;
using Transmogrification.Application.Ports.Driven;

namespace Transmogrification.Application.Ports.Driving
{
    public class TransmogrificationHandlerAsync(
        IAmABoxTransactionProvider<DbTransaction> transactionConnectionProvider,
        IAmACommandProcessor postBox,
        ILogger<TransmogrificationHandlerAsync> logger)
        : RequestHandlerAsync<Transmogrification>
    {
        [UseInboxAsync(step:0, contextKey: typeof(TransmogrificationHandlerAsync), onceOnly: true )] 
        [RequestLoggingAsync(step: 1, timing: HandlerTiming.Before)]
        [UsePolicyAsync(step:2, policy: Policies.Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<Transmogrification> HandleAsync(Transmogrification @event, CancellationToken cancellationToken = default)
        {
            var posts = new List<Guid>();
            
            var tx = await transactionConnectionProvider.GetTransactionAsync(cancellationToken);
            var conn = tx.Connection; 
            try
            {
                var transmogrificationSettings = new TransmogrificationSettings(@event.Name, @event.Transformation);
                
               await conn.ExecuteAsync(
                   "insert into TransmogrificationSettings (Name, Transformation) values (@name, @transformation)", 
                   new {name = transmogrificationSettings.Name, transformation = transmogrificationSettings.Transformation},
                   tx); 
                
                posts.Add(await postBox.DepositPostAsync(new TransmogrificationRequested(transmogrificationSettings), transactionConnectionProvider, cancellationToken: cancellationToken));
                
                await transactionConnectionProvider.CommitAsync(cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not save transmogrification settings");
                                                                           
                //if it went wrong rollback entity write and Outbox write
                await transactionConnectionProvider.RollbackAsync(cancellationToken);
                
                return await base.HandleAsync(@event, cancellationToken);
            }

            postBox.ClearOutbox(posts.ToArray());
            
            return await base.HandleAsync(@event, cancellationToken);
        }
    }
}
