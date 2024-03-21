using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Paramore.Brighter;
using Paramore.Brighter.Inbox.Attributes;
using Paramore.Brighter.Logging.Attributes;
using Paramore.Brighter.Policies.Attributes;

namespace Transmogrification.Application.Ports.Driving
{
    public class TransmogrificationHandlerAsync(
        IAmARelationalDbConnectionProvider relationalDbConnectionProvider,
        IAmACommandProcessor commandProcessor,
        ILogger<TransmogrificationHandlerAsync> logger)
        : RequestHandlerAsync<TransmogrificationMade>
    {
        [UseInboxAsync(step:0, contextKey: typeof(TransmogrificationHandlerAsync), onceOnly: true )] 
        [RequestLoggingAsync(step: 1, timing: HandlerTiming.Before)]
        [UsePolicyAsync(step:2, policy: Policies.Retry.EXPONENTIAL_RETRYPOLICYASYNC)]
        public override async Task<TransmogrificationMade> HandleAsync(TransmogrificationMade @event, CancellationToken cancellationToken = default)
        {
            
            var conn = await relationalDbConnectionProvider.GetConnectionAsync(cancellationToken) ; 
            try
            {
                var history = new TransmogrificationHistory(@event.Name, @event.Transmogrification);
                
               await conn.ExecuteAsync(
                   "insert into TransmogrificationHappened (Name, Transmogrification) values (@name, @transformation)", 
                   new {name = history.Name, transformation = history.Transmogrification}
                   ); 
                
            }
            catch (Exception e)
            {
                logger.LogError(e, "Could not save transmogrification settings");
                return await base.HandleAsync(@event, cancellationToken);
            }
            
            await commandProcessor.PublishAsync(new TransmogrificationHappened(@event.Name, @event.Transmogrification), cancellationToken: cancellationToken);
            
            return await base.HandleAsync(@event, cancellationToken);
        }
    }
}
