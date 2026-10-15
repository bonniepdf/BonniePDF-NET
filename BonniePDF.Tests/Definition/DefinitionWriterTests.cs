using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Definition;

/// <summary>
/// The Document Definition writer (spec 15 §11, §14, §21). Three kinds of check:
/// every valid fixture reads, writes and reads back to a document with identical state and identical PDF bytes;
/// every key of every element, with every token it accepts, survives read, write, read (generated from the element
/// description, so a new key is covered the day it is added); and the constructs a definition cannot express are
/// reported, not approximated.
///
/// Mirrors core/tests/definition/writer.test.ts.
/// </summary>
[Collection("Serial")] // compares PDF bytes, which other tests' global font registrations would change between two generations
public partial class DefinitionWriterTests
{
    private static readonly DescriptionData D = ElementDescription.Data;

    [GeneratedRegex(@"/ID \[[^\]]*\]")]
    private static partial Regex TrailerId();

    [GeneratedRegex("[A-Z]{6}\\+")]
    private static partial Regex SubsetPrefix();

    // The PDF dates and their XMP copies (as TypeScript's definition helpers mask them): two
    // generations a moment apart can straddle a second.
    [GeneratedRegex(@"/(?:Creation|Mod)Date[^\n]*|\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+-]\d{2}:\d{2})")]
    private static partial Regex Dates();

    /// <summary>A 1x1 PNG.</summary>
    private static readonly byte[] Png = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

    private static string Masked(byte[] pdf) =>
        Dates().Replace(SubsetPrefix().Replace(TrailerId().Replace(Encoding.Latin1.GetString(pdf), "/ID []"), "S+"), "");

    private static async Task<(BonniePDF.Document First, BonniePDF.Document Second, DefinitionWriteResult Written, DefinitionWriteResult Rewritten)> RoundTrip(string json)
    {
        BonniePDF.Document first = await BonniePDF.Document.FromDefinitionAsync(json);
        DefinitionWriteResult written = first.ToDefinition();
        BonniePDF.Document second = await BonniePDF.Document.FromDefinitionAsync(written.ToJson());
        return (first, second, written, second.ToDefinition());
    }

    // ── the valid fixtures ───────────────────────────────────────────────────

    public static TheoryData<string> Valid() => DefinitionFixtures.Theory("valid");

    [Theory]
    [MemberData(nameof(Valid))]
    public async Task ValidFixturesReadWriteAndReadBackToTheSameDocument(string name)
    {
        string json = DefinitionFixtures.Text("valid", name);
        var (first, second, written, rewritten) = await RoundTrip(json);

        Assert.Empty(DocumentDefinition.Validate(written.ToJson()).Errors);
        Assert.Null(StateComparer.Find(first, second));

        // The canonical form is stable: writing what was written changes nothing.
        Assert.Equal(written.ToJson(), rewritten.ToJson());

        // Same bytes, masking the trailer id, the dates a document gives itself and the random tag of a subset font.
        Assert.Equal(Masked(first.GenerateBytes()), Masked(second.GenerateBytes()));

        using JsonDocument fixture = JsonDocument.Parse(json);
        if (fixture.RootElement.TryGetProperty("_expect", out JsonElement expect) && expect.TryGetProperty("canonical", out JsonElement canonical))
        {
            Assert.True(
                Equivalent(JsonNode.Parse(written.ToJson()), JsonNode.Parse(canonical.GetRawText())),
                $"The canonical form differs from the one the fixture pins:\n{written.ToJson(indented: true)}");
        }
    }

    /// <summary>
    /// Structural equality across the cores (spec 14 §8): the same values, key order ignored, numbers compared to three
    /// decimal places. A length in centimetres is 56.69291338582677 in a double and 56.692913 in a float.
    /// </summary>
    private static bool Equivalent(JsonNode? a, JsonNode? b)
    {
        switch (a, b)
        {
            case (JsonObject x, JsonObject y):
                return x.Count == y.Count && x.All(pair => y.ContainsKey(pair.Key) && Equivalent(pair.Value, y[pair.Key]));
            case (JsonArray x, JsonArray y):
                return x.Count == y.Count && x.Zip(y).All(pair => Equivalent(pair.First, pair.Second));
            case (JsonValue x, JsonValue y) when x.GetValueKind() == JsonValueKind.Number && y.GetValueKind() == JsonValueKind.Number:
                return Math.Round(x.GetValue<decimal>(), 3) == Math.Round(y.GetValue<decimal>(), 3);
            default:
                return JsonNode.DeepEquals(a, b);
        }
    }

    // ── every key of every element ───────────────────────────────────────────

    private static readonly HashSet<string> SlotKinds = new(StringComparer.Ordinal)
    {
        "element", "elements", "stackChildren", "listItems", "spans", "rows", "cells", "cellContent",
    };

    /// <summary>Values to try for one key: canonical spellings, so what comes back can be compared to what went in.</summary>
    private static List<JsonNode> Samples(string key, KeySpec spec)
    {
        int colon = spec.Kind.IndexOf(':', StringComparison.Ordinal);
        string head = colon < 0 ? spec.Kind : spec.Kind[..colon];
        string arg = colon < 0 ? string.Empty : spec.Kind[(colon + 1)..];
        switch (head)
        {
            case "string":
                return [key switch
                {
                    "fontName" => "Times-Roman",
                    "fontFamily" => "Times",
                    "uri" => "https://example.com/a",
                    "anchorTarget" => "target",
                    "anchorId" => "anchor-x",
                    "tocIdentifier" => "toc-x",
                    _ => "sample",
                }];
            case "boolean":
                return [true, false];
            case "integer" or "number":
            {
                double min = spec.Min ?? 0;
                if (spec.Max is <= 1) return [0.5];
                double v = head == "integer" ? min + 2 : min + 2.5;
                return [spec.Max is { } max ? Math.Min(v, max) : v];
            }
            case "length":
                return [13];
            case "color":
                return ["#336699", "#33669980"];
            case "spacing":
                return [4, new JsonObject { ["top"] = 1, ["left"] = 2 }];
            case "border":
                return [3, new JsonObject { ["top"] = 1 }];
            case "fontWeight":
                return ["bold", 650];
            case "decoration":
                return ["underline", new JsonArray("underline", "overline")];
            case "enum":
                return D.Enums[arg].Tokens.Select(t => (JsonNode)t).ToList();
            case "chartNumberFormat":
                return ["N2", "P1"];
            case "dateTime":
                return ["2020-01-02T03:04:05.000Z"];
            case "permissions":
                return ["none", new JsonArray("print", "copy")];
            case "array":
                return arg switch
                {
                    "string" => [new JsonArray("a", "b")],
                    "number" => [new JsonArray(1, 2)],
                    "color" => [new JsonArray("#111111", "#222222")],
                    _ => [],
                };
            case "map":
                return arg == "string" ? [new JsonObject { ["k"] = "v" }] : [];
            case "object":
                return arg == "ChartData" ? [] : [SampleObject(ElementDescription.Shape(arg))];
            default:
                return [];
        }
    }

    /// <summary>Barcode.QuietZone is an int here where the wire says number (spec 15 §20.3), so its sample is a whole number.</summary>
    private static JsonNode AsDotnetValue(string type, string key, JsonNode sample) =>
        type == "Barcode" && key == "quietZone" ? 3 : sample;

    private static JsonObject SampleObject(ShapeSpec shape)
    {
        var result = new JsonObject();
        var conflicts = new HashSet<string>((shape.Conflicts ?? []).SelectMany(c => c.Skip(1)), StringComparer.Ordinal);
        foreach ((string key, KeySpec spec) in ElementDescription.KeysOf(shape))
        {
            if (SlotKinds.Contains(spec.Kind.Split(':')[0]) || conflicts.Contains(key)) continue;
            if (spec.Kind.StartsWith("object:", StringComparison.Ordinal) && spec.Kind is not ("object:TextStyle" or "object:BorderColors" or "object:Axis")) continue;
            JsonNode? first = Samples(key, spec).FirstOrDefault();
            if (first is not null) result[key] = first;
        }
        return result;
    }

    /// <summary>The smallest valid node of a type, with the definition-level things it refers to.</summary>
    private static JsonObject BaseNode(string type)
    {
        var node = new JsonObject { ["type"] = type };
        foreach ((string key, KeySpec spec) in ElementDescription.KeysOf(D.Elements[type]))
        {
            if (!spec.Required) continue;
            string head = spec.Kind.Split(':')[0];
            node[key] = head switch
            {
                "element" => new JsonObject { ["type"] = "Spacer" },
                "elements" or "stackChildren" => new JsonArray(new JsonObject { ["type"] = "Spacer" }),
                "listItems" => new JsonArray("a"),
                "spans" => new JsonArray(new JsonObject { ["text"] = "a" }),
                "rows" => new JsonArray(new JsonArray("a")),
                _ when spec.Kind == "object:ChartData" => ChartData(),
                _ when key == "role" => "P",
                _ when key == "headingIdentifiers" => new JsonArray("h1"),
                _ => Samples(key, spec)[0],
            };
        }
        if (type == "Hyperlink") node["uri"] = "https://example.com";
        if (type == "Image") node["resource"] = "img";
        if (type == "Svg") node["svgData"] = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"><rect width=\"10\" height=\"10\"/></svg>";
        return node;
    }

    private static JsonObject ChartData() => new()
    {
        ["series"] = new JsonArray(new JsonObject
        {
            ["name"] = "s",
            ["dataPoints"] = new JsonArray(new JsonObject { ["label"] = "a", ["value"] = 1 }),
        }),
    };

    private static string DefinitionAround(JsonObject node) =>
        new JsonObject
        {
            ["version"] = 1,
            ["minorVersion"] = 0,
            ["resources"] = new JsonObject
            {
                ["img"] = new JsonObject { ["kind"] = "image", ["mediaType"] = "image/png", ["data"] = Convert.ToBase64String(Png) },
            },
            ["pages"] = new JsonArray(new JsonObject
            {
                ["content"] = new JsonArray(
                    new JsonObject { ["type"] = "Section", ["anchorId"] = "target" },
                    new JsonObject { ["type"] = "Heading", ["text"] = "H", ["tocIdentifier"] = "h1" },
                    node),
            }),
        }.ToJsonString();

    /// <summary>True when everything written is what was given; a nested value may leave out members equal to their defaults.</summary>
    private static bool IsSubset(JsonNode? written, JsonNode? given)
    {
        if (written is JsonObject w && given is JsonObject g)
            return w.All(pair => IsSubset(pair.Value, g[pair.Key]));
        if (written is JsonArray wa && given is JsonArray ga)
            return wa.Count == ga.Count && wa.Select((x, i) => IsSubset(x, ga[i])).All(x => x);
        return JsonNode.DeepEquals(written, given);
    }

    public static TheoryData<string> ElementTypes()
    {
        TheoryData<string> data = new();
        foreach (string type in D.Elements.Keys) data.Add(type);
        return data;
    }

    [Theory]
    [MemberData(nameof(ElementTypes))]
    public async Task EveryKeyOfEveryElementSurvivesReadWriteRead(string type)
    {
        var problems = new List<string>();
        foreach ((string key, KeySpec spec) in ElementDescription.KeysOf(D.Elements[type]))
        {
            if (SlotKinds.Contains(spec.Kind.Split(':')[0])) continue;
            foreach (JsonNode sample in Samples(key, spec).Select(x => AsDotnetValue(type, key, x)))
            {
                JsonObject node = BaseNode(type);
                node[key] = sample.DeepClone();
                // A choice between keys (fontName/fontFamily, uri/targetPageIndex) makes some pairings illegal.
                if (key == "targetPageIndex") node.Remove("uri");
                if (key == "fontFamily") node.Remove("fontName");
                if (key == "resource" && type == "Svg") node.Remove("svgData");
                if (key == "svgData" && type == "Svg") node.Remove("resource");
                string json = DefinitionAround(node);
                if (!DocumentDefinition.Validate(json).IsValid) continue;

                string label = $"{type}.{key}={sample.ToJsonString()}";
                try
                {
                    var (first, second, written, rewritten) = await RoundTrip(json);
                    string? diff = StateComparer.Find(first, second);
                    if (diff is not null) problems.Add($"{label}: state differs at {diff}");
                    if (written.ToJson() != rewritten.ToJson()) problems.Add($"{label}: the canonical form is not stable");
                    // Either the value is written back exactly, or it is the default and is left out.
                    JsonArray content = written.Definition["pages"]![0]!["content"]!.AsArray();
                    JsonObject back = content[^1]!.AsObject();
                    if (back.ContainsKey(key) && !IsSubset(back[key], sample)) problems.Add($"{label}: written as {back[key]!.ToJsonString()}");
                }
                catch (Exception e) when (e is not Xunit.Sdk.XunitException)
                {
                    problems.Add($"{label}: {e.GetType().Name}: {e.Message}");
                }
            }
        }
        Assert.Empty(problems);
    }

    // ── structure the generated test does not reach ──────────────────────────

    [Fact]
    public async Task WritesChildrenWeightsLayersWrappersAndPageLevelContent()
    {
        const string json = """
            {"version":1,"minorVersion":0,
             "settings":{"pageSize":"a5","orientation":"landscape","margin":30},
             "pages":[
              {"content":[
                {"type":"HorizontalStack","widthDistribution":"weighted","children":[
                   {"element":{"type":"TextBlock","text":"left"},"weight":2},
                   {"type":"TextBlock","text":"right"}]},
                {"type":"Layers","primary":{"type":"Container","padding":6,"child":{"type":"TextBlock","text":"top"}},
                 "backgrounds":[{"type":"Circle","diameter":40,"fillColor":"#eeeeee"}],
                 "foregrounds":[{"type":"Line","length":30}]},
                {"type":"EnsureSpace","minHeight":120,"child":{"type":"TextBlock","text":"kept together"}},
                {"type":"SemanticGroup","role":"Sect","children":[{"type":"TextBlock","text":"g"}]},
                {"type":"PageBreak"}]},
              {"size":"letter","orientation":"portrait","margin":20,"pageColor":"slate-100",
               "background":{"type":"Spacer","height":4},"foreground":{"type":"Spacer","height":5},
               "content":[{"type":"TextBlock","text":"second"}]},
              {"content":[]}]}
            """;
        var (first, second, written, _) = await RoundTrip(json);
        Assert.Null(StateComparer.Find(first, second));

        JsonArray pages = written.Definition["pages"]!.AsArray();
        Assert.Equal(3, pages.Count);
        Assert.Null(pages[0]!["size"]);
        Assert.Equal("letter", pages[1]!["size"]!.GetValue<string>());
        Assert.Equal("portrait", pages[1]!["orientation"]!.GetValue<string>());
        Assert.Equal(20, pages[1]!["margin"]!.GetValue<float>());
        Assert.Equal("#f1f5f9", pages[1]!["pageColor"]!.GetValue<string>());
        Assert.Equal("""{"content":[]}""", pages[2]!.ToJsonString());
        JsonNode stack = pages[0]!["content"]![0]!;
        Assert.Equal(
            """[{"element":{"type":"TextBlock","text":"left"},"weight":2},{"type":"TextBlock","text":"right"}]""",
            stack["children"]!.ToJsonString());
    }

    [Fact]
    public async Task HonoursAMarginStatedOnTheFirstPageAndWritesItBack()
    {
        const string json = """
            {"version":1,"minorVersion":0,"settings":{"margin":50},
             "pages":[{"margin":20,"content":[{"type":"TextBlock","text":"x"}]},{"content":[]}]}
            """;
        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(json);
        SvgDocumentExport exported = await doc.GenerateSvgDocumentAsync(new SvgPageExportOptions { CaptureLayout = true });
        // The text starts at the page's own margin, not the document's.
        Assert.Equal(20, exported.Snapshot!.Root.Children[0].Rects[0].X);
        JsonArray pages = doc.ToDefinition().Definition["pages"]!.AsArray();
        Assert.Equal(20, pages[0]!["margin"]!.GetValue<float>());
        Assert.Null(pages[1]!["margin"]);
    }

    [Fact]
    public async Task WritesTableSectionsSpansHeaderCellsAndPlainTextCells()
    {
        const string json = """
            {"version":1,"minorVersion":0,"pages":[{"content":[
              {"type":"Table","columns":[1,2],
               "header":[["Item","Cost"]],
               "rows":[
                 ["Tea",{"type":"TableCell","content":"Cheap","color":"#ff0000","columnSpan":1}],
                 {"cells":["Coffee",{"type":"Circle","diameter":4}],"backgroundColor":"#eeeeee","minHeight":30},
                 [{"type":"TableCell","content":{"type":"TextBlock","text":"Total","fontSize":14},"isHeader":true},"3"]],
               "footer":[["Sum","3"]]},
              {"type":"RichText","defaultStyle":{"fontSize":10},
               "spans":[{"text":"a","style":{"fontWeight":"bold"}},{"lineBreak":true},{"text":"b"}]},
              {"type":"UnorderedList","items":["one",{"type":"UnorderedList","items":["nested"]},"two"]}]}]}
            """;
        var (first, second, written, _) = await RoundTrip(json);
        Assert.Null(StateComparer.Find(first, second));
        JsonNode table = written.Definition["pages"]![0]!["content"]![0]!;
        Assert.Equal("""[["Item","Cost"]]""", table["header"]!.ToJsonString());
        Assert.Equal(
            """[{"type":"TableCell","content":{"type":"TextBlock","fontSize":14,"text":"Total"},"isHeader":true},"3"]""",
            table["rows"]![2]!.ToJsonString());
    }

    [Fact]
    public void KeepsAnIdThatIsAValidIdAndLeavesOutOneThatIsNot()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(DebugSource.Mark(new TextBlock { Text = "a" }, "title"));
        doc.Add(DebugSource.Mark(new TextBlock { Text = "b" }, "not valid!"));
        doc.Add(DebugSource.Mark(new TextBlock { Text = "c" }, "title"));
        JsonArray content = doc.ToDefinition().Definition["pages"]![0]!["content"]!.AsArray();
        Assert.Equal(["title", null, null], content.Select(n => n!["id"]?.GetValue<string>()));
    }

    // ── text: written only when set ──────────────────────────────────────────

    [Fact]
    public void OmitsFontSizeColourLineHeightAndAlignmentThatWereNeverSet()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = "plain" });
        doc.Add(new TextBlock { Text = "chosen", FontSize = 12, Color = BonniePDF.Color.Black, Alignment = TextAlignment.Left, LineHeight = 1.2f });
        JsonArray content = doc.ToDefinition().Definition["pages"]![0]!["content"]!.AsArray();
        Assert.Equal("""{"type":"TextBlock","text":"plain"}""", content[0]!.ToJsonString());
        // Set to the same value as the default: still written, because a default text style would not override it.
        Assert.Equal(12, content[1]!["fontSize"]!.GetValue<float>());
        Assert.Equal("#000000", content[1]!["color"]!.GetValue<string>());
        Assert.Equal("left", content[1]!["alignment"]!.GetValue<string>());
        Assert.Equal(1.2f, content[1]!["lineHeight"]!.GetValue<float>());
    }

    [Fact]
    public async Task ADefaultTextStyleStillReachesAnElementThatDidNotSetTheValueAfterARoundTrip()
    {
        const string json = """
            {"version":1,"minorVersion":0,"settings":{"defaultTextStyle":{"fontSize":20,"color":"#ff0000"}},
             "pages":[{"content":[{"type":"TextBlock","text":"inherits"},{"type":"TextBlock","text":"explicit","fontSize":12}]}]}
            """;
        var (first, second, _, _) = await RoundTrip(json);
        Assert.Equal(Masked(first.GenerateBytes()), Masked(second.GenerateBytes()));
    }

    // ── document-level ───────────────────────────────────────────────────────

    [Fact]
    public async Task RoundTripsADocumentThatUsesSettingsMetadataResourcesAndAttachments()
    {
        byte[] font = FontManager.GetBytesForFamily("liberation sans", "regular") ?? throw new InvalidOperationException("font");
        string json = """
            {"version":1,"minorVersion":0,
             "settings":{"pageSize":{"width":300,"height":400},"margin":24,"pageColor":"#fafafa","pdfVersion":"pdf-1.7",
               "conformance":"pdf/a-2b","language":"en-GB","defaultZoom":"fit-width","pageMode":"use-outlines","hideToolbar":true,
               "maxImageDpi":150,"defaultTextStyle":{"fontFamily":"Liberation Sans","fontSize":11},
               "watermark":{"text":"DRAFT","opacity":0.3},"pageLabel":{"style":"lowercase-roman","startAt":3,"prefix":"p-"},
               "namedDestinations":{"start":0},
               "bookmarks":[{"title":"One","pageIndex":0,"children":[{"title":"Nested","pageIndex":0}]}],
               "facturX":{"documentType":"INVOICE","documentFileName":"factur-x.xml","version":"1.0","conformanceLevel":"BASIC"}},
             "metadata":{"title":"T","author":"A","creationDate":"2020-01-02T03:04:05.000Z","modifiedDate":"2021-01-02T03:04:05.000Z",
               "customProperties":{"Invoice":"42"}},
             "resources":{
               "logo":{"kind":"image","mediaType":"image/png","data":"@PNG@"},
               "body":{"kind":"font","mediaType":"font/ttf","data":"@FONT@","family":"Liberation Sans","weight":"normal","italic":false},
               "xml":{"kind":"attachment","mediaType":"text/xml","data":"PHgvPg=="}},
             "attachments":[{"resource":"xml","fileName":"factur-x.xml","relationship":"alternative","mediaType":"text/xml",
               "description":"invoice","creationDate":"2020-01-02T03:04:05.000Z","modificationDate":"2020-01-03T03:04:05.000Z"}],
             "pages":[{"content":[{"type":"Image","resource":"logo","width":20},{"type":"Heading","text":"Hi","anchorId":"a"}]}]}
            """.Replace("@PNG@", Convert.ToBase64String(Png), StringComparison.Ordinal).Replace("@FONT@", Convert.ToBase64String(font), StringComparison.Ordinal);
        var (first, second, written, rewritten) = await RoundTrip(json);

        Assert.Empty(written.Notes);
        Assert.Null(StateComparer.Find(first, second));
        Assert.Equal(written.ToJson(), rewritten.ToJson());
        Assert.Equal(
            ["version", "minorVersion", "settings", "metadata", "resources", "attachments", "pages"],
            written.Definition.Select(p => p.Key));
        Assert.Equal(["font-1", "image-1", "attachment-1"], written.Definition["resources"]!.AsObject().Select(p => p.Key));
        Assert.Equal("2020-01-02T03:04:05.000Z", written.Definition["metadata"]!["creationDate"]!.GetValue<string>());
        Assert.Equal(Masked(first.GenerateBytes()), Masked(second.GenerateBytes()));
    }

    [Fact]
    public async Task ReadsAndWritesMaxImageDpiUnlimitedAsNoCap()
    {
        const string template = """{"version":1,"minorVersion":0,"settings":{"maxImageDpi":@V@},"pages":[{"content":[]}]}""";
        string Json(string v) => template.Replace("@V@", v, StringComparison.Ordinal);

        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(Json("\"unlimited\""));
        Assert.Null(doc.Settings.MaxImageDpi);
        Assert.Equal("""{"maxImageDpi":"unlimited"}""", doc.ToDefinition().Definition["settings"]!.ToJsonString());

        using BonniePDF.Document capped = await BonniePDF.Document.FromDefinitionAsync(Json("150"));
        Assert.Equal("""{"maxImageDpi":150}""", capped.ToDefinition().Definition["settings"]!.ToJsonString());

        Assert.Equal(["/settings/maxImageDpi invalid-value"], DocumentDefinition.Validate(Json("\"lots\"")).Errors.Select(e => $"{e.Path} {e.Code}"));
        Assert.Equal(["/settings/maxImageDpi invalid-value"], DocumentDefinition.Validate(Json("0")).Errors.Select(e => $"{e.Path} {e.Code}"));
    }

    [Fact]
    public void DoesNotWriteTheDatesADocumentGaveItself()
    {
        using var doc = new BonniePDF.Document();
        Assert.Null(doc.ToDefinition().Definition["metadata"]);
    }

    [Fact]
    public void PutsIdenticalBytesInOneResource()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new Image { ImageData = Png });
        doc.Add(new Image { ImageData = (byte[])Png.Clone() });
        Assert.Equal(["image-1"], doc.ToDefinition().Definition["resources"]!.AsObject().Select(p => p.Key));
    }

    [Fact]
    public void AsksTheHostToNameAResourceAndWritesARefWithASha256WhenItDoes()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new Image { ImageData = Png });
        DefinitionWriteResult named = doc.ToDefinition(new DefinitionWriteOptions { ResourceRef = r => r.Kind == "image" ? "northwind-logo" : null });
        JsonNode resource = named.Definition["resources"]!["image-1"]!;
        Assert.Equal("northwind-logo", resource["ref"]!.GetValue<string>());
        Assert.Matches("^[0-9a-f]{64}$", resource["sha256"]!.GetValue<string>());
        Assert.Null(resource["data"]);

        DefinitionWriteResult embedded = doc.ToDefinition(new DefinitionWriteOptions { ResourceRef = _ => "x", EmbedResources = true });
        Assert.Equal(Convert.ToBase64String(Png), embedded.Definition["resources"]!["image-1"]!["data"]!.GetValue<string>());
    }

    [Fact]
    public async Task WritesEncryptionWithoutItsPasswordsAndSaysSo()
    {
        var settings = new DocumentSettings
        {
            Version = PdfVersion.Pdf17,
            Encryption = new BonniePDF.EncryptionSettings { Algorithm = BonniePDF.EncryptionAlgorithm.AES_256, OwnerPassword = "secret", UserPassword = "open" },
        };
        using var doc = new BonniePDF.Document(settings);
        doc.Add(new TextBlock { Text = "x" });
        DefinitionWriteResult result = doc.ToDefinition();
        string text = result.ToJson();
        Assert.DoesNotContain("secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"open\"", text, StringComparison.Ordinal);
        Assert.Equal("aes-256", result.Definition["settings"]!["encryption"]!["algorithm"]!.GetValue<string>());
        DefinitionIssue note = Assert.Single(result.Notes);
        Assert.Equal(("/settings/encryption", "secret-omitted"), (note.Path, note.Code));

        DefinitionException error = await Assert.ThrowsAsync<DefinitionException>(() => BonniePDF.Document.FromDefinitionAsync(text));
        Assert.Equal("secret-required", Assert.Single(error.Issues).Code);
        using BonniePDF.Document back = await BonniePDF.Document.FromDefinitionAsync(
            text, new DefinitionReadOptions { Passwords = new DefinitionPasswords { Owner = "secret", User = "open" } });
        Assert.True(back.Settings.IsEncrypted);
    }

    [Fact]
    public void WritesPageChromeBuiltFromElements()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.FooterTemplate = PageTemplate.FromElements(
            @default: new TextBlock { Text = "Page {{CurrentPage}}" },
            firstPage: new TextBlock { Text = "Cover" });
        doc.Add(new TextBlock { Text = "x" });
        Assert.Equal(
            """{"footer":{"default":{"type":"TextBlock","text":"Page {{CurrentPage}}"},"firstPage":{"type":"TextBlock","text":"Cover"}}}""",
            doc.ToDefinition().Definition["settings"]!["pageChrome"]!.ToJsonString());
    }

    [Fact]
    public void PaletteTokensAreWrittenOnlyWhenAskedFor()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new Container { BackgroundColor = BonniePDF.ColorPalette.Slate[100] });
        Assert.Equal("#f1f5f9", doc.ToDefinition().Definition["pages"]![0]!["content"]![0]!["backgroundColor"]!.GetValue<string>());
        Assert.Equal(
            "slate-100",
            doc.ToDefinition(new DefinitionWriteOptions { WritePaletteTokens = true }).Definition["pages"]![0]!["content"]![0]!["backgroundColor"]!.GetValue<string>());
    }

    [Fact]
    public void RefusesAVersionItCannotWrite()
    {
        using var doc = new BonniePDF.Document();
        DefinitionException error = Assert.Throws<DefinitionException>(() => doc.ToDefinition(new DefinitionWriteOptions { Target = "2.0" }));
        Assert.Equal("unsupported-version", Assert.Single(error.Issues).Code);
        Assert.NotNull(doc.ToDefinition(new DefinitionWriteOptions { Target = "1.0" }));
    }

    // ── what cannot be written ───────────────────────────────────────────────

    private sealed class CustomText : TextBlock;

    private static BonniePDF.Document DocumentFromRecipe()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.FooterTemplate = page => new TextBlock { Text = $"Page {page}" };
        doc.Add(new TextBlock { Text = "Kept" });
        doc.Add(new CustomText { Text = "custom" });
        return doc;
    }

    [Fact]
    public void FailsByDefaultListingEveryConstructAndWritingNothing()
    {
        using BonniePDF.Document doc = DocumentFromRecipe();
        DefinitionException error = Assert.Throws<DefinitionException>(() => doc.ToDefinition());
        Assert.Equal(
            ["/settings/footerTemplate unrepresentable", "/pages/0/content/1 unrepresentable"],
            error.Issues.Select(i => $"{i.Path} {i.Code}"));
    }

    [Fact]
    public void DropsThemOnRequestAndNotesEachOne()
    {
        using BonniePDF.Document doc = DocumentFromRecipe();
        DefinitionWriteResult result = doc.ToDefinition(new DefinitionWriteOptions { OnUnrepresentable = DefinitionUnrepresentable.Drop });
        Assert.Equal(
            ["/settings/footerTemplate dropped", "/pages/0/content/1 dropped"],
            result.Notes.Select(n => $"{n.Path} {n.Code}"));
        Assert.Equal(
            """{"version":1,"minorVersion":0,"pages":[{"content":[{"type":"TextBlock","text":"Kept"}]}]}""",
            result.ToJson());
    }

    [Fact]
    public void ReportsAShowIfPredicateAnSvgGeneratorAndAnImageWithNoBytes()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new ShowIf(_ => true, new TextBlock { Text = "x" }));
        doc.Add(new Svg { SvgGenerator = _ => "<svg/>", Width = 10, Height = 10 });
        doc.Add(new Image { FilePath = "/nonexistent/logo.png" });
        DefinitionException error = Assert.Throws<DefinitionException>(() => doc.ToDefinition());
        Assert.Equal(
            ["/pages/0/content/0 unrepresentable", "/pages/0/content/1 unrepresentable", "/pages/0/content/2 unrepresentable"],
            error.Issues.Select(i => $"{i.Path} {i.Code}"));
    }
}
