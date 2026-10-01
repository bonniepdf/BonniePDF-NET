using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Definition;

/// <summary>The shared Document Definition conformance fixtures, as generated from the specification.</summary>
internal static class DefinitionFixtures
{
    private static string Root => Path.Combine(AppContext.BaseDirectory, "Definition", "Fixtures");

    public static IEnumerable<string> Names(string kind) =>
        Directory.GetFiles(Path.Combine(Root, kind), "*.json").Select(f => Path.GetFileNameWithoutExtension(f)!).Order(StringComparer.Ordinal);

    public static string Text(string kind, string name) => File.ReadAllText(Path.Combine(Root, kind, name + ".json"));

    public static JsonDocument Parse(string kind, string name) => JsonDocument.Parse(Text(kind, name));

    public static TheoryData<string> Theory(string kind)
    {
        TheoryData<string> data = new();
        foreach (string name in Names(kind)) data.Add(name);
        return data;
    }
}
