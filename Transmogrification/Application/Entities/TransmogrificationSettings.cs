using System;

namespace Transmogrification.Application.Entities
{
    public class TransmogrificationSettings
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Transformation { get; set; } = string.Empty; 
        
        public TransmogrificationSettings() {}
        
        public TransmogrificationSettings(string name, string transformation)
        {
            Name = name;
            Transformation = transformation;
        }
    }
}
