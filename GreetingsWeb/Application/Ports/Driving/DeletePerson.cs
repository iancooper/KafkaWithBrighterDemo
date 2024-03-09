using System;
using Paramore.Brighter;

namespace GreetingsWeb.Application.Ports.Driving
{
    public class DeletePerson(string name) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
    }
}
