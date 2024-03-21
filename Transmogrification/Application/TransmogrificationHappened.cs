using System;
using Paramore.Brighter;

namespace Transmogrification.Application
{
    public class TransmogrificationHappened(string name, string transmogrification) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = name;
        public string Transmogrification { get; set; } = transmogrification;

        public TransmogrificationHappened(): this(string.Empty, string.Empty) {}
    }
}
