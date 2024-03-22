using TransmogrifierAPI.Application;

namespace TransmogrifierAPI.Adapters.Driving
{
    public class FindPersonResult(Person person)
    {
         public Person Person { get; set; } = person;

         public FindPersonResult() : this(null) { }
    }
}
