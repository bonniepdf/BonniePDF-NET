using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace BonniePDF;

/// <summary>
/// Writes a <see cref="Document"/> as a Document Definition. The inverse of
/// <see cref="DefinitionReader"/>: driven by the same element description, it reads the same public
/// properties the reader sets, and leaves out what a freshly constructed element already holds.
/// </summary>
internal sealed partial class DefinitionWriter
{
    private static readonly DescriptionData D = ElementDescription.Data;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.:-]{0,127}$", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex IdPattern();

    private static readonly HashSet<string> SlotKinds = new(StringComparer.Ordinal)
    {
        "element", "elements", "stackChildren", "listItems", "spans", "rows", "cells", "cellContent",
    };

    /// <summary>
    /// Every element in the catalogue: its class (matched exactly: a subclass is custom behaviour), its
    /// catalogue name, and a fresh element for its defaults.
    /// </summary>
    private static readonly (Type Class, string Name, Func<object> Blank)[] Catalogue =
    [
        (typeof(Flip), "Flip", () => new Flip()),
        (typeof(Translate), "Translate", () => new Translate()),
        (typeof(Rotate), "Rotate", () => new Rotate()),
        (typeof(Container), "Container", () => new Container()),
        (typeof(VerticalStack), "VerticalStack", () => new VerticalStack()),
        (typeof(HorizontalStack), "HorizontalStack", () => new HorizontalStack()),
        (typeof(Layers), "Layers", () => new Layers()),
        (typeof(Spacer), "Spacer", () => new Spacer()),
        (typeof(PageBreak), "PageBreak", () => new PageBreak()),
        (typeof(SemanticGroup), "SemanticGroup", () => new SemanticGroup(SemanticRole.Div)),
        (typeof(EnsureSpace), "EnsureSpace", () => new EnsureSpace()),
        (typeof(PreventPageBreak), "PreventPageBreak", () => new PreventPageBreak()),
        (typeof(ShowEntire), "ShowEntire", () => new ShowEntire()),
        (typeof(StopPaging), "StopPaging", () => new StopPaging()),
        (typeof(ShowOnce), "ShowOnce", () => new ShowOnce()),
        (typeof(SkipOnce), "SkipOnce", () => new SkipOnce()),
        (typeof(Unconstrained), "Unconstrained", () => new Unconstrained()),
        (typeof(ShowIf), "ShowIf", () => new ShowIf()),
        (typeof(TextBlock), "TextBlock", () => new TextBlock { Text = string.Empty }),
        (typeof(Heading), "Heading", () => new Heading { Text = string.Empty, TocIdentifier = string.Empty }),
        (typeof(RichText), "RichText", () => new RichText()),
        (typeof(PageNumberOf), "PageNumberOf", () => new PageNumberOf("a")),
        (typeof(VisualToc), "VisualToc", () => new VisualToc()),
        (typeof(Hyperlink), "Hyperlink", () => new Hyperlink("a")),
        (typeof(SectionLink), "SectionLink", () => new SectionLink("a", "a")),
        (typeof(Section), "Section", () => new Section("a")),
        (typeof(UnorderedList), "UnorderedList", () => new UnorderedList()),
        (typeof(OrderedList), "OrderedList", () => new OrderedList()),
        (typeof(Table), "Table", () => new Table()),
        (typeof(Grid), "Grid", () => new Grid()),
        (typeof(Image), "Image", () => new Image()),
        (typeof(Svg), "Svg", () => new Svg()),
        (typeof(Line), "Line", () => new Line()),
        (typeof(Circle), "Circle", () => new Circle()),
        (typeof(Ellipse), "Ellipse", () => new Ellipse()),
        (typeof(QRCode), "QRCode", () => new QRCode()),
        (typeof(Barcode), "Barcode", () => new Barcode()),
        (typeof(DataMatrix), "DataMatrix", () => new DataMatrix()),
        (typeof(BarChart), "BarChart", () => new BarChart()),
        (typeof(GroupedBarChart), "GroupedBarChart", () => new GroupedBarChart()),
        (typeof(LineChart), "LineChart", () => new LineChart()),
        (typeof(AreaChart), "AreaChart", () => new AreaChart()),
        (typeof(DotPlotChart), "DotPlotChart", () => new DotPlotChart()),
        (typeof(LollipopChart), "LollipopChart", () => new LollipopChart()),
        (typeof(PieChart), "PieChart", () => new PieChart()),
    ];

    private static readonly Dictionary<Type, string> TypeByClass = Catalogue.ToDictionary(e => e.Class, e => e.Name);

    private static readonly Dictionary<string, Func<object>> Blanks = Catalogue.ToDictionary(e => e.Name, e => e.Blank, StringComparer.Ordinal);

    private static readonly Dictionary<string, object> BlankCache = new(StringComparer.Ordinal);

    private static readonly Dictionary<int, string> WeightTokens = new()
    {
        [100] = "thin", [200] = "extra-light", [300] = "light", [400] = "normal", [500] = "medium",
        [600] = "semi-bold", [700] = "bold", [800] = "extra-bold", [900] = "black",
    };

    private static Dictionary<string, string>? _paletteByHex;

    private readonly Document _doc;
    private readonly DefinitionWriteOptions _options;
    private readonly bool _drop;
    private readonly List<DefinitionIssue> _notes = [];
    private readonly List<DefinitionIssue> _unrepresentable = [];
    private readonly List<ResourceEntry> _resources = [];
    private readonly Dictionary<string, int> _counters = new(StringComparer.Ordinal);
    private readonly HashSet<string> _usedIds = new(StringComparer.Ordinal);

    private DefinitionWriter(Document doc, DefinitionWriteOptions options)
    {
        _doc = doc;
        _options = options;
        _drop = options.OnUnrepresentable == DefinitionUnrepresentable.Drop;
    }

    private sealed record ResourceEntry(string Name, string Kind, byte[] Bytes, JsonObject Body);

    private enum SlotOutcome
    {
        /// <summary>An ordinary key.</summary>
        Ordinary,

        /// <summary>The key is handled; write <see cref="Slot.Node"/> (or nothing when it is null).</summary>
        Written,

        /// <summary>The element cannot be written and has been reported.</summary>
        DropElement,
    }

    private readonly record struct Slot(SlotOutcome Outcome, JsonNode? Node = null)
    {
        public static Slot Ordinary => new(SlotOutcome.Ordinary);

        public static Slot Drop => new(SlotOutcome.DropElement);

        public static Slot Write(JsonNode? node) => new(SlotOutcome.Written, node);
    }

    // ── entry ────────────────────────────────────────────────────────────────

    internal static DefinitionWriteResult Write(Document doc, DefinitionWriteOptions? options)
    {
        options ??= new DefinitionWriteOptions();
        if (options.Target is not null && !string.Equals(options.Target, $"{D.Definition.Version}.0", StringComparison.Ordinal))
        {
            throw new DefinitionException([
                new DefinitionIssue(string.Empty, "unsupported-version",
                    $"This release writes definition version {D.Definition.Version}.0; {options.Target} was requested.")
                {
                    Expected = $"{D.Definition.Version}.0",
                    Found = options.Target,
                },
            ]);
        }

        var writer = new DefinitionWriter(doc, options);
        JsonObject definition = writer.Definition();
        if (writer._unrepresentable.Count > 0) throw new DefinitionException(writer._unrepresentable);

        // A definition this writer produces must read back; anything the reader would refuse is reported now.
        DefinitionValidationResult check = DocumentDefinition.Validate(definition.ToJsonString());
        if (!check.IsValid) throw new DefinitionException(check.Errors);
        return new DefinitionWriteResult(definition, writer._notes);
    }

    // ── problems ─────────────────────────────────────────────────────────────

    private void Cannot(string path, string? id, string message)
    {
        List<DefinitionIssue> target = _drop ? _notes : _unrepresentable;
        if (target.Exists(i => string.Equals(i.Path, path, StringComparison.Ordinal) && string.Equals(i.Message, message, StringComparison.Ordinal))) return;
        target.Add(new DefinitionIssue(path, _drop ? "dropped" : "unrepresentable", message) { Id = id });
    }

    // ── values ───────────────────────────────────────────────────────────────

    private static string Hex2(int n) => n.ToString("x2", CultureInfo.InvariantCulture);

    private string Colour(Color color)
    {
        string rgb = $"#{Hex2(color.R)}{Hex2(color.G)}{Hex2(color.B)}";
        if (color.A < 255) return rgb + Hex2(color.A);
        return (_options.WritePaletteTokens ? PaletteToken(rgb) : null) ?? rgb;
    }

    private static string? PaletteToken(string hex)
    {
        if (_paletteByHex is null)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string family in D.Palette.Families)
            {
                var palette = (ColorPalette?)typeof(ColorPalette).GetProperty(char.ToUpperInvariant(family[0]) + family[1..], BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (palette is null) continue;
                foreach (int shade in D.Palette.Shades) map.TryAdd(palette[shade].ToHexString(), $"{family}-{shade}");
            }
            _paletteByHex = map;
        }
        return _paletteByHex.GetValueOrDefault(hex);
    }

    private static JsonNode? Number(object? value) => value switch
    {
        null => null,
        float f => JsonValue.Create(f),
        double d => JsonValue.Create(d),
        decimal m => JsonValue.Create(m),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        short s => JsonValue.Create(s),
        byte b => JsonValue.Create(b),
        _ => throw new InvalidOperationException($"{value.GetType().Name} is not a number."),
    };

    private static JsonNode Sides(float left, float top, float right, float bottom)
    {
        if (left == top && top == right && right == bottom) return JsonValue.Create(left);
        var sides = new JsonObject();
        if (top != 0) sides["top"] = top;
        if (right != 0) sides["right"] = right;
        if (bottom != 0) sides["bottom"] = bottom;
        if (left != 0) sides["left"] = left;
        return sides;
    }

    private static JsonNode PageSizeWire(PdfSize size)
    {
        PdfSize portrait = size.Portrait();
        foreach (string token in D.PageSizes)
        {
            PdfSize named = ((PdfSize)typeof(PdfSize).GetProperty(char.ToUpperInvariant(token[0]) + token[1..], BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!).Portrait();
            if (named.Width == portrait.Width && named.Height == portrait.Height) return token;
        }
        return new JsonObject { ["width"] = portrait.Width, ["height"] = portrait.Height };
    }

    private static JsonNode PermissionsWire(PdfPermissions mask)
    {
        if (mask == PdfPermissions.All) return "all";
        if (mask == PdfPermissions.None) return "none";
        var list = new JsonArray();
        foreach (string token in D.Enums["PdfPermission"].Tokens)
        {
            var flag = (PdfPermissions)DefinitionReader.EnumFromToken(typeof(PdfPermissions), token);
            if ((mask & flag) != 0) list.Add(token);
        }
        return list;
    }

    private static JsonNode TextDecorationWire(TextDecoration flags)
    {
        var parts = new List<string>();
        if ((flags & TextDecoration.Underline) != 0) parts.Add("underline");
        if ((flags & TextDecoration.Strikethrough) != 0) parts.Add("strikethrough");
        if ((flags & TextDecoration.Overline) != 0) parts.Add("overline");
        if (parts.Count == 0) return "none";
        if (parts.Count == 1) return parts[0];
        var list = new JsonArray();
        foreach (string part in parts) list.Add(part);
        return list;
    }

    /// <summary>The token of a stored enumeration value, or null when it has none.</summary>
    private static string? EnumToken(string enumName, object value)
    {
        EnumSpec spec = D.Enums[enumName];
        if (value is string text)
        {
            // A property that holds the value as text (the chart's shape rendering).
            if (spec.Members is not null)
            {
                foreach (KeyValuePair<string, string> pair in spec.Members)
                    if (string.Equals(pair.Value, text, StringComparison.Ordinal)) return pair.Key;
            }
            return spec.Tokens.Contains(text, StringComparer.Ordinal) ? text : null;
        }
        string name = value.ToString()!;
        string wanted = DefinitionReader.Normalise(name);
        foreach (string token in spec.Tokens)
        {
            if (spec.Dotnet?.Members is { } members && members.TryGetValue(token, out string? member)
                && string.Equals(member, name, StringComparison.Ordinal))
                return token;
        }
        foreach (string token in spec.Tokens)
            if (string.Equals(DefinitionReader.Normalise(token), wanted, StringComparison.OrdinalIgnoreCase)) return token;
        return null;
    }

    /// <summary>Converts one stored value to its wire form; null when the value is unset.</summary>
    private JsonNode? Wire(string kind, object? value)
    {
        if (value is null) return null;
        int colon = kind.IndexOf(':', StringComparison.Ordinal);
        string head = colon < 0 ? kind : kind[..colon];
        string arg = colon < 0 ? string.Empty : kind[(colon + 1)..];
        switch (head)
        {
            case "string" or "chartNumberFormat":
                return JsonValue.Create((string)value);
            case "boolean":
                return JsonValue.Create((bool)value);
            case "integer" or "integerOrUnlimited" or "number" or "length":
                return Number(value);
            case "color":
                return Colour(value is string text ? DefinitionReader.ToColor(text) : (Color)value);
            case "spacing":
            {
                var t = (Thickness)value;
                return Sides(t.Left, t.Top, t.Right, t.Bottom);
            }
            case "border":
            {
                var b = (Border)value;
                return Sides(b.Left, b.Top, b.Right, b.Bottom);
            }
            case "pageSize":
                return PageSizeWire((PdfSize)value);
            case "fontWeight":
            {
                int weight = (int)value;
                return WeightTokens.TryGetValue(weight, out string? token) ? token : weight;
            }
            case "decoration":
                return TextDecorationWire((TextDecoration)value);
            case "permissions":
                return PermissionsWire((PdfPermissions)value);
            case "dateTime":
                return value is DateTimeOffset offset
                    ? offset.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture)
                    : ((DateTime)value).ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
            case "enum":
                return EnumToken(arg, value) is { } enumToken ? JsonValue.Create(enumToken) : null;
            case "array":
            {
                var array = new JsonArray();
                foreach (object? item in (IEnumerable)value) array.Add(Wire(arg, item));
                return array;
            }
            case "fontFeatures":
            {
                var map=new JsonObject();
                foreach(var entry in (IReadOnlyDictionary<string,int>)value) map[entry.Key]=entry.Value;
                return map;
            }
            case "map":
            {
                var map = new JsonObject();
                foreach (DictionaryEntry entry in (IDictionary)value) map[(string)entry.Key] = Wire(arg, entry.Value);
                return map;
            }
            case "object":
                return Shape(value, ElementDescription.Shape(arg), Blank(value.GetType()), []);
            default:
                return null;
        }
    }

    private static object? Blank(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch (MissingMethodException)
        {
            return null;
        }
    }

    /// <summary>A nested value object, writing only what differs from a fresh instance. Empty when nothing does.</summary>
    private JsonObject Shape(object value, ShapeSpec spec, object? blank, HashSet<string> skip)
    {
        var result = new JsonObject();
        Type type = value.GetType();
        foreach ((string key, KeySpec keySpec) in ElementDescription.KeysOf(spec))
        {
            if (skip.Contains(key)) continue;
            PropertyInfo? property = DefinitionReader.FindProperty(type, key);
            if (property is null) continue;
            JsonNode? wire = Wire(keySpec.Kind, property.GetValue(value));
            if (wire is null) continue;
            if (!keySpec.Required && blank is not null && Same(wire, Wire(keySpec.Kind, property.GetValue(blank)))) continue;
            result[key] = wire;
        }
        return result;
    }

    private static bool Same(JsonNode? a, JsonNode? b) => JsonNode.DeepEquals(a, b);

    // ── elements ─────────────────────────────────────────────────────────────

    private string? IdOf(object element)
    {
        string? id = DebugSource.Of(element);
        if (id is null || !IdPattern().IsMatch(id) || !_usedIds.Add(id)) return null;
        return id;
    }

    private JsonArray Elements(IEnumerable<IElement> list, string path)
    {
        var array = new JsonArray();
        int index = 0;
        // Paths name the position in the document being written, so a dropped element does not renumber the rest.
        foreach (IElement child in list)
        {
            JsonObject? node = WriteElement(child, $"{path}/{index}");
            if (node is not null) array.Add(node);
            index++;
        }
        return array;
    }

    private JsonObject? WriteElement(IElement element, string path)
    {
        if (!TypeByClass.TryGetValue(element.GetType(), out string? type))
        {
            Cannot(path, null, $"{element.GetType().Name} is a custom element; a definition can only hold catalogue elements.");
            return null;
        }
        string? id = IdOf(element);
        var node = new JsonObject { ["type"] = type };
        if (id is not null) node["id"] = id;

        ShapeSpec spec = D.Elements[type];
        object blank = BlankOf(type);
        Type clr = element.GetType();
        JsonObject? fonts = element is TextElementBase text ? FontValues(text) : null;

        foreach ((string key, KeySpec keySpec) in ElementDescription.KeysOf(spec))
        {
            string at = $"{path}/{key.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)}";
            string head = keySpec.Kind.Split(':')[0];

            Slot slot = WriteSlot(type, key, element, at, id);
            if (slot.Outcome == SlotOutcome.DropElement) return null;
            if (slot.Outcome == SlotOutcome.Written)
            {
                if (slot.Node is not null) node[key] = slot.Node;
                continue;
            }
            if (SlotKinds.Contains(head)) continue;

            if (fonts is not null && key is "fontName" or "fontFamily" or "fontWeight" or "italic")
            {
                if (fonts.TryGetPropertyValue(key, out JsonNode? font) && font is not null) node[key] = font.DeepClone();
                continue;
            }

            // Values a document-level default or an enclosing container can supply are written only when the author
            // set them, and then even if equal to the default: an unset value would be overridden.
            bool setByAuthor = false;
            if (element is TextElementBase t)
            {
                bool? explicitly = key switch
                {
                    "fontSize" => t.FontSizeIsExplicit,
                    "color" => t.TextColorIsExplicit,
                    "lineHeight" => t.LineHeightIsExplicit,
                    "decoration" => t.DecorationIsExplicit,
                    _ => null,
                };
                if (explicitly is false) continue;
                setByAuthor |= explicitly is true;
            }
            if (key == "alignment" && element is TextBlock or RichText)
            {
                bool explicitAlignment = element is TextBlock block ? block.AlignmentIsExplicit : ((RichText)element).AlignmentIsExplicit;
                if (!explicitAlignment) continue;
                setByAuthor = true;
            }
            if (key == "bulletColor" && element is UnorderedList list)
            {
                // Unset, the bullets follow the text colour; writing that colour would fix them to it.
                if (!list.BulletColorIsExplicit) continue;
                setByAuthor = true;
            }

            PropertyInfo property = DefinitionReader.FindProperty(clr, key)
                ?? throw new InvalidOperationException($"{clr.Name} has no property for the definition key `{key}`.");
            JsonNode? wire = Wire(keySpec.Kind, property.GetValue(element));
            if (wire is null) continue;
            if (!keySpec.Required && !setByAuthor && Same(wire, Wire(keySpec.Kind, property.GetValue(blank)))) continue;
            if (wire is JsonObject { Count: 0 } && head == "object") continue;
            node[key] = wire;
        }
        return node;
    }

    private static object BlankOf(string type)
    {
        lock (BlankCache)
        {
            if (!BlankCache.TryGetValue(type, out object? blank))
            {
                blank = Blanks[type]();
                BlankCache[type] = blank;
            }
            return blank;
        }
    }

    /// <summary><c>fontName</c>/<c>fontFamily</c>/<c>fontWeight</c>/<c>italic</c> of a text element: only what its author set.</summary>
    private JsonObject FontValues(TextElementBase element)
    {
        var values = new JsonObject();
        if (!element.FontIsExplicit) return values;
        bool weighted = element.FontWeight != FontWeight.Normal || element.Italic;
        if (element.FontIsChosenByFamily)
        {
            values["fontFamily"] = element.FontFamily;
        }
        else if (element.FontIsNamed
            && (!weighted || !string.Equals(FontFamilyResolver.Resolve(StandardFonts.Helvetica, element.FontWeight, element.Italic), element.FontName, StringComparison.Ordinal)))
        {
            // A font that was named, rather than one the weight and italic resolved from the default.
            values["fontName"] = element.FontName;
        }
        // Weight and italic that DefaultTextStyle supplied are not the author's, so they are not written.
        // An explicit Normal weight or false italic is written too: it is a choice that stops the cascade.
        if (element.FontWeightIsExplicit) values["fontWeight"] = Wire("fontWeight", (int)element.FontWeight);
        if (element.ItalicIsExplicit) values["italic"] = element.Italic;
        return values;
    }

    // ── slots ────────────────────────────────────────────────────────────────

    private JsonObject? Child(IElement? child, string at) => child is null ? null : WriteElement(child, at);

    private Slot WriteSlot(string type, string key, IElement element, string at, string? id)
    {
        string parent = at[..at.LastIndexOf('/')];
        switch ($"{type}.{key}")
        {
            case "Flip.child":
                return Slot.Write(Child(((Flip)element).Child, at));
            case "Translate.child":
                return Slot.Write(Child(((Translate)element).Child, at));
            case "Rotate.child":
                return Slot.Write(Child(((Rotate)element).Child, at));
            case "Container.child":
                return Slot.Write(Child(((Container)element).Child, at));
            case "EnsureSpace.child" or "PreventPageBreak.child" or "ShowEntire.child" or "StopPaging.child"
                or "ShowOnce.child" or "SkipOnce.child" or "Unconstrained.child" or "ShowIf.child":
            {
                var wrapper = (PaginationWrapperBase)element;
                if (element is ShowIf { Predicate: not null })
                {
                    Cannot(parent, id, "A ShowIf with a predicate is a function of the page and cannot be written.");
                    return Slot.Drop;
                }
                JsonObject? child = Child(wrapper.Child, at);
                return child is null ? Slot.Drop : Slot.Write(child);
            }
            case "VerticalStack.children" or "SemanticGroup.children":
                return Slot.Write(Elements(((VerticalStack)element).Children, at));
            case "SemanticGroup.role":
                return Slot.Write(Wire("enum:SemanticRole", ((SemanticGroup)element).SemanticRole));
            case "HorizontalStack.children":
            {
                var stack = (HorizontalStack)element;
                var children = new JsonArray();
                for (int i = 0; i < stack.Children.Count; i++)
                {
                    JsonObject? node = WriteElement(stack.Children[i], $"{at}/{i}");
                    if (node is null) continue;
                    float? weight = stack.Weights[i];
                    children.Add(weight is null ? node : new JsonObject { ["element"] = node, ["weight"] = weight.Value });
                }
                return Slot.Write(children);
            }
            case "Layers.primary":
            {
                JsonObject? primary = Child(((Layers)element).PrimaryLayer, at);
                return primary is null ? Slot.Drop : Slot.Write(primary);
            }
            case "Layers.backgrounds":
            {
                JsonArray list = Elements(((Layers)element).BackgroundLayers, at);
                return Slot.Write(list.Count > 0 ? list : null);
            }
            case "Layers.foregrounds":
            {
                JsonArray list = Elements(((Layers)element).ForegroundLayers, at);
                return Slot.Write(list.Count > 0 ? list : null);
            }
            case "RichText.spans":
                return Slot.Write(Spans((RichText)element));
            case "RichText.defaultStyle":
                return Slot.Write(DefaultStyle((RichText)element));
            case "UnorderedList.items" or "OrderedList.items":
            {
                var list = (ListElementBase)element;
                var items = new JsonArray();
                foreach (string item in list is UnorderedList u ? u.Items : ((OrderedList)list).Items) items.Add(item);
                foreach (JsonNode? nested in Elements(list.GetNestedLists(), at)) items.Add(nested!.DeepClone());
                return Slot.Write(items);
            }
            case "Table.columns" or "Grid.columns":
            {
                IReadOnlyList<float> columns = ((Table)element).ColumnWeights;
                if (columns.Count == 0) return Slot.Write(null);
                var array = new JsonArray();
                foreach (float column in columns) array.Add(column);
                return Slot.Write(array);
            }
            case "Table.header" or "Grid.header" or "Table.rows" or "Grid.rows" or "Table.footer" or "Grid.footer":
            {
                var table = (Table)element;
                IReadOnlyList<TableRow> rows = key switch
                {
                    "header" => table.HeaderRows,
                    "footer" => table.FooterRows,
                    _ => [.. table.Rows],
                };
                if (rows.Count == 0 && key != "rows") return Slot.Write(null);
                var array = new JsonArray();
                foreach (TableRow row in rows) array.Add(Row(row, $"{at}/{array.Count}", key == "header"));
                return Slot.Write(array);
            }
            case "Image.resource":
                return ImageResource((Image)element, parent, id);
            case "Svg.resource" or "Svg.svgData":
                return SvgSource((Svg)element, key, parent, id);
            case "BarChart.data" or "GroupedBarChart.data" or "LineChart.data" or "AreaChart.data"
                or "DotPlotChart.data" or "LollipopChart.data" or "PieChart.data":
                return Slot.Write(ChartData((ChartBase)element));
            default:
                return Slot.Ordinary;
        }
    }

    private JsonObject ChartData(ChartBase chart)
    {
        var series = new JsonArray();
        ShapeSpec seriesSpec = D.Shapes["Series"];
        foreach (ChartSeries s in chart.Data.Series)
        {
            var blankSeries = new ChartSeries();
            var node = new JsonObject();
            foreach ((string key, KeySpec keySpec) in seriesSpec.Keys)
            {
                switch (key)
                {
                    case "dataPoints":
                    {
                        var points = new JsonArray();
                        foreach (ChartDataPoint point in s.DataPoints)
                            points.Add(Shape(point, D.Shapes["DataPoint"], new ChartDataPoint(), []));
                        node[key] = points;
                        break;
                    }
                    case "style":
                    {
                        JsonObject style = Shape(s.Style, D.Shapes["SeriesStyle"], new ChartSeriesStyle(), []);
                        if (style.Count > 0) node[key] = style;
                        break;
                    }
                    default:
                    {
                        PropertyInfo property = DefinitionReader.FindProperty(typeof(ChartSeries), key)!;
                        JsonNode? wire = Wire(keySpec.Kind, property.GetValue(s));
                        if (wire is not null && !Same(wire, Wire(keySpec.Kind, property.GetValue(blankSeries)))) node[key] = wire;
                        break;
                    }
                }
            }
            series.Add(node);
        }
        return new JsonObject { ["series"] = series };
    }

    private JsonArray Spans(RichText rich)
    {
        var spans = new JsonArray();
        foreach (TextSpan span in rich.SpanList)
        {
            if (span.IsLineBreak && string.IsNullOrEmpty(span.Text) && span.Style is null)
            {
                spans.Add(new JsonObject { ["lineBreak"] = true });
                continue;
            }
            var node = new JsonObject { ["text"] = span.Text };
            if (span.Style is not null)
            {
                JsonObject style = Shape(span.Style, D.Shapes["TextStyle"], new TextStyle(), []);
                if (style.Count > 0) node["style"] = style;
            }
            if (span.Link is not null) node["link"] = span.Link;
            spans.Add(node);
        }
        return spans;
    }

    private JsonNode? DefaultStyle(RichText rich)
    {
        // The default of this core is a Helvetica 12 black style; the wire default is "nothing set". Equal to the
        // default writes nothing, an empty style that is not writes `{}`, anything else writes what it sets.
        var blank = new RichText();
        JsonObject style = Shape(rich.DefaultStyle, D.Shapes["TextStyle"], new TextStyle(), []);
        JsonObject defaults = Shape(blank.DefaultStyle, D.Shapes["TextStyle"], new TextStyle(), []);
        return Same(style, defaults) ? null : style;
    }

    // ── tables ───────────────────────────────────────────────────────────────

    private JsonNode Row(TableRow row, string at, bool inHeader)
    {
        var cells = new JsonArray();
        foreach (TableCell cell in row.Cells) cells.Add(Cell(cell, $"{at}/cells/{cells.Count}", inHeader));
        JsonObject options = Shape(row, D.Shapes["TableRow"], new TableRow { Cells = [] }, ["cells"]);
        if (options.Count == 0) return cells;
        var result = new JsonObject { ["cells"] = cells };
        foreach (KeyValuePair<string, JsonNode?> pair in options.ToList())
        {
            options.Remove(pair.Key);
            result[pair.Key] = pair.Value;
        }
        return result;
    }

    private JsonNode Cell(TableCell cell, string at, bool inHeader)
    {
        JsonObject? content = WriteElement(cell.Content, $"{at}/content");
        bool plain = content is not null && content.Count == 2 && content["type"]?.GetValue<string>() == "TextBlock";
        HashSet<string> skip = inHeader ? ["content", "type", "isHeader"] : ["content", "type"];
        JsonObject options = Shape(cell, D.Shapes["TableCell"], new TableCell { Content = new TextBlock { Text = string.Empty } }, skip);
        JsonNode contentNode = content is null ? string.Empty : plain ? content["text"]!.GetValue<string>() : content;
        if (options.Count == 0) return contentNode;
        var result = new JsonObject { ["type"] = "TableCell", ["content"] = contentNode };
        foreach (KeyValuePair<string, JsonNode?> pair in options.ToList())
        {
            options.Remove(pair.Key);
            result[pair.Key] = pair.Value;
        }
        return result;
    }

    // ── resources ────────────────────────────────────────────────────────────

    private static string MediaTypeOf(string kind, byte[] bytes)
    {
        static bool Starts(byte[] d, params byte[] sig) => d.Length >= sig.Length && sig.Select((b, i) => d[i] == b).All(x => x);
        if (kind == "image")
        {
            if (Starts(bytes, 0x89, 0x50, 0x4e, 0x47)) return "image/png";
            if (Starts(bytes, 0xff, 0xd8, 0xff)) return "image/jpeg";
        }
        if (kind == "font") return Starts(bytes, 0x4f, 0x54, 0x54, 0x4f) ? "font/otf" : "font/ttf";
        if (kind == "svg") return "image/svg+xml";
        return "application/octet-stream";
    }

    /// <summary>Adds a resource (or finds the identical one already added) and returns its name.</summary>
    private string Resource(string kind, byte[] bytes, JsonObject? extra = null)
    {
        foreach (ResourceEntry found in _resources)
        {
            if (string.Equals(found.Kind, kind, StringComparison.Ordinal) && found.Bytes.AsSpan().SequenceEqual(bytes)
                && Same(found.Body["family"], extra?["family"]) && Same(found.Body["weight"], extra?["weight"]) && Same(found.Body["italic"], extra?["italic"]))
                return found.Name;
        }
        string mediaType = MediaTypeOf(kind, bytes);
        var body = new JsonObject { ["kind"] = kind, ["mediaType"] = mediaType };
        string? reference = _options.EmbedResources ? null : _options.ResourceRef?.Invoke(new DefinitionResourceDescriptor(kind, mediaType, bytes));
        if (reference is not null)
        {
            body["ref"] = reference;
            body["sha256"] = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
        else
        {
            body["data"] = Convert.ToBase64String(bytes);
        }
        if (extra is not null)
        {
            foreach (KeyValuePair<string, JsonNode?> pair in extra) body[pair.Key] = pair.Value?.DeepClone();
        }
        _counters[kind] = _counters.GetValueOrDefault(kind) + 1;
        string name = $"{kind}-{_counters[kind]}";
        _resources.Add(new ResourceEntry(name, kind, bytes, body));
        return name;
    }

    private static byte[]? ReadFile(string path)
    {
        try
        {
            return System.IO.File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is System.IO.IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private Slot ImageResource(Image image, string parent, string? id)
    {
        byte[]? bytes = image.ImageData.Length > 0 ? image.ImageData : image.FilePath.Length > 0 ? ReadFile(image.FilePath) : null;
        if (bytes is null)
        {
            Cannot(parent, id, "The image has no bytes a definition can carry (its file could not be read).");
            return Slot.Drop;
        }
        return Slot.Write(Resource("image", bytes));
    }

    private Slot SvgSource(Svg svg, string key, string parent, string? id)
    {
        if (svg.SvgGenerator is not null)
        {
            Cannot(parent, id, "An Svg with an svgGenerator is a function of its box and cannot be written.");
            return Slot.Drop;
        }
        string markup = svg.SvgData;
        if (markup.Length == 0 && svg.FilePath.Length > 0)
        {
            byte[]? file = ReadFile(svg.FilePath);
            markup = file is null ? string.Empty : Encoding.UTF8.GetString(file);
        }
        if (markup.Length == 0)
        {
            Cannot(parent, id, "The Svg has no markup a definition can carry (its file could not be read).");
            return Slot.Drop;
        }
        byte[] bytes = Encoding.UTF8.GetBytes(markup);
        string? named = _options.EmbedResources ? null : _options.ResourceRef?.Invoke(new DefinitionResourceDescriptor("svg", "image/svg+xml", bytes));
        if (named is null) return Slot.Write(key == "svgData" ? markup : null);
        return Slot.Write(key == "resource" ? Resource("svg", bytes) : null);
    }

    // ── the whole document ───────────────────────────────────────────────────

    private JsonObject Definition()
    {
        var definition = new JsonObject
        {
            ["version"] = D.Definition.Version,
            ["minorVersion"] = 0,
        };

        JsonObject settings = Settings();
        if (settings.Count > 0) definition["settings"] = settings;
        JsonObject metadata = Metadata();
        if (metadata.Count > 0) definition["metadata"] = metadata;

        // Faces are resources; register them before the pages so they come first.
        foreach (DocumentFontFace face in _doc.Fonts.Faces)
        {
            Resource("font", face.Bytes, new JsonObject
            {
                ["family"] = face.Family,
                ["weight"] = Wire("fontWeight", face.Weight),
                ["italic"] = face.Italic,
            });
        }

        var pages = new JsonArray();
        DocumentSettings baseline = _doc.Settings;
        for (int i = 0; i < _doc.Pages.Count; i++)
        {
            Page page = _doc.Pages[i];
            string at = $"/pages/{i}";
            var node = new JsonObject();
            JsonNode size = PageSizeWire(page.Size);
            if (baseline.PreservePageDimensions)
            {
                if (page.Width != baseline.PageSize.Width || page.Height != baseline.PageSize.Height)
                    node["size"] = new JsonObject { ["width"] = page.Width, ["height"] = page.Height };
            }
            else
            {
                if (!Same(size, PageSizeWire(baseline.PageSize))) node["size"] = size;
                bool landscape = page.Width > page.Height;
                if (landscape != (baseline.Orientation == PageOrientation.Landscape)) node["orientation"] = landscape ? "landscape" : "portrait";
            }
            if (page.HasExplicitMargin) node["margin"] = page.Margin;
            if (page.Margins is Thickness margins) node["margins"] = Wire("spacing", margins);
            if (page.PageColor is { } pageColor) node["pageColor"] = Colour(pageColor);
            if (page.PageBackground is not null && WriteElement(page.PageBackground, $"{at}/background") is { } background) node["background"] = background;
            if (page.PageForeground is not null && WriteElement(page.PageForeground, $"{at}/foreground") is { } foreground) node["foreground"] = foreground;
            node["content"] = Elements(page.Elements, $"{at}/content");
            pages.Add(node);
        }

        var attachments = new JsonArray();
        foreach (FileAttachment attachment in _doc.Attachments)
        {
            string resource = Resource("attachment", attachment.Content);
            var node = new JsonObject { ["resource"] = resource, ["fileName"] = attachment.FileName };
            if (attachment.Relationship != AttachmentRelationship.Unspecified) node["relationship"] = Wire("enum:AttachmentRelationship", attachment.Relationship);
            if (attachment.MediaType is not null) node["mediaType"] = attachment.MediaType;
            if (attachment.Description is not null) node["description"] = attachment.Description;
            if (attachment.CreationDate is not null) node["creationDate"] = Wire("dateTime", attachment.CreationDate);
            if (attachment.ModificationDate is not null) node["modificationDate"] = Wire("dateTime", attachment.ModificationDate);
            attachments.Add(node);
        }

        // Root keys in the order of the description, so the JSON text is the same every time.
        var result = new JsonObject();
        foreach (string key in D.Root.Keys.Keys)
        {
            switch (key)
            {
                case "version" or "minorVersion" or "settings" or "metadata":
                    if (definition.TryGetPropertyValue(key, out JsonNode? value)) result[key] = value?.DeepClone();
                    break;
                case "resources" when _resources.Count > 0:
                {
                    var resources = new JsonObject();
                    foreach (ResourceEntry entry in _resources) resources[entry.Name] = entry.Body.DeepClone();
                    result[key] = resources;
                    break;
                }
                case "attachments" when attachments.Count > 0:
                    result[key] = attachments;
                    break;
                case "pages":
                    result[key] = pages;
                    break;
                default:
                    break;
            }
        }
        return result;
    }

    private JsonObject Metadata()
    {
        DocumentMetadata metadata = _doc.Metadata;
        var blank = new DocumentMetadata();
        var result = new JsonObject();
        foreach ((string key, KeySpec keySpec) in D.Objects["Metadata"].Keys)
        {
            // A date the metadata gave itself when it was built is "the time it was made", not a chosen date.
            if (key == "creationDate" && metadata.CreationDateIsBuiltIn) continue;
            if (key == "modifiedDate" && metadata.ModifiedDateIsBuiltIn) continue;
            PropertyInfo property = DefinitionReader.FindProperty(typeof(DocumentMetadata), key)!;
            object? value = property.GetValue(metadata);
            if (key == "customProperties" && value is IDictionary { Count: 0 }) value = null;
            JsonNode? wire = Wire(keySpec.Kind, value);
            if (wire is null) continue;
            if (keySpec.Kind != "dateTime" && Same(wire, Wire(keySpec.Kind, property.GetValue(blank)))) continue;
            result[key] = wire;
        }
        return result;
    }

    private JsonObject Settings()
    {
        DocumentSettings settings = _doc.Settings;
        var blank = new DocumentSettings();
        var result = new JsonObject();
        foreach ((string key, KeySpec keySpec) in D.Objects["Settings"].Keys)
        {
            string at = $"/settings/{key}";
            switch (key)
            {
                case "pageSize":
                    if (settings.PreservePageDimensions && settings.PageSize.Width > settings.PageSize.Height)
                    {
                        result[key] = new JsonObject { ["width"] = settings.PageSize.Width, ["height"] = settings.PageSize.Height };
                        continue;
                    }
                    break;
                case "pageChrome":
                    if (PageChrome() is { } chrome) result[key] = chrome;
                    continue;
                case "bookmarks":
                    if (settings.Bookmarks.Count > 0) result[key] = Bookmarks(settings.Bookmarks);
                    continue;
                case "namedDestinations":
                    if (settings.NamedDestinations.Count > 0) result[key] = Wire("map:integer", settings.NamedDestinations);
                    continue;
                case "facturX":
                    if (settings.FacturX is not null) result[key] = Shape(settings.FacturX, D.Shapes["FacturX"], null, []);
                    continue;
                case "encryption":
                    if (settings.Encryption is null) continue;
                    result[key] = Shape(settings.Encryption, D.Shapes["Encryption"], new EncryptionSettings(), []);
                    _notes.Add(new DefinitionIssue(at, "secret-omitted", "The passwords are not stored in a definition; the host supplies them when it is read."));
                    continue;
                default:
                    break;
            }
            PropertyInfo property = DefinitionReader.FindProperty(typeof(DocumentSettings), key)!;
            object? value = property.GetValue(settings);
            JsonNode? wire = Wire(keySpec.Kind, value);
            // "No cap" is null here and the token "unlimited" on the wire.
            if (wire is null && keySpec.Kind == "integerOrUnlimited" && blank.MaxImageDpi is not null) wire = "unlimited";
            if (wire is null) continue;
            if (Same(wire, Wire(keySpec.Kind, property.GetValue(blank)))) continue;
            if (wire is JsonObject { Count: 0 } && keySpec.Kind.StartsWith("object:", StringComparison.Ordinal)) continue;
            result[key] = wire;
        }
        return result;
    }

    private static JsonArray Bookmarks(IEnumerable<BookmarkItem> items)
    {
        var array = new JsonArray();
        foreach (BookmarkItem item in items)
        {
            var node = new JsonObject { ["title"] = item.Title, ["pageIndex"] = item.PageIndex };
            if (item.Children.Count > 0) node["children"] = Bookmarks(item.Children);
            array.Add(node);
        }
        return array;
    }

    private JsonObject? PageChrome()
    {
        DocumentSettings settings = _doc.Settings;
        (string Slot, string Property, Func<int, IElement?>? Template)[] slots =
        [
            ("header", "headerTemplate", settings.HeaderTemplate),
            ("footer", "footerTemplate", settings.FooterTemplate),
            ("background", "pageBackground", settings.PageBackground),
            ("foreground", "pageForeground", settings.PageForeground),
        ];
        var chrome = new JsonObject();
        foreach ((string name, string property, Func<int, IElement?>? template) in slots)
        {
            if (template is null) continue;
            string at = $"/settings/pageChrome/{name}";
            if (!PageTemplate.TryGetElements(template, out PageTemplateElements elements))
            {
                // The wire has no key for a function; the report names the property of the document that holds it.
                Cannot($"/settings/{property}", null, $"{property} is a function of the page number; build it with PageTemplate.FromElements to write it.");
                continue;
            }
            var slot = new JsonObject();
            (string Member, IElement? Element)[] members =
            [
                ("default", elements.Default), ("firstPage", elements.FirstPage), ("odd", elements.Odd), ("even", elements.Even),
            ];
            foreach ((string member, IElement? element) in members)
            {
                if (element is null) continue;
                JsonObject? node = WriteElement(element, $"{at}/{member}");
                if (node is not null) slot[member] = node;
            }
            if (slot.Count > 0) chrome[name] = slot;
        }
        return chrome.Count > 0 ? chrome : null;
    }
}
