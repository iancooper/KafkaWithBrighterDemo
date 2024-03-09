using System;
using Paramore.Brighter;

namespace Transmogrifier.Application.Ports.Driving
{
    public class AddGreeting(string name, string greeting) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
        public string Greeting { get; } = greeting;
    }
}
