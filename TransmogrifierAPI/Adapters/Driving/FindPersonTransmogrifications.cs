using System;
using System.Collections.Generic;

namespace TransmogrifierAPI.Adapters.Driving;

public class FindPersonTransmogrifications(string name, IEnumerable<string> transmogrifications)
{
    public string Name { get; } = name;
    public IEnumerable<string> Transmogrifications { get; } = transmogrifications;
    public FindPersonTransmogrifications() : this(string.Empty, Array.Empty<string>()) { }
}