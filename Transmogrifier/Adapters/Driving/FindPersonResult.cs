using Transmogrifier.Application.Entities;

namespace Transmogrifier.Adapters.Driving
{
    public class FindPersonResult(Person person)
    {
         public Person Person { get; private set; } = person;
    }
}
