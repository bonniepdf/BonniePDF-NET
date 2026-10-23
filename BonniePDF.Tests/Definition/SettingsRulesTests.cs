using System.Text;
using System.Text.Json;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Definition;

/// <summary>
/// The cross-key rules of <c>settings</c>: the shared fixtures
/// document-definition/settings-rules. A combination the validator refuses must be one generation refuses,
/// and one it accepts must generate and land on the PDF version the fixture names, so the rules in the
/// description cannot drift from behaviour.
///
/// Mirrors core/tests/definition/settings-rules.test.ts.
/// </summary>
[Collection("Serial")]
public class SettingsRulesTests
{
    public static TheoryData<string> Fixtures() => DefinitionFixtures.Theory("settings-rules");

    private static PdfVersion Version(string token) => token switch
    {
        "pdf-1.4" => PdfVersion.Pdf14,
        "pdf-1.7" => PdfVersion.Pdf17,
        _ => PdfVersion.Pdf20,
    };

    private static string RuleText(string id)
    {
        using JsonDocument description = JsonDocument.Parse(File.ReadAllText(DescriptionPath()));
        foreach (JsonElement rule in description.RootElement.GetProperty("settingsRules").GetProperty("rules").EnumerateArray())
            if (rule.GetProperty("id").GetString() == id) return rule.GetProperty("text").GetString()!;
        throw new InvalidOperationException(id);
    }

    private static string DescriptionPath() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "BonniePDF", "Definition", "ElementDescription.json"));

    /// <summary>The same settings applied through the code API, which never validates a definition.</summary>
    private static PdfDocument DocumentFrom(JsonElement settings)
    {
        var doc = new PdfDocument();
        if (settings.TryGetProperty("pdfVersion", out JsonElement v)) doc.Settings.Version = Version(v.GetString()!);
        if (settings.TryGetProperty("conformance", out JsonElement c))
            doc.Settings.Conformance = c.GetString() switch
            {
                "pdf/a-1b" => PdfAConformance.PdfA1b,
                "pdf/a-1a" => PdfAConformance.PdfA1a,
                "pdf/a-2b" => PdfAConformance.PdfA2b,
                "pdf/a-2a" => PdfAConformance.PdfA2a,
                "pdf/a-3b" => PdfAConformance.PdfA3b,
                "pdf/a-3a" => PdfAConformance.PdfA3a,
                "pdf/a-4" => PdfAConformance.PdfA4,
                _ => PdfAConformance.None,
            };
        if (settings.TryGetProperty("accessibility", out JsonElement a) && a.GetString() == "pdf/ua-1")
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        if (settings.TryGetProperty("encryption", out JsonElement e))
        {
            doc.Settings.Encryption = new EncryptionSettings
            {
                Algorithm = e.GetProperty("algorithm").GetString() switch
                {
                    "rc4-40" => EncryptionAlgorithm.RC4_40,
                    "rc4-128" => EncryptionAlgorithm.RC4_128,
                    _ => EncryptionAlgorithm.AES_256,
                },
                OwnerPassword = "owner-secret",
                UserPassword = "",
            };
        }
        if (settings.TryGetProperty("bookmarks", out JsonElement b))
            foreach (JsonElement item in b.EnumerateArray())
                doc.Settings.Bookmarks.Add(new BookmarkItem { Title = item.GetProperty("title").GetString()!, PageIndex = item.GetProperty("pageIndex").GetInt32() });
        doc.Add(new TextBlock("Hello"));
        return doc;
    }

    [Fact]
    public void EveryErrorRuleHasAFixture()
    {
        var named = new HashSet<string>();
        foreach (string name in DefinitionFixtures.Names("settings-rules"))
        {
            using JsonDocument f = DefinitionFixtures.Parse("settings-rules", name);
            if (f.RootElement.GetProperty("_expect").TryGetProperty("rule", out JsonElement rule)) named.Add(rule.GetString()!);
        }
        using JsonDocument description = JsonDocument.Parse(File.ReadAllText(DescriptionPath()));
        foreach (JsonElement rule in description.RootElement.GetProperty("settingsRules").GetProperty("rules").EnumerateArray())
            if (rule.GetProperty("outcome").GetString() == "error")
                Assert.Contains(rule.GetProperty("id").GetString()!, named);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task RulesAgreeWithGeneration(string name)
    {
        using JsonDocument fixture = DefinitionFixtures.Parse("settings-rules", name);
        JsonElement expect = fixture.RootElement.GetProperty("_expect");
        JsonElement settings = fixture.RootElement.GetProperty("settings");

        DefinitionValidationResult result = DocumentDefinition.Validate(fixture.RootElement);
        if (!expect.GetProperty("valid").GetBoolean())
        {
            Assert.Equal(["/settings incompatible-settings"], result.Errors.Select(e => $"{e.Path} {e.Code}").ToList());
            Assert.Equal(RuleText(expect.GetProperty("rule").GetString()!), result.Errors[0].Message);

            using PdfDocument doc = DocumentFrom(settings);
            await Assert.ThrowsAsync<InvalidOperationException>(() => doc.GenerateBytesAsync());
        }
        else
        {
            Assert.Empty(result.Errors);
            using PdfDocument doc = await PdfDocument.FromDefinitionAsync(fixture.RootElement, new DefinitionReadOptions
            {
                Passwords = new DefinitionPasswords { Owner = "owner-secret" },
            });
            byte[] bytes = await doc.GenerateBytesAsync();
            string expected = expect.GetProperty("pdfVersion").GetString() == "pdf-2.0" ? "%PDF-2.0" : "%PDF-1.7";
            Assert.Equal(expected, Encoding.ASCII.GetString(bytes, 0, 8));
            Assert.Equal(Version(expect.GetProperty("pdfVersion").GetString()!), doc.Settings.Version);
        }
    }
}
