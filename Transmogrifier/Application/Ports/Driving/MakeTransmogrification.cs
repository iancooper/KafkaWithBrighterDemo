using System;
using Paramore.Brighter;

namespace Transmogrifier.Application.Ports.Driving
{
    public class MakeTransmogrification(string name, string description) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
        public string Description { get; } = description;
    }
}
