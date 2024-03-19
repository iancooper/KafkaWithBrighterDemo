using Paramore.Darker;
using Transmogrifier.Adapters.Driving;

namespace Transmogrifier.Application.Ports.Driving;

public class FindTransmogrificationsForPerson(string name): IQuery<FindPersonTransmogrifications>
{
    public string Name { get; } = name;
}