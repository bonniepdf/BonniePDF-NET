using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Api;

/// <summary>
/// The public surface: the assembly exposes the API and not its plumbing. <c>InternalSurface.json</c> lists the
/// types that are internal; none of them may be public.
///
/// Mirrors core/tests/api/surface.test.ts.
/// </summary>
public class PublicSurfaceTests
{
    private static string Base(string name) => name.Split('`')[0];

    [Fact]
    public void NoInternalTypeIsPublic()
    {
        using JsonDocument surface = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Api", "InternalSurface.json")));
        var internalNames = surface.RootElement.GetProperty("names").EnumerateArray().Select(n => n.GetString()!).ToHashSet(StringComparer.Ordinal);

        List<string> exposed = typeof(BonniePDF.Document).Assembly.GetExportedTypes()
            .Where(t => internalNames.Contains(Base(t.Name)))
            .Select(t => t.FullName!)
            .Order(StringComparer.Ordinal)
            .ToList();

        Assert.Empty(exposed);
    }

    [Fact]
    public void TheFrontDoorIsPublic()
    {
        var exported = typeof(BonniePDF.Document).Assembly.GetExportedTypes().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);
        foreach (string name in new[] { "Document", "TextBlock", "Table", "Container", "FontManager", "ImageLoader", "RenderContext", "Pdf" })
            Assert.Contains(name, exported);
    }
}
