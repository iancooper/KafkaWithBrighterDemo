using System;
using Paramore.Brighter;
using Transmogrifier.Application.Entities;

namespace Transmogrifier.Application.Ports.Driven
{
    public class TransmogrificationMade(Person person, Transmogrification transmogrification) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = person.Name;
        public string Transformation { get; set; } = transmogrification.Description;
    }
}
