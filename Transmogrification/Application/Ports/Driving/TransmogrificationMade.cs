using System;
using Paramore.Brighter;

namespace Transmogrification.Application.Ports.Driving
{
    public class TransmogrificationMade(string name, string transmogrification) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = name;
        public string Transmogrification { get; set; } = transmogrification;

        public TransmogrificationMade() : this(string.Empty, string.Empty) { }
    }
}
