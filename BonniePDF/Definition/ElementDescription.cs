using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BonniePDF.Definition;

internal sealed class KeySpec
{
    public string Kind { get; init; } = "";
    public bool Required { get; init; }
    public double? Min { get; init; }
    public double? Max { get; init; }
}

internal sealed class EnumSpec
{
    public List<string> Tokens { get; init; } = [];
    public string Via { get; init; } = "options";
    public string? Type { get; init; }
    public Dictionary<string, string>? Members { get; init; }
    public DotnetEnum? Dotnet { get; init; }
}

internal sealed class DotnetEnum
{
    public string? Type { get; init; }
    public Dictionary<string, string>? Members { get; init; }
}

internal class ShapeSpec
{
    public List<string>? Uses { get; init; }
    public Dictionary<string, KeySpec> Keys { get; init; } = [];
    public List<List<string>>? ExactlyOne { get; init; }
    public List<List<string>>? Conflicts { get; init; }
    public List<string>? Secrets { get; init; }
    public bool AtLeastOne { get; init; }
    public Dictionary<string, string>? Refs { get; init; }
    public Dictionary<string, string>? Defines { get; init; }
    public List<string>? Rules { get; init; }
}

internal sealed class LimitsData
{
    public long InputBytes { get; init; }
    public int JsonDepth { get; init; }
    public int ElementDepth { get; init; }
    public int ElementNodes { get; init; }
    public int Pages { get; init; }
    public int StringLength { get; init; }
    public int ArrayLength { get; init; }
    public long ResourceBytes { get; init; }
    public long ResourceBytesTotal { get; init; }
    public int ChartDataPoints { get; init; }
}

internal sealed class PaletteData
{
    public List<string> Families { get; init; } = [];
    public List<int> Shades { get; init; } = [];
}

internal sealed class VersionData
{
    public int Version { get; init; }
    public int MinorVersion { get; init; }
}

internal sealed class SettingsCondition
{
    public string Key { get; init; } = "";
    public List<string>? In { get; init; }
    public List<string>? NotIn { get; init; }
    public bool? Present { get; init; }
}

internal sealed class SettingsRule
{
    public string Id { get; init; } = "";
    public string Outcome { get; init; } = "error";
    public List<SettingsCondition> When { get; init; } = [];
    public List<SettingsCondition>? Require { get; init; }
    public Dictionary<string, string>? Set { get; init; }
    public string Text { get; init; } = "";
}

internal sealed class SettingsRulesData
{
    public Dictionary<string, string> Defaults { get; init; } = [];
    public List<SettingsRule> Rules { get; init; } = [];
}

internal sealed class DescriptionData
{
    public VersionData Definition { get; init; } = new();
    public LimitsData Limits { get; init; } = new();
    public PaletteData Palette { get; init; } = new();
    public List<string> PageSizes { get; init; } = [];
    public Dictionary<string, EnumSpec> Enums { get; init; } = [];
    public Dictionary<string, Dictionary<string, KeySpec>> Mixins { get; init; } = [];
    public Dictionary<string, ShapeSpec> Shapes { get; init; } = [];
    public Dictionary<string, ShapeSpec> Elements { get; init; } = [];
    public ShapeSpec Root { get; init; } = new();
    public Dictionary<string, ShapeSpec> Objects { get; init; } = [];
    public SettingsRulesData SettingsRules { get; init; } = new();
}

/// <summary>
/// The Document Definition element description: elements, keys, kinds and tokens. It is generated from
/// the specification and embedded, so neither this validator nor the reader hand-maintains a table.
/// </summary>
internal static class ElementDescription
{
    private static readonly ConcurrentDictionary<ShapeSpec, Dictionary<string, KeySpec>> KeyCache = new();

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public static DescriptionData Data { get; } = Load();

    private static DescriptionData Load()
    {
        var assembly = typeof(ElementDescription).Assembly;
        string name = Array.Find(assembly.GetManifestResourceNames(), n => n.EndsWith("ElementDescription.json", StringComparison.Ordinal))
            ?? throw new InvalidOperationException("The embedded Document Definition element description is missing.");
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<DescriptionData>(stream, ReadOptions)
            ?? throw new InvalidOperationException("The Document Definition element description is empty.");
    }

    /// <summary>The keys of a shape or element, merged with the mixins it uses.</summary>
    public static Dictionary<string, KeySpec> KeysOf(ShapeSpec spec) =>
        KeyCache.GetOrAdd(spec, static s =>
        {
            var merged = new Dictionary<string, KeySpec>(StringComparer.Ordinal);
            foreach (string use in s.Uses ?? [])
                foreach (KeyValuePair<string, KeySpec> pair in Data.Mixins[use]) merged[pair.Key] = pair.Value;
            foreach (KeyValuePair<string, KeySpec> pair in s.Keys) merged[pair.Key] = pair.Value;
            return merged;
        });

    /// <summary>A shape by name, from the shared shapes or the top-level objects.</summary>
    public static ShapeSpec Shape(string name) =>
        Data.Shapes.TryGetValue(name, out ShapeSpec? shape) ? shape : Data.Objects[name];
}
