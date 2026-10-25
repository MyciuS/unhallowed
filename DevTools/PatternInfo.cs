namespace Isaac.DevTools;

/// <summary>One design pattern the course requires (requirement 10).</summary>
/// <param name="Letter">The sub-point of requirement 10 with the pattern's specific rule.</param>
/// <param name="Markers">
/// Names that must exist in the build for the pattern to count as merged: type names, or
/// <c>Name()</c> for a method that some type declares.
/// </param>
public sealed record PatternInfo(string Letter, string Name, string Requirement, string Where, string[] Markers);
