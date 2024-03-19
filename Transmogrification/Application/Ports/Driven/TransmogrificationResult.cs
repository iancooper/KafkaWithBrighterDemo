using System;
using Paramore.Brighter;
using Transmogrification.Application.Entities;

namespace Transmogrification.Application.Ports.Driven
{
    public class TransmogrificationResult(Entities.TransmogrificationResult result) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = result.Name;
        public string Transformation { get; set; } = result.Transformation;
    }
}
