using GreetingsWeb.Adapters.Driving;
using Paramore.Darker;

namespace GreetingsWeb.Application.Ports.Driving
{
    public class FindPersonByName(string name) : IQuery<FindPersonResult>
    {
        public string Name { get; } = name;
    }
}
