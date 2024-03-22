using Spectre.Console;
using Transmogrification.Application;

namespace Transmogrification.Adapters.Driven;

public interface IBox
{
    bool StarTransformation();
    void BeginTransforming();
    void WriteRow(TransmogrificationHappened transmogrificationHappened);
    void WriteDivider(string text);
    void EndTransforming();
}

public class Box : IBox
{
    public bool StarTransformation()
    {
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("[green]Begin Transmogrification?[/]");
    }

    public void BeginTransforming()
    {
        WriteDivider("Transformations");
    }

    public void WriteRow(TransmogrificationHappened transmogrificationHappened)
    {
        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Transformation");
        table.AddColumn("Result");

        table.AddRow(transmogrificationHappened.Name, transmogrificationHappened.Transmogrification, "Success");
        
        AnsiConsole.Write(table);
    }

    public void WriteDivider(string text)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[yellow]{text}[/]").RuleStyle("grey").LeftJustified());
    }

    public void EndTransforming()
    {
        WriteDivider("End of Transformations");
    }
}