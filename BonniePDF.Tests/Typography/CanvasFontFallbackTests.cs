using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Tests.Support;
using Xunit;

namespace BonniePDF.Tests.Typography;

// Glyph fallback applies to all text: text drawn on a positioned canvas, as charts
// and canvas elements draw theirs, takes the same fallback font as body text. U+2641 (the earth
// symbol) is in Liberation Sans but not in Liberation Mono, the primary face here. The
// TypeScript suite asserts the same.
public sealed partial class CanvasFontFallbackTests
{
    private const string Sample = "Ab ♁ z";

    private static string[] EmbeddedFonts(Action<BonniePDF.Document, string> draw)
    {
        var document = new BonniePDF.Document();
        string primary = document.Fonts.Add(File.ReadAllBytes(TestFonts.MonoRegularPath)).FontName;
        document.Fonts.Add(File.ReadAllBytes(TestFonts.SansRegularPath));
        draw(document, primary);
        string pdf = Encoding.Latin1.GetString(document.GenerateBytes());
        return BaseFont().Matches(pdf).Select(m => m.Groups[1].Value).Distinct().Order(StringComparer.Ordinal).ToArray();
    }

    [GeneratedRegex(@"/BaseFont /[A-Z]{6}\+([^\s/]+)")]
    private static partial Regex BaseFont();

    [Fact]
    public void ACanvasUsesTheFallbackFontAsBodyTextDoes()
    {
        string[] body = EmbeddedFonts((document, primary) => document.Add(new TextBlock(Sample) { FontName = primary, FontSize = 12 }));
        string[] canvas = EmbeddedFonts((document, primary) => document.Add(new CanvasElement((c, _, _) =>
            c.DrawText(10, 10, Sample, Color.Black, 12, primary, TextAlignment.Left)) { DrawHeight = 40 }));
        Assert.Equal(new[] { "LiberationMono", "LiberationSans" }, body);
        Assert.Equal(body, canvas);
    }
}
