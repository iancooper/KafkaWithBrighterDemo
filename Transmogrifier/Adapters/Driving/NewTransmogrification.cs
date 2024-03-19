namespace Transmogrifier.Adapters.Driving;

public class NewTransmogrification(string name, string transmogrification)
{
    public string Name { get; } = name;
    public string Transmogrification { get; } = transmogrification;
}