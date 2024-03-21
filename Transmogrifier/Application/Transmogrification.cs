namespace Transmogrifier.Application;

public class Transmogrification
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty; 
    public long RecipientId { get; set; }
        
    public Transmogrification() {}
        
    public Transmogrification(string description, Person recipient)
    {
        Description = description;
        RecipientId = recipient.Id;
    }
}