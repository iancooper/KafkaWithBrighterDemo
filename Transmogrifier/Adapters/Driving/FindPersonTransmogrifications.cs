using System.Collections.Generic;

namespace Transmogrifier.Adapters.Driving;

public class FindPersonTransmogrifications(string name, IEnumerable<string> transmogrifications)
{
    public string Name { get; } = name;
    public IEnumerable<string> Transmogrifications { get; } = transmogrifications;
}