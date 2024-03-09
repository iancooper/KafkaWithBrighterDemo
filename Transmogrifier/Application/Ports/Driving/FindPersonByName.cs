using Paramore.Darker;
using Transmogrifier.Adapters.Driving;

namespace Transmogrifier.Application.Ports.Driving
{
    public class FindPersonByName(string name) : IQuery<FindPersonResult>
    {
        public string Name { get; } = name;
    }
}
