using System;
using Paramore.Brighter;

namespace SalutationAnalytics.Application.Ports.Driving
{
    public class GreetingMade(string greeting) : Event(Guid.NewGuid())
    {
        public string Greeting { get; set; } = greeting;
    }
}
