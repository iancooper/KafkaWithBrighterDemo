using GreetingsWeb.Adapters.Driving;
using Paramore.Darker;

namespace GreetingsWeb.Application.Ports.Driving
{
    public class FindGreetingsForPerson(string name) : IQuery<FindPersonsGreetings>
    {
        public string Name { get; } = name;
    }
}
