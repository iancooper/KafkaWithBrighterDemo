using System;
using Paramore.Brighter;

namespace SalutationAnalytics.Application.Ports.Driven
{
    public class SalutationReceived(DateTimeOffset receivedAt) : Event(Guid.NewGuid())
    {
        public DateTimeOffset ReceivedAt { get; } = receivedAt;
    }
}
