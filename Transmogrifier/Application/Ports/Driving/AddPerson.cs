using System;
using Paramore.Brighter;

namespace Transmogrifier.Application.Ports.Driving
{
    public class AddPerson(string name) : Command(Guid.NewGuid())
    {
        public string Name { get; set; } = name;
    }
}
