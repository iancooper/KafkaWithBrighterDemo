using System;
using System.Collections.Generic;

namespace Transmogrifier.Application;

public class Person
{
    public DateTime TimeStamp { get; set; }
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Transmogrification> Transmogrifications { get; set; } = new List<Transmogrification>();
    
    public Person(){ /*Required for Dapper*/}

    public Person(string name)
    {
        Name = name;
    }

    public Person(int id, string name)
    {
        Id = id;
        Name = name;
    }
}