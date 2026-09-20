namespace Unhallowed.PatternDemos;

/// <summary>
/// One runnable demonstration of one design pattern.
/// Requirement 10: every pattern must be demonstrable from a working main() method.
/// </summary>
public interface IPatternDemo
{
    /// <summary>Command-line key, e.g. <c>decorator</c>.</summary>
    string Key { get; }

    /// <summary>Catalogue number, matching the file name under docs/patterns/.</summary>
    int Number { get; }

    /// <summary>Pattern name as it appears in the report.</summary>
    string Title { get; }

    /// <summary>One line on what this demo proves.</summary>
    string Summary { get; }

    void Run();
}

/// <summary>Small console helpers so every demo prints in the same shape.</summary>
public static class DemoConsole
{
    public static void Header(IPatternDemo demo)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 74));
        Console.WriteLine($"  {demo.Number:00}. {demo.Title.ToUpperInvariant()}");
        Console.WriteLine($"  {demo.Summary}");
        Console.WriteLine($"  docs/patterns/{demo.Number:00}-{demo.Key}.md");
        Console.WriteLine(new string('=', 74));
        Console.WriteLine();
    }

    public static void Step(string text) => Console.WriteLine($"  -> {text}");

    public static void Note(string text) => Console.WriteLine($"     {text}");

    public static void Section(string text)
    {
        Console.WriteLine();
        Console.WriteLine($"  [{text}]");
    }
}
