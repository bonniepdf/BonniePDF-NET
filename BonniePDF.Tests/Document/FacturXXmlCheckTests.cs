using System.Text;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// The Factur-X invoice XML check, driven by the shared fixture <c>factur-x/xml-check.json</c>
/// (spec 04 §11.4), copied into <c>Fixtures/factur-x</c>. Parity peer of the TypeScript
/// <c>tests/document/factur-x-xml-check.test.ts</c>.
/// </summary>
public class FacturXXmlCheckTests
{
    private static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "Document", "Fixtures", "factur-x");

    private static readonly Dictionary<string, FacturXProfile> Profiles = new(StringComparer.Ordinal)
    {
        ["minimum"] = FacturXProfile.Minimum,
        ["basicWl"] = FacturXProfile.BasicWl,
        ["basic"] = FacturXProfile.Basic,
        ["en16931"] = FacturXProfile.En16931,
        ["extended"] = FacturXProfile.Extended,
        ["xRechnung"] = FacturXProfile.XRechnung,
    };

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir, "xml-check.json")));

    public static TheoryData<string> Accepted() => Ids("accepted");

    public static TheoryData<string> Rejected() => Ids("rejected");

    private static TheoryData<string> Ids(string group)
    {
        using var fixture = Fixture();
        var data = new TheoryData<string>();
        foreach (var item in fixture.RootElement.GetProperty(group).EnumerateArray())
            data.Add(item.GetProperty("id").GetString()!);
        return data;
    }

    private static JsonElement Case(JsonDocument fixture, string group, string id) =>
        fixture.RootElement.GetProperty(group).EnumerateArray().Single(c => string.Equals(c.GetProperty("id").GetString(), id, StringComparison.Ordinal));

    private static byte[] Input(JsonElement c)
    {
        if (c.TryGetProperty("file", out var file)) return File.ReadAllBytes(Path.Combine(Dir, file.GetString()!));
        if (c.TryGetProperty("bytes", out var bytes)) return Convert.FromBase64String(bytes.GetString()!);
        byte[] text = Encoding.UTF8.GetBytes(c.GetProperty("xml").GetString()!);
        bool bom = c.TryGetProperty("prefixBom", out var prefix) && prefix.GetBoolean();
        return bom ? [0xEF, 0xBB, 0xBF, .. text] : text;
    }

    private static FacturXProfile? Stated(JsonElement c) =>
        c.TryGetProperty("statedProfile", out var p) ? Profiles[p.GetString()!] : null;

    [Fact]
    public void Fixture_CoversEveryCode()
    {
        using var fixture = Fixture();
        var codes = fixture.RootElement.GetProperty("rejected").EnumerateArray()
            .Select(c => c.GetProperty("code").GetString()).ToHashSet(StringComparer.Ordinal);
        foreach (var code in new[]
                 {
                     "unsupported-encoding", "malformed-xml", "doctype-forbidden", "not-cii",
                     "guideline-missing", "guideline-unrecognized", "profile-mismatch",
                 })
        {
            Assert.Contains(code, codes);
        }
    }

    [Theory]
    [MemberData(nameof(Accepted))]
    public void Accepts(string id)
    {
        using var fixture = Fixture();
        var c = Case(fixture, "accepted", id);
        using var doc = new BonniePDF.Document();

        FacturX.Apply(doc, Input(c), Stated(c));

        string level = c.GetProperty("conformanceLevel").GetString()!;
        Assert.Equal(level, doc.Settings.FacturX?.ConformanceLevel);
        string profile = c.GetProperty("profile").GetString()!;
        Assert.Equal(level, fixture.RootElement.GetProperty("_fixture").GetProperty("profiles").GetProperty(profile).GetString());
    }

    [Theory]
    [MemberData(nameof(Rejected))]
    public void Rejects(string id)
    {
        using var fixture = Fixture();
        var c = Case(fixture, "rejected", id);
        using var doc = new BonniePDF.Document();

        var error = Assert.Throws<FacturXException>(() => FacturX.Apply(doc, Input(c), Stated(c)));

        string code = c.GetProperty("code").GetString()!;
        Assert.Equal(code, error.Code);
        Assert.Contains($"({code})", error.Message);
        Assert.IsAssignableFrom<BonniePdfException>(error);
        // The document is left unchanged.
        Assert.Equal(PdfAConformance.None, doc.Settings.Conformance);
        Assert.Null(doc.Settings.FacturX);
        Assert.Empty(doc.Attachments);
    }
}
