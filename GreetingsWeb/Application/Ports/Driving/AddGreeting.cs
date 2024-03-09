using System;
using Paramore.Brighter;

namespace GreetingsWeb.Application.Ports.Driving
{
    public class AddGreeting(string name, string greeting) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
        public string Greeting { get; } = greeting;
    }
}
