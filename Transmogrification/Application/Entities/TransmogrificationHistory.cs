using System;

namespace Transmogrification.Application.Entities
{
    public class TransmogrificationHistory(string name, string transmogrification)
    {
        public string Name { get; set; } = name;
        public string Transmogrification { get; set; } = transmogrification; 
        
        public TransmogrificationHistory(): this(string.Empty, string.Empty) {}
   }
}
