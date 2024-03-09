using System;
using Paramore.Brighter;
using Transmogrification.Application.Entities;

namespace Transmogrification.Application.Ports.Driven
{
    public class TransmogrificationRequested(TransmogrificationSettings settings) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = settings.Name;
        public string Transformation { get; set; } = settings.Transformation;
    }
}
