using System.Collections.Generic;

namespace Transmogrifier.Adapters.Driving
{
    public class FindPersonsGreetings
    {
        public string Name { get; set; }
        public IEnumerable<Salutation> Greetings { get;set; }

    }

    public class Salutation
    {
        public string Words { get; set; }
        
        public Salutation(string words)
        {
            Words = words;
        }

    }
}
