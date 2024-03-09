using System;
using Paramore.Brighter;

namespace GreetingsWeb.Application.Ports.Driven
{
    public class GreetingMade(string greeting) : Event(Guid.NewGuid())
    {
        public string Greeting { get; set; } = greeting;
    }
}
