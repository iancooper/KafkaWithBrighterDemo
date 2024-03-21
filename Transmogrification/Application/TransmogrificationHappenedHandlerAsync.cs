using System.Threading;
using System.Threading.Tasks;
using Paramore.Brighter;
using Paramore.Brighter.Logging.Attributes;
using Transmogrification.Adapters.Driven;

namespace Transmogrification.Application;

public class TransmogrificationHappenedHandlerAsync(IBox box) : RequestHandlerAsync<TransmogrificationHappened>
{
    [RequestLoggingAsync(step: 0, timing: HandlerTiming.Before)]
    public override async Task<TransmogrificationHappened> HandleAsync(TransmogrificationHappened @event, CancellationToken cancellationToken = new CancellationToken())
    {
        box.WriteRow(@event);
        
        return await base.HandleAsync(@event, cancellationToken);
    }
}