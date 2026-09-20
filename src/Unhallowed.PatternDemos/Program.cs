using Unhallowed.PatternDemos;

// Requirement 10: every pattern is demonstrable from a working main() method.
//
//   dotnet run --project src/Unhallowed.PatternDemos                 -> the catalogue
//   dotnet run --project src/Unhallowed.PatternDemos -- decorator    -> one pattern
//   dotnet run --project src/Unhallowed.PatternDemos -- 9            -> the same, by number
//   dotnet run --project src/Unhallowed.PatternDemos -- all          -> everything implemented

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "list";

switch (command)
{
    case "list":
    case "--list":
    case "-l":
        PrintCatalog();
        return 0;

    case "all":
        foreach (var entry in PatternCatalog.Implemented)
        {
            RunOne(entry);
        }

        return 0;

    default:
        var found = PatternCatalog.Find(command);
        if (found is null)
        {
            Console.Error.WriteLine($"Unknown pattern '{command}'.");
            Console.Error.WriteLine();
            PrintCatalog();
            return 1;
        }

        if (found.Demo is null)
        {
            Console.Error.WriteLine(
                $"'{found.Title}' has no demo yet - it is still on the to-do list.");
            Console.Error.WriteLine($"Planned use: {found.GameUse}");
            Console.Error.WriteLine(
                $"Implement it, then register the demo in PatternCatalog.cs (entry {found.Number}).");
            return 1;
        }

        RunOne(found);
        return 0;
}

static void RunOne(PatternEntry entry)
{
    DemoConsole.Header(entry.Demo!);
    entry.Demo!.Run();
    Console.WriteLine();
}

static void PrintCatalog()
{
    var all = PatternCatalog.All;
    var done = PatternCatalog.Implemented.Count();

    Console.WriteLine();
    Console.WriteLine("  UNHALLOWED - design pattern catalogue");
    Console.WriteLine($"  {done} of {all.Count} demonstrable. The course requires 22 - drop one.");
    Console.WriteLine();

    foreach (var group in all.GroupBy(p => p.Category))
    {
        Console.WriteLine($"  {group.Key.ToString().ToUpperInvariant()}");

        foreach (var entry in group)
        {
            var status = entry.IsImplemented ? "[demo]" : "[todo]";
            var owner = string.IsNullOrWhiteSpace(entry.Owner) ? "unclaimed" : entry.Owner;

            Console.WriteLine($"   {entry.Number,2}. {status} {entry.Title,-25} {entry.Key,-26} {owner}");
            Console.WriteLine($"       {entry.GameUse}");
        }

        Console.WriteLine();
    }

    Console.WriteLine("  Run one:  dotnet run --project src/Unhallowed.PatternDemos -- <key|number>");
    Console.WriteLine("  Run all:  dotnet run --project src/Unhallowed.PatternDemos -- all");
    Console.WriteLine();
}
