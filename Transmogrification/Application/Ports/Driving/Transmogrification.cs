using System;
using Paramore.Brighter;

namespace Transmogrification.Application.Ports.Driving
{
    public class Transmogrification(string name, string transformation) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = name;
        public string Transformation { get; set; } = transformation;
    }
}
