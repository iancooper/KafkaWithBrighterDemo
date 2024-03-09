using GreetingsWeb.Application.Entities;

namespace GreetingsWeb.Adapters.Driving
{
    public class FindPersonResult(Person person)
    {
         public Person Person { get; private set; } = person;
    }
}
