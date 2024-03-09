using System;
using Paramore.Brighter;

namespace Transmogrifier.Application.Ports.Driving
{
    public class DeletePerson(string name) : Command(Guid.NewGuid())
    {
        public string Name { get; } = name;
    }
}
