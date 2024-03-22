using System;
using Paramore.Brighter;

namespace TransmogrifierAPI.Application.Ports.Driving
{
    public class AddPerson(string name) : Command(Guid.NewGuid())
    {
        public string Name { get; set; } = name;
    }
}
