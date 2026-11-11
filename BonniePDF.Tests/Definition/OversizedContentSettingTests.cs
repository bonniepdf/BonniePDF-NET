using Xunit;

namespace BonniePDF.Tests.Definition;

/// <summary>The oversizedContent setting in a document definition. Mirrors core/tests/definition/oversized-content-setting.test.ts.</summary>
public class OversizedContentSettingTests
{
    private static string Definition(string settings) =>
        $$"""{"version":1,"minorVersion":0,"settings":{{settings}},"pages":[{"content":[]}]}""";

    [Fact]
    public async Task ReadsAndWritesTheThrowToken()
    {
        using BonniePDF.Document doc = await BonniePDF.Document.FromDefinitionAsync(Definition("""{"oversizedContent":"throw"}"""));
        Assert.Equal(OversizedContent.Throw, doc.Settings.OversizedContent);
        Assert.Equal("""{"oversizedContent":"throw"}""", doc.ToDefinition().Definition["settings"]!.ToJsonString());
    }

    [Fact]
    public void LeavesTheDefaultOutOfTheWrittenDefinition()
    {
        using BonniePDF.Document doc = new();
        Assert.Null(doc.ToDefinition().Definition["settings"]?["oversizedContent"]);
    }

    [Fact]
    public async Task RejectsAnUnknownToken() =>
        await Assert.ThrowsAnyAsync<Exception>(() => BonniePDF.Document.FromDefinitionAsync(Definition("""{"oversizedContent":"shrink"}""")));
}
