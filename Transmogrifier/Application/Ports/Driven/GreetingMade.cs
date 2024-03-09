using System;
using Paramore.Brighter;

namespace Transmogrifier.Application.Ports.Driven
{
    public class GreetingMade(string greeting) : Event(Guid.NewGuid())
    {
        public string Greeting { get; set; } = greeting;
    }
}
