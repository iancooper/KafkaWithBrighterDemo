using System;
using Paramore.Brighter;

namespace Transmogrification.Application.Ports.Driving
{
    public class GreetingMade(string greeting) : Event(Guid.NewGuid())
    {
        public string Greeting { get; set; } = greeting;
    }
}
