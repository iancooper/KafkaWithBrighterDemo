using System;

namespace Transmogrification.Application.Entities
{
    public class TransmogrificationResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Transformation { get; set; } = string.Empty; 
        
        public TransmogrificationResult() {}
        
        public TransmogrificationResult(string name, string transformation)
        {
            Name = name;
            Transformation = transformation;
        }
    }
}
