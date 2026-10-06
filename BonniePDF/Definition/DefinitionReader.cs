using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Attachments;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Layout.Pagination;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Security.Models;
using BonniePDF.Security.Permissions;
using BonniePDF.Typography;

namespace BonniePDF.Definition;

/// <summary>
/// Builds a <see cref="Document"/> from a validated definition. Values are converted as the element
/// description says, and applied to the same public properties the code API sets.
/// </summary>
internal sealed class DefinitionReader
{
    private static readonly DescriptionData D = ElementDescription.Data;

    private static readonly HashSet<string> SlotKinds = new(StringComparer.Ordinal)
    {
        "element", "elements", "stackChildren", "listItems", "spans", "rows", "cells", "cellContent",
    };

    // Where the wire key is not the property's name in PascalCase.
    private static readonly Dictionary<string, string[]> Aliases = new(StringComparer.Ordinal)
    {
        ["pdfVersion"] = ["Version"],
    };

    private readonly Dictionary<string, byte[]> _resources;

    private DefinitionReader(Dictionary<string, byte[]> resources)
    {
        _resources = resources;
    }

    // ── entry ────────────────────────────────────────────────────────────────

    internal static async Task<Document> ReadAsync(JsonElement definition, DefinitionReadOptions options, CancellationToken cancellationToken)
    {
        if (definition.TryGetProperty("settings", out JsonElement settingsJson) && settingsJson.TryGetProperty("encryption", out _)
            && string.IsNullOrEmpty(options.Passwords?.Owner))
        {
            throw new DefinitionException([
                new DefinitionIssue("/settings/encryption", "secret-required", "The definition is encrypted; the host must supply `Passwords.Owner` when reading."),
            ]);
        }

        Dictionary<string, byte[]> resources = await LoadResourcesAsync(definition, options, cancellationToken).ConfigureAwait(false);
        var reader = new DefinitionReader(resources);
        return reader.Build(definition, options);
    }

    private static async Task<Dictionary<string, byte[]>> LoadResourcesAsync(JsonElement definition, DefinitionReadOptions options, CancellationToken cancellationToken)
    {
        var issues = new List<DefinitionIssue>();
        var bytes = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (definition.TryGetProperty("resources", out JsonElement resources))
        {
            foreach (JsonProperty entry in resources.EnumerateObject())
            {
                string at = $"/resources/{entry.Name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal)}";
                JsonElement resource = entry.Value;
                string mediaType = resource.GetProperty("mediaType").GetString()!;
                byte[]? data = null;
                if (resource.TryGetProperty("data", out JsonElement encoded)) data = Convert.FromBase64String(encoded.GetString()!);
                else if (resource.TryGetProperty("ref", out JsonElement reference))
                {
                    if (options.Resources is not null)
                    {
                        data = await options.Resources(reference.GetString()!, new DefinitionResourceRequest(entry.Name, resource.GetProperty("kind").GetString()!, mediaType), cancellationToken).ConfigureAwait(false);
                    }
                    if (data is null)
                    {
                        issues.Add(new DefinitionIssue($"{at}/ref", "resource-unresolved", $"The host supplied no bytes for {JsonSerializer.Serialize(reference.GetString())}."));
                        continue;
                    }
                }
                if (data is null) continue;
                if (!MatchesMediaType(mediaType, data))
                {
                    issues.Add(new DefinitionIssue(at, "resource-type-mismatch", $"The bytes are not {mediaType}."));
                    continue;
                }
                if (resource.TryGetProperty("sha256", out JsonElement hash)
                    && !string.Equals(Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), hash.GetString(), StringComparison.Ordinal))
                {
                    issues.Add(new DefinitionIssue($"{at}/sha256", "resource-hash-mismatch", "The bytes do not match the recorded sha256."));
                    continue;
                }
                bytes[entry.Name] = data;
            }
        }
        if (issues.Count > 0) throw new DefinitionException(issues);
        return bytes;
    }

    private static bool MatchesMediaType(string mediaType, byte[] data)
    {
        static bool Starts(byte[] d, params byte[] sig)
        {
            if (d.Length < sig.Length) return false;
            for (int i = 0; i < sig.Length; i++)
                if (d[i] != sig[i]) return false;
            return true;
        }
        switch (mediaType.ToLowerInvariant())
        {
            case "image/png":
                return Starts(data, 0x89, 0x50, 0x4e, 0x47);
            case "image/jpeg":
                return Starts(data, 0xff, 0xd8, 0xff);
            case "font/ttf" or "font/otf" or "font/sfnt" or "application/font-sfnt" or "application/x-font-ttf":
                return Starts(data, 0x00, 0x01, 0x00, 0x00) || Starts(data, 0x74, 0x72, 0x75, 0x65) || Starts(data, 0x4f, 0x54, 0x54, 0x4f) || Starts(data, 0x74, 0x74, 0x63, 0x66);
            case "image/svg+xml":
                return Encoding.UTF8.GetString(data, 0, Math.Min(data.Length, 4096)).Contains("<svg", StringComparison.Ordinal);
            default:
                return true;
        }
    }

    // ── document ─────────────────────────────────────────────────────────────

    private Document Build(JsonElement definition, DefinitionReadOptions options)
    {
        JsonElement pagesJson = definition.GetProperty("pages");
        JsonElement first = pagesJson[0];

        var settings = new DocumentSettings();
        definition.TryGetProperty("settings", out JsonElement settingsJson);
        var settingsSkip = new HashSet<string>(StringComparer.Ordinal) { "pageChrome", "encryption" };
        if (settingsJson.ValueKind == JsonValueKind.Object)
        {
            Populate(settings, settingsJson, D.Objects["Settings"], settingsSkip);
            if (settingsJson.TryGetProperty("encryption", out JsonElement encryption))
            {
                var settingsEncryption = new EncryptionSettings();
                Populate(settingsEncryption, encryption, ElementDescription.Shape("Encryption"), []);
                settingsEncryption.OwnerPassword = options.Passwords!.Owner!;
                settingsEncryption.UserPassword = options.Passwords.User ?? "";
                settings.Encryption = settingsEncryption;
            }
        }

        // The first page is the one the document creates itself, so its size comes from the settings the
        // document is constructed with; the real defaults are put back before the other pages are added.
        PdfSize documentSize = settings.PageSize;
        PageOrientation documentOrientation = settings.Orientation;
        float documentMargin = settings.Margin;
        if (first.TryGetProperty("size", out JsonElement firstSize)) settings.PageSize = ToPageSize(firstSize);
        if (first.TryGetProperty("orientation", out JsonElement firstOrientation)) settings.Orientation = EnumFromToken<PageOrientation>(firstOrientation.GetString()!);
        if (first.TryGetProperty("margin", out JsonElement firstMargin)) settings.Margin = ToPoints(firstMargin);

        var metadata = new DocumentMetadata();
        if (definition.TryGetProperty("metadata", out JsonElement metadataJson)) Populate(metadata, metadataJson, D.Objects["Metadata"], []);

        var doc = new Document(settings, metadata);
        settings.PageSize = documentSize;
        settings.Orientation = documentOrientation;
        settings.Margin = documentMargin;

        // The page the document made for itself is given a margin of its own only if the definition states one.
        doc.Pages[0].HasExplicitMargin = first.TryGetProperty("margin", out _);

        if (settingsJson.ValueKind == JsonValueKind.Object && settingsJson.TryGetProperty("pageChrome", out JsonElement chrome))
        {
            settings.HeaderTemplate = ChromeTemplate(chrome, "header");
            settings.FooterTemplate = ChromeTemplate(chrome, "footer");
            settings.PageBackground = ChromeTemplate(chrome, "background");
            settings.PageForeground = ChromeTemplate(chrome, "foreground");
        }
        if (options.Rasterizer is not null) settings.SvgRasterizer = options.Rasterizer;

        // Fonts belong to this document (spec 16).
        if (definition.TryGetProperty("resources", out JsonElement resources))
        {
            foreach (JsonProperty entry in resources.EnumerateObject())
            {
                JsonElement resource = entry.Value;
                if (resource.GetProperty("kind").GetString() != "font") continue;
                doc.Fonts.Add(
                    _resources[entry.Name],
                    resource.TryGetProperty("family", out JsonElement family) ? family.GetString() : null,
                    resource.TryGetProperty("weight", out JsonElement weight) ? ToFontWeight(weight) : null,
                    resource.TryGetProperty("italic", out JsonElement italic) ? italic.GetBoolean() : null);
            }
        }

        int index = 0;
        foreach (JsonElement entry in pagesJson.EnumerateArray())
        {
            Page page;
            if (index == 0) page = doc.Pages[0];
            else
            {
                PdfSize size = entry.TryGetProperty("size", out JsonElement s) ? ToPageSize(s) : documentSize;
                PageOrientation orientation = entry.TryGetProperty("orientation", out JsonElement o) ? EnumFromToken<PageOrientation>(o.GetString()!) : documentOrientation;
                // A page states a margin of its own only if the definition does; otherwise it follows the document's.
                page = entry.TryGetProperty("margin", out JsonElement m)
                    ? doc.AddPage(size, orientation, ToPoints(m))
                    : doc.AddPage(size, orientation);
            }
            if (entry.TryGetProperty("pageColor", out JsonElement color)) page.PageColor = ToColor(color.GetString()!);
            if (entry.TryGetProperty("background", out JsonElement background)) page.PageBackground = Element(background);
            if (entry.TryGetProperty("foreground", out JsonElement foreground)) page.PageForeground = Element(foreground);
            foreach (JsonElement node in entry.GetProperty("content").EnumerateArray()) page.Add(Element(node));
            index++;
        }

        if (definition.TryGetProperty("attachments", out JsonElement attachments))
        {
            foreach (JsonElement a in attachments.EnumerateArray())
            {
                var attachment = new FileAttachment
                {
                    FileName = a.GetProperty("fileName").GetString()!,
                    Content = _resources[a.GetProperty("resource").GetString()!],
                    MediaType = a.TryGetProperty("mediaType", out JsonElement mt) ? mt.GetString() : null,
                    Description = a.TryGetProperty("description", out JsonElement d) ? d.GetString() : null,
                };
                if (a.TryGetProperty("relationship", out JsonElement rel)) attachment.Relationship = EnumFromToken<AttachmentRelationship>(rel.GetString()!);
                if (a.TryGetProperty("creationDate", out JsonElement created)) attachment.CreationDate = ParseDate(created.GetString()!);
                if (a.TryGetProperty("modificationDate", out JsonElement modified)) attachment.ModificationDate = ParseDate(modified.GetString()!);
                doc.Attach(attachment);
            }
        }
        return doc;
    }

    private Func<int, IElement?>? ChromeTemplate(JsonElement chrome, string slotName)
    {
        if (!chrome.TryGetProperty(slotName, out JsonElement slot)) return null;
        IElement? Pick(string key) => slot.TryGetProperty(key, out JsonElement node) ? Element(node) : null;
        return PageTemplate.FromElements(@default: Pick("default"), firstPage: Pick("firstPage"), odd: Pick("odd"), even: Pick("even"));
    }

    // ── elements ─────────────────────────────────────────────────────────────

    /// <summary>Builds an element, and marks it with its `id` (spec 07 source identifiers).</summary>
    private IElement Element(JsonElement node)
    {
        IElement element = Build(node);
        if (node.TryGetProperty("id", out JsonElement id)) DebugSource.Mark(element, id.GetString()!);
        return element;
    }

    private IEnumerable<IElement> Elements(JsonElement nodes) => nodes.EnumerateArray().Select(Element).ToList();

    private static HashSet<string> Skip(params string[] keys) => new(keys, StringComparer.Ordinal);

    private IElement Build(JsonElement node)
    {
        string type = node.GetProperty("type").GetString()!;
        ShapeSpec spec = D.Elements[type];
        IElement? Child() => node.TryGetProperty("child", out JsonElement c) ? Element(c) : null;

        switch (type)
        {
            case "Flip":
            {
                var e = new Flip();
                Populate(e, node, spec, []);
                e.Child = Child();
                return e;
            }
            case "Translate":
            {
                var e = new Translate();
                Populate(e, node, spec, []);
                e.Child = Child();
                return e;
            }
            case "Rotate":
            {
                var e = new Rotate();
                Populate(e, node, spec, []);
                e.Child = Child();
                return e;
            }
            case "Container":
            {
                var e = new Container();
                Populate(e, node, spec, []);
                e.Child = Child();
                return e;
            }
            case "VerticalStack":
            {
                var e = new VerticalStack();
                Populate(e, node, spec, []);
                foreach (IElement child in Elements(node.GetProperty("children"))) e.Add(child);
                return e;
            }
            case "HorizontalStack":
            {
                var e = new HorizontalStack();
                Populate(e, node, spec, []);
                foreach (JsonElement c in node.GetProperty("children").EnumerateArray())
                {
                    if (!c.TryGetProperty("type", out _) && c.TryGetProperty("element", out JsonElement inner)) e.Add(Element(inner), (float)c.GetProperty("weight").GetDouble());
                    else e.Add(Element(c));
                }
                return e;
            }
            case "Layers":
            {
                var e = new Layers();
                Populate(e, node, spec, []);
                e.PrimaryLayer = Element(node.GetProperty("primary"));
                if (node.TryGetProperty("backgrounds", out JsonElement backgrounds)) foreach (IElement b in Elements(backgrounds)) e.AddBackground(b);
                if (node.TryGetProperty("foregrounds", out JsonElement foregrounds)) foreach (IElement f in Elements(foregrounds)) e.AddForeground(f);
                return e;
            }
            case "Spacer":
                return Populated(new Spacer(), node, spec);
            case "PageBreak":
                return Populated(new PageBreak(), node, spec);
            case "SemanticGroup":
            {
                var e = new SemanticGroup(EnumFromToken<SemanticRole>(node.GetProperty("role").GetString()!));
                Populate(e, node, spec, Skip("role"));
                foreach (IElement child in Elements(node.GetProperty("children"))) e.Add(child);
                return e;
            }
            case "EnsureSpace":
                return WithChild(new EnsureSpace(), node, spec, Child());
            case "PreventPageBreak":
                return WithChild(new PreventPageBreak(), node, spec, Child());
            case "ShowEntire":
                return WithChild(new ShowEntire(), node, spec, Child());
            case "StopPaging":
                return WithChild(new StopPaging(), node, spec, Child());
            case "ShowOnce":
                return WithChild(new ShowOnce(), node, spec, Child());
            case "SkipOnce":
                return WithChild(new SkipOnce(), node, spec, Child());
            case "Unconstrained":
                return WithChild(new Unconstrained(), node, spec, Child());
            case "ShowIf":
                return WithChild(new ShowIf(), node, spec, Child());
            case "TextBlock":
                return Populated(new TextBlock { Text = string.Empty }, node, spec);
            case "Heading":
                return Populated(new Heading { Text = string.Empty, TocIdentifier = string.Empty }, node, spec);
            case "PageNumberOf":
            {
                var e = new PageNumberOf(node.GetProperty("anchorTarget").GetString()!);
                Populate(e, node, spec, Skip("anchorTarget"));
                return e;
            }
            case "VisualToc":
                return Populated(new VisualToc(), node, spec);
            case "Hyperlink":
            {
                var e = new Hyperlink(node.GetProperty("text").GetString()!);
                Populate(e, node, spec, Skip("text"));
                return e;
            }
            case "SectionLink":
            {
                var e = new SectionLink(node.GetProperty("text").GetString()!, node.GetProperty("anchorTarget").GetString()!);
                Populate(e, node, spec, Skip("text", "anchorTarget"));
                return e;
            }
            case "Section":
            {
                var e = new Section(node.GetProperty("anchorId").GetString()!);
                Populate(e, node, spec, Skip("anchorId"));
                return e;
            }
            case "RichText":
                return RichText(node, spec);
            case "UnorderedList":
            {
                var e = new UnorderedList();
                Populate(e, node, spec, Skip("items"));
                foreach (JsonElement item in node.GetProperty("items").EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String) e.AddItem(item.GetString()!);
                    else e.AddNestedList((UnorderedList)Element(item));
                }
                return e;
            }
            case "OrderedList":
            {
                var e = new OrderedList();
                Populate(e, node, spec, Skip("items"));
                foreach (JsonElement item in node.GetProperty("items").EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String) e.AddItem(item.GetString()!);
                    else e.AddNestedList((OrderedList)Element(item));
                }
                return e;
            }
            case "Table":
            case "Grid":
                return TableElement(node, spec, type);
            case "Image":
            {
                var e = new Image();
                Populate(e, node, spec, Skip("resource"));
                e.ImageData = _resources[node.GetProperty("resource").GetString()!];
                return e;
            }
            case "Svg":
            {
                var e = new Svg();
                Populate(e, node, spec, Skip("resource", "svgData"));
                e.SvgData = node.TryGetProperty("svgData", out JsonElement inline)
                    ? inline.GetString()!
                    : Encoding.UTF8.GetString(_resources[node.GetProperty("resource").GetString()!]);
                return e;
            }
            case "Line":
                return Populated(new Line(), node, spec);
            case "Circle":
                return Populated(new Circle(), node, spec);
            case "Ellipse":
                return Populated(new Ellipse(), node, spec);
            case "QRCode":
                return Populated(new QRCode(), node, spec);
            case "Barcode":
                return Populated(new Barcode(), node, spec);
            case "DataMatrix":
                return Populated(new DataMatrix(), node, spec);
            case "BarChart":
                return Populated(new Charts.BarChart(), node, spec);
            case "GroupedBarChart":
                return Populated(new Charts.GroupedBarChart(), node, spec);
            case "LineChart":
                return Populated(new Charts.LineChart(), node, spec);
            case "AreaChart":
                return Populated(new Charts.AreaChart(), node, spec);
            case "DotPlotChart":
                return Populated(new Charts.DotPlotChart(), node, spec);
            case "LollipopChart":
                return Populated(new Charts.LollipopChart(), node, spec);
            case "PieChart":
                return Populated(new Charts.PieChart(), node, spec);
            default:
                throw new DefinitionException([new DefinitionIssue("", "unknown-type", $"Element type {type} is not supported by this reader.")]);
        }
    }

    private T Populated<T>(T element, JsonElement node, ShapeSpec spec)
        where T : class
    {
        Populate(element, node, spec, []);
        return element;
    }

    private T WithChild<T>(T element, JsonElement node, ShapeSpec spec, IElement? child)
        where T : PaginationWrapperBase
    {
        Populate(element, node, spec, []);
        element.Child = child;
        return element;
    }

    private IElement RichText(JsonElement node, ShapeSpec spec)
    {
        var rich = new RichText();
        Populate(rich, node, spec, Skip("spans"));
        foreach (JsonElement span in node.GetProperty("spans").EnumerateArray())
        {
            if (span.TryGetProperty("lineBreak", out JsonElement lb) && lb.ValueKind == JsonValueKind.True) rich.LineBreak();
            else
            {
                TextStyle? style = null;
                if (span.TryGetProperty("style", out JsonElement styleJson))
                {
                    style = new TextStyle();
                    Populate(style, styleJson, D.Shapes["TextStyle"], []);
                }
                rich.Span(span.GetProperty("text").GetString()!, style!);
            }
        }
        return rich;
    }

    private IElement TableElement(JsonElement node, ShapeSpec spec, string type)
    {
        float[] columns = node.TryGetProperty("columns", out JsonElement c) ? c.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray() : [];
        Table table = type == "Grid" ? new Grid(columns) : new Table(columns);
        Populate(table, node, spec, Skip("columns", "header", "rows", "footer"));
        if (node.TryGetProperty("header", out JsonElement header)) foreach (JsonElement row in header.EnumerateArray()) table.AddHeader(Row(row));
        if (node.TryGetProperty("rows", out JsonElement rows)) foreach (JsonElement row in rows.EnumerateArray()) table.AddRow(Row(row));
        if (node.TryGetProperty("footer", out JsonElement footer)) foreach (JsonElement row in footer.EnumerateArray()) table.AddFooter(Row(row));
        return table;
    }

    private TableRow Row(JsonElement row)
    {
        var tableRow = new TableRow { Cells = [] };
        JsonElement cells = row;
        if (row.ValueKind == JsonValueKind.Object)
        {
            Populate(tableRow, row, D.Shapes["TableRow"], Skip("cells"));
            cells = row.GetProperty("cells");
        }
        tableRow.Cells = cells.EnumerateArray().Select(Cell).ToArray();
        return tableRow;
    }

    private TableCell Cell(JsonElement cell)
    {
        if (cell.ValueKind == JsonValueKind.String) return new TableCell { Content = new TextBlock { Text = cell.GetString()! } };
        if (cell.TryGetProperty("type", out JsonElement t) && t.GetString() == "TableCell")
        {
            JsonElement content = cell.GetProperty("content");
            var tableCell = new TableCell
            {
                Content = content.ValueKind == JsonValueKind.String ? new TextBlock { Text = content.GetString()! } : Element(content),
            };
            Populate(tableCell, cell, D.Shapes["TableCell"], Skip("content", "type"));
            return tableCell;
        }
        return new TableCell { Content = Element(cell) };
    }

    // ── populating objects ───────────────────────────────────────────────────

    /// <summary>Sets every value key of <paramref name="node"/> that the shape describes on <paramref name="target"/>.</summary>
    private void Populate(object target, JsonElement node, ShapeSpec spec, HashSet<string> skip)
    {
        Dictionary<string, KeySpec> keys = ElementDescription.KeysOf(spec);
        Type type = target.GetType();

        // In the description's order, not the document's: a chart's `data` replaces the whole data object,
        // so it has to be set before the axes and title that live on it.
        foreach ((string key, KeySpec keySpec) in keys)
        {
            if (key.StartsWith('_') || key is "type" or "id" || skip.Contains(key)) continue;
            if (!node.TryGetProperty(key, out JsonElement value)) continue;
            string head = keySpec.Kind.Split(':')[0];
            if (SlotKinds.Contains(head) || keySpec.Kind.Contains("element", StringComparison.Ordinal)) continue;
            PropertyInfo property = FindProperty(type, key)
                ?? throw new InvalidOperationException($"{type.Name} has no property for the definition key `{key}`.");

            ApplyValue(target, property, keySpec.Kind, value);
        }

        // A border written with a colour sets the border colours too, unless they were given.
        if (node.TryGetProperty("border", out JsonElement border) && border.ValueKind == JsonValueKind.Object
            && border.TryGetProperty("color", out JsonElement color) && !node.TryGetProperty("borderColors", out _)
            && type.GetProperty("BorderColors") is { CanWrite: true } borderColors)
        {
            borderColors.SetValue(target, new BorderColors(ToColor(color.GetString()!)));
        }
    }

    private static DateTimeOffset ParseDate(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    internal static PropertyInfo? FindProperty(Type type, string key)
    {
        string pascal = char.ToUpperInvariant(key[0]) + key[1..];
        IEnumerable<string> names = Aliases.TryGetValue(key, out string[]? aliases) ? aliases.Prepend(pascal) : [pascal];
        foreach (string name in names)
        {
            if (type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) is { } property) return property;
        }
        return null;
    }

    private void ApplyValue(object target, PropertyInfo property, string kind, JsonElement value)
    {
        Type propertyType = property.PropertyType;
        string head = kind.Split(':')[0];

        // Collections and maps: fill the property's own collection when it has no setter.
        if (head is "array" or "map" && propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() is { } generic
            && (generic == typeof(IList<>) || generic == typeof(IDictionary<,>)))
        {
            object collection = property.GetValue(target) ?? Activator.CreateInstance(
                generic == typeof(IList<>) ? typeof(List<>).MakeGenericType(propertyType.GetGenericArguments()) : typeof(Dictionary<,>).MakeGenericType(propertyType.GetGenericArguments()))!;
            string inner = kind[(kind.IndexOf(':', StringComparison.Ordinal) + 1)..];
            if (head == "array")
            {
                Type itemType = propertyType.GetGenericArguments()[0];
                foreach (JsonElement item in value.EnumerateArray()) ((IList)collection).Add(ConvertTo(itemType, inner, item));
            }
            else
            {
                Type itemType = propertyType.GetGenericArguments()[1];
                foreach (JsonProperty entry in value.EnumerateObject()) ((IDictionary)collection)[entry.Name] = ConvertTo(itemType, inner, entry.Value);
            }
            if (property.CanWrite) property.SetValue(target, collection);
            return;
        }

        property.SetValue(target, ConvertTo(propertyType, kind, value));
    }

    // ── value conversion ─────────────────────────────────────────────────────

    private object? ConvertTo(Type target, string kind, JsonElement value)
    {
        Type type = Nullable.GetUnderlyingType(target) ?? target;
        int colon = kind.IndexOf(':', StringComparison.Ordinal);
        string head = colon < 0 ? kind : kind[..colon];
        string arg = colon < 0 ? "" : kind[(colon + 1)..];
        switch (head)
        {
            case "string" or "chartNumberFormat":
                return value.GetString();
            case "boolean":
                return value.GetBoolean();
            case "integer":
                return ToNumericType(type, value.GetDouble());
            case "integerOrUnlimited":
                return value.ValueKind == JsonValueKind.String ? null : ToNumericType(type, value.GetDouble());
            case "number":
                return type == typeof(decimal) ? ToDecimal(value) : ToNumericType(type, value.GetDouble());
            case "length":
                return ToNumericType(type, ToPoints(value));
            case "spacing":
                return ToThickness(value);
            case "border":
                return ToBorder(value);
            case "color":
                return type == typeof(string) ? value.GetString() : ToColor(value.GetString()!);
            case "pageSize":
                return ToPageSize(value);
            case "fontWeight":
                return ToFontWeight(value);
            case "decoration":
                return ToFlags(typeof(TextDecoration), value);
            case "permissions":
                return ToFlags(typeof(PdfPermissions), value);
            case "dateTime":
                return DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            case "enum":
                return ToEnum(type, arg, value.GetString()!);
            case "array":
                return ConvertList(type, arg, value);
            case "map":
                return ConvertMap(type, arg, value);
            case "object":
                return BuildObject(type, arg, value);
            default:
                throw new InvalidOperationException($"No conversion for value kind {kind}.");
        }
    }

    private object ConvertList(Type type, string inner, JsonElement value)
    {
        Type itemType = type.IsGenericType ? type.GetGenericArguments()[0] : typeof(object);
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(itemType))!;
        foreach (JsonElement item in value.EnumerateArray()) list.Add(ConvertTo(itemType, inner, item));
        return list;
    }

    private object ConvertMap(Type type, string inner, JsonElement value)
    {
        Type itemType = type.IsGenericType ? type.GetGenericArguments()[1] : typeof(object);
        var map = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), itemType))!;
        foreach (JsonProperty entry in value.EnumerateObject()) map[entry.Name] = ConvertTo(itemType, inner, entry.Value);
        return map;
    }

    private object BuildObject(Type type, string shapeName, JsonElement value)
    {
        if (shapeName == "BorderColors")
        {
            Color? Side(string key) => value.TryGetProperty(key, out JsonElement c) ? ToColor(c.GetString()!) : null;
            return new BorderColors(Side("left"), Side("top"), Side("right"), Side("bottom"));
        }
        object instance = Activator.CreateInstance(type)!;
        Populate(instance, value, ElementDescription.Shape(shapeName), []);
        return instance;
    }

    private static object ToNumericType(Type type, double number)
    {
        if (type == typeof(float)) return (float)number;
        if (type == typeof(double)) return number;
        if (type == typeof(decimal)) return (decimal)number;
        if (type == typeof(int)) return (int)Math.Round(number, MidpointRounding.AwayFromZero);
        if (type == typeof(long)) return (long)Math.Round(number, MidpointRounding.AwayFromZero);
        throw new InvalidOperationException($"Cannot store a number in {type.Name}.");
    }

    private static decimal ToDecimal(JsonElement value) => value.TryGetDecimal(out decimal d) ? d : (decimal)value.GetDouble();

    private static float ToPoints(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Number:
                return (float)value.GetDouble();
            case JsonValueKind.String:
                return Units.Parse(value.GetString()!);
            default:
                Unit unit = value.GetProperty("unit").GetString() switch
                {
                    "in" => Unit.Inch,
                    "cm" => Unit.Centimeter,
                    "mm" => Unit.Millimeter,
                    _ => Unit.Point,
                };
                return Units.ToPoints((float)value.GetProperty("value").GetDouble(), unit);
        }
    }

    private static Thickness ToThickness(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Array:
                float[] sides = value.EnumerateArray().Select(ToPoints).ToArray();
                return sides.Length == 2 ? new Thickness(sides[0], sides[1]) : new Thickness(sides[0], sides[1], sides[2], sides[3]);
            case JsonValueKind.Object:
                float Side(string key) => value.TryGetProperty(key, out JsonElement s) ? ToPoints(s) : 0f;
                return new Thickness(Side("left"), Side("top"), Side("right"), Side("bottom"));
            default:
                return new Thickness(ToPoints(value));
        }
    }

    private static Border ToBorder(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return new Border(ToPoints(value));
        if (value.TryGetProperty("width", out JsonElement width)) return new Border(ToPoints(width));
        float Side(string key) => value.TryGetProperty(key, out JsonElement s) ? ToPoints(s) : 0f;
        return new Border(Side("left"), Side("top"), Side("right"), Side("bottom"));
    }

    internal static Color ToColor(string text)
    {
        if (text.StartsWith('#')) return Color.FromHex(text);
        int dash = text.LastIndexOf('-');
        string family = text[..dash];
        int shade = int.Parse(text[(dash + 1)..], CultureInfo.InvariantCulture);
        var palette = (ColorPalette)typeof(ColorPalette).GetProperty(char.ToUpperInvariant(family[0]) + family[1..], BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        return palette[shade];
    }

    internal static PdfSize ToPageSize(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            string token = value.GetString()!;
            return (PdfSize)typeof(PdfSize).GetProperty(char.ToUpperInvariant(token[0]) + token[1..], BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        }
        return new PdfSize(
            (float)value.GetProperty("width").GetDouble(),
            (float)value.GetProperty("height").GetDouble(),
            value.TryGetProperty("name", out JsonElement name) ? name.GetString() : null);
    }

    private static FontWeight ToFontWeight(JsonElement value) =>
        value.ValueKind == JsonValueKind.Number ? (FontWeight)(int)value.GetDouble() : EnumFromToken<FontWeight>(value.GetString()!);

    private static object ToFlags(Type enumType, JsonElement value)
    {
        IEnumerable<string> tokens = value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().Select(t => t.GetString()!) : [value.GetString()!];
        long mask = 0;
        foreach (string token in tokens) mask |= Convert.ToInt64(EnumFromToken(enumType, token), CultureInfo.InvariantCulture);
        return Enum.ToObject(enumType, mask);
    }

    internal static string Normalise(string text) => new(text.Where(char.IsLetterOrDigit).ToArray());

    private static T EnumFromToken<T>(string token)
        where T : struct, Enum =>
        (T)EnumFromToken(typeof(T), token);

    internal static object EnumFromToken(Type enumType, string token)
    {
        string wanted = Normalise(token);
        foreach (string name in Enum.GetNames(enumType))
            if (string.Equals(Normalise(name), wanted, StringComparison.OrdinalIgnoreCase)) return Enum.Parse(enumType, name);
        throw new InvalidOperationException($"{enumType.Name} has no member for the token \"{token}\".");
    }

    /// <summary>Converts a wire token to the enum the property has, using the description's .NET mapping where the names differ.</summary>
    private static object ToEnum(Type target, string enumName, string token)
    {
        EnumSpec spec = D.Enums[enumName];
        if (target == typeof(string))
        {
            // A property that holds the value as text (the chart's shape rendering) takes the mapped member.
            return spec.Members?.GetValueOrDefault(token) ?? token;
        }
        if (spec.Dotnet?.Members is { } members && members.TryGetValue(token, out string? member)) return Enum.Parse(target, member);
        return EnumFromToken(target, token);
    }
}
