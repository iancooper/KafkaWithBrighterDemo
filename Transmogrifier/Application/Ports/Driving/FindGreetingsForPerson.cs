using Paramore.Darker;
using Transmogrifier.Adapters.Driving;

namespace Transmogrifier.Application.Ports.Driving
{
    public class FindGreetingsForPerson(string name) : IQuery<FindPersonsGreetings>
    {
        public string Name { get; } = name;
    }
}
