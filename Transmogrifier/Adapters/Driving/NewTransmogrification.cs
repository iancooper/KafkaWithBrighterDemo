namespace Transmogrifier.Adapters.Driving;

public class NewTransmogrification(string transmogrification)
{
    public string Transmogrification { get; set; } = transmogrification;

    public NewTransmogrification() : this(string.Empty) { }
}