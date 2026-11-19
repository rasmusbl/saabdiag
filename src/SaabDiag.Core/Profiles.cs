using System.Text.Json;
namespace SaabDiag.Core;

public static class Profiles
{
    public static IReadOnlyList<int> Years { get; } = Array.AsReadOnly(Enumerable.Range(2003, 10).ToArray());
    public static string Simulation(int year) => $"saab-93ii-{year}-simulation-v1";
    public static bool Supports(string profile) => Years.Any(y => Simulation(y) == profile);
}
