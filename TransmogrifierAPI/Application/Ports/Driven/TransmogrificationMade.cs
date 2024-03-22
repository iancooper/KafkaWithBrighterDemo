using System;
using Paramore.Brighter;

namespace TransmogrifierAPI.Application.Ports.Driven
{
    public class TransmogrificationMade(Person person, Transmogrification transmogrification) : Event(Guid.NewGuid())
    {
        public string Name { get; set; } = person.Name;
        public string Transmogrification { get; set; } = transmogrification.Description;
    }
}
