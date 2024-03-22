using System;
using Paramore.Brighter;

namespace TransmogrifierAPI.Application.Ports.Driving
{
    public class DeletePerson(string name) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
    }
}
