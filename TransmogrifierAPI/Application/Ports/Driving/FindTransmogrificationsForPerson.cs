using Paramore.Darker;
using TransmogrifierAPI.Adapters.Driving;

namespace TransmogrifierAPI.Application.Ports.Driving;

public class FindTransmogrificationsForPerson(string name): IQuery<FindPersonTransmogrifications>
{
    public string Name { get; } = name;
}