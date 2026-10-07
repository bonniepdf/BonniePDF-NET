using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Definition;

/// <summary>
/// The Document Definition validator (spec 15). The fixtures are the shared conformance fixtures
/// document-definition/{valid,invalid}, generated into this suite from the specification; the
/// TypeScript suite runs the same files. Every fixture must produce exactly the errors it lists.
///
/// Mirrors core/tests/definition/definition.test.ts.
/// </summary>
public class DefinitionValidationTests
{
    private static string Key(DefinitionIssue i) => $"{i.Path} {i.Code}";

    private static string Minimal(string pagesJson = """[{"content":[{"type":"TextBlock","text":"x"}]}]""", string extra = "") =>
        $$"""{"version":1,"minorVersion":0{{extra}},"pages":{{pagesJson}}}""";

    public static TheoryData<string> Valid() => DefinitionFixtures.Theory("valid");

    public static TheoryData<string> Invalid() => DefinitionFixtures.Theory("invalid");

    [Theory]
    [MemberData(nameof(Valid))]
    public void ValidFixturesHaveNoErrors(string name)
    {
        DefinitionValidationResult result = DocumentDefinition.Validate(DefinitionFixtures.Text("valid", name));

        Assert.Empty(result.Errors);
        Assert.True(result.IsValid);
        Assert.NotNull(result.Definition);
        using JsonDocument fixture = DefinitionFixtures.Parse("valid", name);
        if (fixture.RootElement.TryGetProperty("_expectWarnings", out JsonElement warnings))
        {
            List<string> expected = warnings.EnumerateArray()
                .Select(w => $"{w.GetProperty("path").GetString()} {w.GetProperty("code").GetString()}")
                .Order(StringComparer.Ordinal).ToList();
            Assert.Equal(expected, result.Warnings.Select(Key).Order(StringComparer.Ordinal));
            DefinitionValidationResult strict = DocumentDefinition.Validate(fixture.RootElement, new DefinitionValidateOptions { Strict = true });
            Assert.Equal(expected, strict.Errors.Select(Key).Order(StringComparer.Ordinal));
        }
    }

    [Theory]
    [MemberData(nameof(Invalid))]
    public void InvalidFixturesReturnExactlyTheListedErrors(string name)
    {
        using JsonDocument fixture = DefinitionFixtures.Parse("invalid", name);
        List<string> expected = fixture.RootElement.GetProperty("_expectErrors").EnumerateArray()
            .Select(e => $"{e.GetProperty("path").GetString()} {e.GetProperty("code").GetString()}").Order(StringComparer.Ordinal).ToList();

        DefinitionValidationResult result = DocumentDefinition.Validate(fixture.RootElement);

        Assert.False(result.IsValid);
        Assert.Null(result.Definition);
        Assert.Equal(expected, result.Errors.Select(Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void NewerMinorReadsTheRestUnderBestEffort()
    {
        using JsonDocument fixture = DefinitionFixtures.Parse("invalid", "newer-minor");
        int warnings = fixture.RootElement.GetProperty("_expectBestEffort").GetProperty("warnings").GetInt32();

        DefinitionValidationResult result = DocumentDefinition.Validate(fixture.RootElement, new DefinitionValidateOptions { BestEffort = true });

        Assert.Empty(result.Errors);
        Assert.Equal(warnings, result.Warnings.Count);
        Assert.All(result.Warnings, w => Assert.Equal("newer-minor", w.Code));
    }

    [Fact]
    public void ReportsEveryProblemInOnePassEachLocated()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal("""[{"content":[{"type":"Spacer","height":"far"},{"type":"Nope"},{"type":"TextBlock"}]}]"""));

        Assert.Equal(
            ["/pages/0/content/0/height invalid-value", "/pages/0/content/1 unknown-type", "/pages/0/content/2 missing-required"],
            r.Errors.Select(Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void NamesTheElementInEveryIssueThatHasAnId()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal("""[{"content":[{"type":"Spacer","id":"gap","height":"far"}]}]"""));

        Assert.Equal("gap", r.Errors[0].Id);
    }

    [Fact]
    public void TreatsUnrecognisedKeysAsWarningsAndAsErrorsWhenStrict()
    {
        string doc = Minimal("""[{"content":[{"type":"Spacer","hieght":4}]}]""");

        Assert.True(DocumentDefinition.Validate(doc).IsValid);
        Assert.Equal(["/pages/0/content/0/hieght unknown-key"], DocumentDefinition.Validate(doc).Warnings.Select(Key));
        Assert.Equal(["/pages/0/content/0/hieght unknown-key"], DocumentDefinition.Validate(doc, new DefinitionValidateOptions { Strict = true }).Errors.Select(Key));
    }

    [Fact]
    public void IgnoresAnnotationKeysAndSchema()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal(extra: ""","$schema":"https://example.com/x.json","_note":"hi" """));

        Assert.True(r.IsValid);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void RejectsTextThatIsNotJsonAndARootThatIsNotAnObject()
    {
        Assert.Equal("invalid-json", DocumentDefinition.Validate("{ nope").Errors[0].Code);
        DefinitionIssue array = DocumentDefinition.Validate("[1]").Errors[0];
        Assert.Equal(("", "invalid-json"), (array.Path, array.Code));
    }

    [Fact]
    public void AcceptsEveryLengthSpacingBorderAndColourForm()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate("""
            {"version":1,"minorVersion":0,
             "settings":{"margin":"2cm","pageColor":"#fff","pageSize":{"width":300,"height":400}},
             "pages":[{"content":[{"type":"Container","margin":[4,"1mm"],"border":{"width":"0.5pt","color":"slate-300"},
               "padding":{"top":{"value":1,"unit":"in"}},"backgroundColor":"#11223344","child":{"type":"Spacer"}}]}]}
            """);

        Assert.Empty(r.Errors);
    }

    [Fact]
    public void RejectsBadLengthsColoursAndTokensWithInvalidValue()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal(
            """[{"content":[{"type":"Container","margin":[1,2,3],"backgroundColor":"slate-80","width":"10km","horizontalAlignment":"diagonal"}]}]"""));

        Assert.Equal(
            [
                "/pages/0/content/0/backgroundColor invalid-value",
                "/pages/0/content/0/horizontalAlignment invalid-value",
                "/pages/0/content/0/margin invalid-value",
                "/pages/0/content/0/width invalid-value",
            ],
            r.Errors.Select(Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ChecksIdsForShapeAndForDuplicates()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal("""[{"content":[{"type":"Spacer","id":"-bad"},{"type":"Spacer","id":"a"},{"type":"Spacer","id":"a"}]}]"""));

        Assert.Equal(["/pages/0/content/0/id invalid-value", "/pages/0/content/2/id duplicate-id"], r.Errors.Select(Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void RequiresAtLeastOnePage()
    {
        Assert.Equal(["/pages invalid-value"], DocumentDefinition.Validate(Minimal("[]")).Errors.Select(Key));
    }

    [Fact]
    public void ChecksTablesStrictRowWidthAndEveryCellForm()
    {
        string doc = Minimal("""
            [{"content":[{"type":"Table","columns":[1,1],"rows":[["a","b","c"],[{"type":"TableCell","content":"span","columnSpan":2}],
              {"cells":["x",{"type":"Spacer"}],"minHeight":20}]}]}]
            """);

        Assert.Empty(DocumentDefinition.Validate(doc).Errors);
        Assert.Equal(["/pages/0/content/0/rows/0 table-row-width"], DocumentDefinition.Validate(doc, new DefinitionValidateOptions { Strict = true }).Errors.Select(Key));
    }

    [Fact]
    public void RejectsUnsupportedChartNumberFormats()
    {
        string chart = """[{"content":[{"type":"BarChart","data":{"series":[{"dataPoints":[{"label":"a","value":1,"labelFormat":"C0"}]}]}}]}]""";

        Assert.Equal(["/pages/0/content/0/data/series/0/dataPoints/0/labelFormat unsupported-format"], DocumentDefinition.Validate(Minimal(chart)).Errors.Select(Key));
    }

    [Fact]
    public void ReportsDuplicateFontFacesAndDanglingAttachmentResources()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate(Minimal(extra: """
            ,"resources":{"a":{"kind":"font","mediaType":"font/ttf","data":"AAAA","family":"Inter","weight":400},
                          "b":{"kind":"font","mediaType":"font/ttf","data":"AAAA","family":"inter","weight":400}},
             "attachments":[{"resource":"missing","fileName":"x.txt"}]
            """));

        Assert.Equal(["/attachments/0/resource dangling-reference", "/resources/b duplicate-font-face"], r.Errors.Select(Key).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void StatesTheRangeThisReleaseReads()
    {
        Assert.Equal(new DefinitionVersions(1, 0, 0), DocumentDefinition.SupportedVersions);
    }

    [Fact]
    public void ReadsNothingWithoutBothStampsAndNamesAnUnsupportedVersion()
    {
        Assert.Equal([" missing-version"], DocumentDefinition.Validate("""{"pages":[]}""").Errors.Select(Key));
        Assert.Equal([" missing-version"], DocumentDefinition.Validate("""{"version":1,"pages":[]}""").Errors.Select(Key));
        DefinitionIssue two = DocumentDefinition.Validate("""{"version":2,"minorVersion":0,"pages":[]}""").Errors[0];
        Assert.Equal(("/version", "unsupported-version"), (two.Path, two.Code));
        DefinitionIssue text = DocumentDefinition.Validate("""{"version":"one","minorVersion":0,"pages":[]}""").Errors[0];
        Assert.Equal(("/version", "invalid-type"), (text.Path, text.Code));
    }

    [Fact]
    public void WarnsWhenANewerMinorVersionIsClaimedButNothingNeededIt()
    {
        DefinitionValidationResult r = DocumentDefinition.Validate("""{"version":1,"minorVersion":4,"pages":[{"content":[{"type":"TextBlock","text":"x"}]}]}""");

        Assert.True(r.IsValid);
        Assert.Equal(["/minorVersion newer-minor"], r.Warnings.Select(Key));
    }

    [Fact]
    public void RefusesInputThatIsTooBigTooDeepOrHasTooManyElements()
    {
        Assert.Equal("limit-exceeded", DocumentDefinition.Validate(Minimal(), new DefinitionValidateOptions { Limits = new DefinitionLimits { InputBytes = 10 } }).Errors[0].Code);
        Assert.Equal("limit-exceeded", DocumentDefinition.Validate(new string('[', 200) + new string(']', 200)).Errors[0].Code);

        string many = Minimal("""[{"content":[{"type":"Spacer"},{"type":"Spacer"},{"type":"Spacer"},{"type":"Spacer"},{"type":"Spacer"},{"type":"Spacer"}]}]""");
        Assert.Equal(["limit-exceeded"], DocumentDefinition.Validate(many, new DefinitionValidateOptions { Limits = new DefinitionLimits { ElementNodes = 5 } }).Errors.Select(e => e.Code));

        string deep = """{"type":"Spacer"}""";
        for (int i = 0; i < 40; i++) deep = $$"""{"type":"Container","child":{{deep}}}""";
        Assert.Equal(["limit-exceeded"], DocumentDefinition.Validate(Minimal($$"""[{"content":[{{deep}}]}]""")).Errors.Select(e => e.Code));
    }

    [Fact]
    public void RefusesALongStringAndTooManyPages()
    {
        string longText = Minimal($$"""[{"content":[{"type":"TextBlock","text":"{{new string('x', 50)}}"}]}]""");
        Assert.Equal(["limit-exceeded"], DocumentDefinition.Validate(longText, new DefinitionValidateOptions { Limits = new DefinitionLimits { StringLength = 10 } }).Errors.Select(e => e.Code));

        string pages = Minimal("""[{"content":[]},{"content":[]},{"content":[]}]""");
        Assert.Equal(["limit-exceeded"], DocumentDefinition.Validate(pages, new DefinitionValidateOptions { Limits = new DefinitionLimits { Pages = 2 } }).Errors.Select(e => e.Code));
    }
}
