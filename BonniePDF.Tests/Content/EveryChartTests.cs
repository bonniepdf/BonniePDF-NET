using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Every chart type must work in a document. <c>chart-types.json</c> is generated with the chart sources (see
/// <c>BonniePDF/chart-sources.json</c>): one shared case per chart type, taken from the chart feature fixtures, with
/// options written as TypeScript options objects. The TypeScript suite runs the same cases (every-chart.test.ts).
/// </summary>
public class EveryChartTests
{
    /// <summary>
    /// Chart types that Document Definition 1.0 does not describe yet. Remove a type from this list when its
    /// element description lands; <see cref="IsDescribedByDocumentDefinitionOrListedAsNotYet"/> fails if a listed
    /// type is already described.
    /// </summary>
    private static readonly string[] NotYetInDefinitions =
    [
        "ArcDiagramChart", "BoxPlotChart", "CandlestickChart", "ChordChart", "DumbbellChart", "FunnelChart",
        "GanttChart", "GaugeChart", "HistogramChart", "NightingaleRoseChart", "PackedCircleChart", "PartitionChart",
        "PictographChart", "ProgressRingChart", "PyramidChart", "RadarChart", "RadialBarChart", "RangeAreaChart",
        "SankeyChart", "SlopeChart", "StatTileChart", "SunburstChart", "TimelineChart", "TreemapChart",
        "VennChart", "WaffleChart",
    ];

    private static readonly Assembly Library = typeof(ChartBase).Assembly;

    private static JsonArray Cases() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Content", "Fixtures", "chart-types.json")))!.AsArray();

    public static TheoryData<string> ChartTypes()
    {
        TheoryData<string> data = [];
        foreach (JsonNode? item in Cases()) data.Add(item!["chart"]!.GetValue<string>());
        return data;
    }

    private static JsonObject CaseOf(string chart) =>
        Cases().Select(n => n!.AsObject()).Single(c => string.Equals(c["chart"]!.GetValue<string>(), chart, StringComparison.Ordinal));

    [Fact]
    public void HasOneCaseForEachPublicChartType()
    {
        string[] declared = Library.GetTypes()
            .Where(t => t.IsPublic && !t.IsAbstract && t.IsSubclassOf(typeof(ChartBase)))
            .Select(t => t.Name).Order(StringComparer.Ordinal).ToArray();
        string[] listed = Cases().Select(n => n!["chart"]!.GetValue<string>()).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(declared, listed);
        Assert.All(NotYetInDefinitions, name => Assert.Contains(name, listed, StringComparer.Ordinal));
    }

    private static ChartBase Build(string chart)
    {
        Type type = Library.GetType("BonniePDF." + chart, throwOnError: true)!;
        ChartBase built = (ChartBase)Activator.CreateInstance(type)!;
        Apply(built, CaseOf(chart)["options"]!.AsObject());
        return built;
    }

    [Theory]
    [MemberData(nameof(ChartTypes))]
    public void RendersAsATaggedFigureWithItsAltText(string chart)
    {
        ChartBase built = Build(chart);
        string altText = $"{chart} sample";
        built.AltText = altText;

        using BonniePDF.Document doc = new();
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Add(built);
        using MemoryStream ms = new();
        doc.Save(ms, true);
        string pdf = Encoding.Latin1.GetString(ms.ToArray());

        Assert.Contains("/Figure", pdf, StringComparison.Ordinal);
        Assert.Contains($"/Alt ({altText})", pdf, StringComparison.Ordinal);
        Assert.DoesNotContain(doc.Diagnostics, d => d.Severity == DiagnosticSeverity.Error);
    }

    [Theory]
    [MemberData(nameof(ChartTypes))]
    public void PreflightReportsTheChartWhenItHasNoAltText(string chart)
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Add(Build(chart));

        LayoutSnapshot? snapshot = doc.GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true }).Snapshot;
        Assert.NotNull(snapshot);
        ComplianceReport report = CompliancePreflight.Evaluate(snapshot);
        Assert.Contains(report.Findings, f => string.Equals(f.Code, "figure-missing-alt", StringComparison.Ordinal)
            && f.Message.StartsWith(chart + " ", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(ChartTypes))]
    public void IsDescribedByDocumentDefinitionOrListedAsNotYet(string chart)
    {
        bool described = ElementDescription.Data.Elements.ContainsKey(chart);
        Assert.True(described != NotYetInDefinitions.Contains(chart, StringComparer.Ordinal),
            described
                ? $"{chart} is described: remove it from NotYetInDefinitions."
                : $"{chart} is not described by Document Definition and is not listed in NotYetInDefinitions.");
    }

    /// <summary>Sets properties from TypeScript-style option names, creating nested objects and lists as needed.</summary>
    private static void Apply(object target, JsonObject options)
    {
        Type type = target.GetType();
        foreach (var (key, value) in options)
        {
            string propertyName = char.ToUpperInvariant(key[0]) + key[1..];
            // A custom theme object is `CustomTheme` in .NET; `Theme` stays a preset name.
            if (string.Equals(key, "theme", StringComparison.Ordinal) && value is JsonObject) propertyName = "CustomTheme";
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
                ?? throw new InvalidOperationException($"{type.Name} has no property {propertyName} (from option '{key}').");
            if (value is null) { property.SetValue(target, null); continue; }
            object? current = property.CanRead && value is JsonObject && !IsList(property.PropertyType) ? property.GetValue(target) : null;
            object? converted = Convert(value, property.PropertyType, current);
            if (property.CanWrite) property.SetValue(target, converted);
        }
    }

    private static bool IsList(Type t) => t != typeof(string) && (t.IsArray || typeof(IEnumerable).IsAssignableFrom(t));

    /// <summary>Creates the subclass that an object's <c>type</c> member selects, then applies its other members.</summary>
    private static object Typed(JsonNode node, Func<string, object> create)
    {
        JsonObject json = node.AsObject();
        object created = create(json["type"]!.GetValue<string>());
        Apply(created, new JsonObject(json.Where(kv => !string.Equals(kv.Key, "type", StringComparison.Ordinal)).Select(kv => new KeyValuePair<string, JsonNode?>(kv.Key, kv.Value?.DeepClone()))));
        return created;
    }

    private static object? Convert(JsonNode node, Type target, object? current)
    {
        Type type = Nullable.GetUnderlyingType(target) ?? target;
        if (type == typeof(string)) return node.GetValue<string>();
        if (type == typeof(bool)) return node.GetValue<bool>();
        if (type == typeof(int)) return node.GetValue<int>();
        if (type == typeof(float)) return node.GetValue<float>();
        if (type == typeof(double)) return node.GetValue<double>();
        if (type == typeof(decimal)) return node.GetValue<decimal>();
        if (type == typeof(Color)) return Color.FromHex(node.GetValue<string>());
        if (type.IsEnum) return Enum.Parse(type, node.GetValue<string>(), ignoreCase: true);
        if (type == typeof(DateTimeOffset))
            return DateTimeOffset.Parse(node.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        if (type == typeof(object))
            return node is JsonValue v && v.TryGetValue(out int index) ? index : node.GetValue<string>();
        if (ConvertTyped(node, type) is { } typedValue) return typedValue;
        if (IsList(type))
        {
            Type element = type.IsArray ? type.GetElementType()! : type.GetGenericArguments()[0];
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;
            foreach (JsonNode? child in node.AsArray()) list.Add(child is null ? null : Convert(child, element, null));
            if (!type.IsArray) return list;
            Array array = Array.CreateInstance(element, list.Count);
            list.CopyTo(array, 0);
            return array;
        }
        object instance = current ?? Activator.CreateInstance(type)!;
        Apply(instance, node.AsObject());
        return instance;
    }

    /// <summary>Fills, annotations and effects: an object whose <c>type</c> member selects the subclass (a fill may also be a colour).</summary>
    private static object? ConvertTyped(JsonNode node, Type type)
    {
        if (type == typeof(ChartFill))
        {
            if (node is JsonValue) return ChartFill.Solid(Color.FromHex(node.GetValue<string>()));
            return Typed(node, kind => kind switch
            {
                "linear" => new ChartLinearFill(),
                "radial" => new ChartRadialFill(),
                _ => throw new InvalidOperationException($"Unknown fill type '{kind}'."),
            });
        }
        if (type == typeof(ChartAnnotation))
        {
            return Typed(node, kind => kind switch
            {
                "text" => new ChartTextAnnotation { At = new(), Text = "" },
                "callout" => new ChartCalloutAnnotation { Target = new(), Text = "" },
                "arrow" => new ChartArrowAnnotation { From = new(), To = new() },
                "growth" => new ChartGrowthAnnotation { From = new(), To = new() },
                "cagr" => new ChartCagrAnnotation { From = new(), To = new() },
                "bracket" => new ChartBracketAnnotation { FromCategory = 0, ToCategory = 0 },
                "range" => new ChartRangeAnnotation { FromCategory = 0, ToCategory = 0, Fill = ChartFill.Solid(Color.Black) },
                "highlight" => new ChartHighlightAnnotation(),
                "marker" => new ChartMarkerAnnotation { Target = new() },
                _ => throw new InvalidOperationException($"Unknown annotation type '{kind}'."),
            });
        }
        if (type == typeof(ChartEffect))
        {
            return Typed(node, kind => kind switch
            {
                "shadow" => new ChartShadowEffect(),
                "glow" => new ChartGlowEffect(),
                "gloss" => new ChartGlossEffect(),
                _ => throw new InvalidOperationException($"Unknown effect type '{kind}'."),
            });
        }
        return null;
    }
}
