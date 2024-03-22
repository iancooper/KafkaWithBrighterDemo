using Paramore.Darker;
using TransmogrifierAPI.Adapters.Driving;

namespace TransmogrifierAPI.Application.Ports.Driving
{
    public class FindPersonByName(string name) : IQuery<FindPersonResult>
    {
        public string Name { get; } = name;
    }
}
