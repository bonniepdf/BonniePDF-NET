using System.Text.RegularExpressions;
using BonniePDF.Drawing;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Export;

/// <summary>
/// The SVG preview must paint each code point with the same face the PDF painter
/// picks. NotoSansSymbols2 deliberately omits code points whose default
/// presentation is emoji (Noto delegates those to NotoEmoji), so this pair
/// reproduces the real emojis-and-unicode example: symbols the primary font
/// cannot draw, with a registered font that can.
///
/// Without run resolution the uncovered characters were emitted anyway, tagged
/// with a face lacking the glyph, and the viewer silently substituted a system
/// font - painting glyphs absent from the PDF at widths layout never reserved.
/// </summary>
[Collection("Serial")]
public partial class SvgGlyphFallbackTests
{
    private const string Star = "★";      // covered by NotoSansSymbols2
    private const string Church = "⛪";    // NotoEmoji only
    private const string FuelPump = "⛽";  // NotoEmoji only
    private const string RaisedFist = "✊";// NotoEmoji only

    private static string FontDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "Examples", "assets", "fonts");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate Examples/assets/fonts from " + AppContext.BaseDirectory);
    }

    private static string SymbolsPath => Path.Combine(FontDir(), "NotoSansSymbols2-Regular.ttf");

    private static string EmojiPath => Path.Combine(FontDir(), "NotoEmoji-Regular.ttf");

    private static void RegisterSymbolsOnly()
    {
        FontManager.Clear();
        FontManager.RegisterFont(File.ReadAllBytes(SymbolsPath), "Noto Sans Symbols 2", "Regular");
    }

    private static void RegisterSymbolsAndEmoji()
    {
        RegisterSymbolsOnly();
        FontManager.RegisterFont(File.ReadAllBytes(EmojiPath), "Noto Emoji", "Regular");
    }

    private static BonniePDF.Document SymbolsDoc(string text)
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock { Text = text, FontName = SymbolsPath, FontSize = 20 });
        return doc;
    }

    [GeneratedRegex(@"<tspan x=""([\-\d.]+)""")]
    private static partial Regex TspanXRegex();

    [GeneratedRegex("font-family:'(bpf-[^']+)'")]
    private static partial Regex FontFaceFamilyRegex();

    private static IEnumerable<float> TspanXs(string svg) =>
        TspanXRegex().Matches(svg)
            .Select(m => float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public void UncoveredCodePoints_AreRoutedToAFallbackFace()
    {
        RegisterSymbolsAndEmoji();
        try
        {
            SvgDocumentExport export = SvgPageExporter.ExportDocument(
                SymbolsDoc(Star + Church + FuelPump + RaisedFist), null);

            string primary = SvgCanvas.EmbeddedFontFamily(SymbolsPath);
            string[] faces = FontFaceFamilyRegex().Matches(export.FontFaceCss)
                .Select(m => m.Groups[1].Value).Distinct().ToArray();

            // Two faces embedded: the primary, plus the fallback covering the rest.
            Assert.Contains(primary, faces);
            Assert.Equal(2, faces.Length);

            string fallback = faces.Single(f => f != primary);
            Assert.Contains(primary, export.Pages[0], StringComparison.Ordinal);
            Assert.Contains(fallback, export.Pages[0], StringComparison.Ordinal);
        }
        finally
        {
            FontManager.Clear();
        }
    }

    [Fact]
    public void UnrenderableCodePoints_AreDropped_NotLeftForViewerSubstitution()
    {
        // With no fallback registered these cannot be drawn at all. They must be
        // dropped, exactly as the PDF encoder drops them rather than referencing
        // .notdef - not emitted against a face that lacks them.
        RegisterSymbolsOnly();
        try
        {
            IReadOnlyList<string> pages = SymbolsDoc(Star + Church + FuelPump + RaisedFist).GenerateSvgPages();

            Assert.DoesNotContain(Church, pages[0], StringComparison.Ordinal);
            Assert.DoesNotContain(FuelPump, pages[0], StringComparison.Ordinal);
            Assert.DoesNotContain(RaisedFist, pages[0], StringComparison.Ordinal);
            Assert.Contains(Star, pages[0], StringComparison.Ordinal);
        }
        finally
        {
            FontManager.Clear();
        }
    }

    [Fact]
    public void FallbackRun_ContinuesTheSharedAdvanceCursor()
    {
        RegisterSymbolsAndEmoji();
        try
        {
            IReadOnlyList<string> pages = SymbolsDoc(Star + Church).GenerateSvgPages();
            float[] xs = TspanXs(pages[0]).ToArray();

            Assert.Equal(2, xs.Length);
            // Strictly increasing: the fallback run continues where the primary
            // stopped, rather than restarting at the line origin.
            Assert.True(xs[1] > xs[0], $"expected {xs[1]} > {xs[0]}");
        }
        finally
        {
            FontManager.Clear();
        }
    }

    [Fact]
    public void FallbackRuns_AreMeasuredAgainstTheFaceThatDrawsThem()
    {
        RegisterSymbolsAndEmoji();
        try
        {
            // Right-aligned text is positioned from its measured width, so a wrong
            // measurement shifts the whole line. Every glyph must contribute.
            float wide = TspanXs(SymbolsDoc(Church + FuelPump + RaisedFist).GenerateSvgPages()[0]).Max();
            float narrow = TspanXs(SymbolsDoc(Church).GenerateSvgPages()[0]).Max();

            Assert.True(wide > narrow, $"expected {wide} > {narrow}");
        }
        finally
        {
            FontManager.Clear();
        }
    }
}
