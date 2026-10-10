using System.Globalization;
using System.Text.RegularExpressions;
using BonniePDF.Tests.Support;
using Xunit;

namespace BonniePDF.Tests.Export;

/// <summary>
/// Every embedded-font text path must paint explicitly positioned glyphs, so the
/// preview places each glyph where the layout engine (and the PDF painter) put it
/// rather than leaving the spacing to whatever the viewer decides.
///
/// The rotated and justified paths previously emitted a plain run and let the
/// viewer lay it out, and the straight path wrapped every glyph in its own
/// &lt;tspan&gt;. Nothing caught either, because no test looked at these paths -
/// hence this file. The TypeScript implementation emits the same shape; these are
/// the invariants both are held to.
/// </summary>
public sealed partial class SvgGlyphPositioningTests
{
    [GeneratedRegex(@"<text x=""([^""]+)""")]
    private static partial Regex TextXListRegex();

    [GeneratedRegex(@"rotate\(([\-\d.]+)")]
    private static partial Regex RotateRegex();

    /// <summary>The per-glyph x list of the first glyph-positioned run on the page.</summary>
    private static float[] FirstGlyphRun(string svg)
    {
        Match m = TextXListRegex().Match(svg);
        Assert.True(m.Success, "expected at least one glyph-positioned <text> element");
        return m.Groups[1].Value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(v => float.Parse(v, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static BonniePDF.Document EmbeddedDoc(string text, TextAlignment align = TextAlignment.Left)
    {
        var doc = new BonniePDF.Document();
        doc.Add(new TextBlock
        {
            Text = text,
            FontName = TestFonts.SansRegularPath,
            FontSize = 12,
            Alignment = align,
        });
        return doc;
    }

    [Fact]
    public void EmbeddedText_CarriesOneXPerGlyph_OnTheTextElement()
    {
        using BonniePDF.Document doc = EmbeddedDoc("Positioned");
        string svg = doc.GenerateSvgPages()[0];

        // Positions belong on the <text> element's x list, not on a <tspan> per
        // glyph: same geometry, a fraction of the markup.
        Assert.DoesNotContain("<tspan", svg, StringComparison.Ordinal);
        Assert.Equal("Positioned".Length, FirstGlyphRun(svg).Length);
    }

    [Fact]
    public void EmbeddedGlyphOrigins_AreStrictlyIncreasing()
    {
        using BonniePDF.Document doc = EmbeddedDoc("Advance");
        float[] xs = FirstGlyphRun(doc.GenerateSvgPages()[0]);

        for (int i = 1; i < xs.Length; i++)
            Assert.True(xs[i] > xs[i - 1], $"glyph {i} at {xs[i]} did not advance past {xs[i - 1]}");
    }

    [Fact]
    public void RotatedEmbeddedText_IsGlyphPositioned()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Watermark = new Watermark
        {
            Text = "DRAFT",
            FontName = TestFonts.SansRegularPath,
            FontSize = 48,
            RotationDegrees = -45,
        };
        doc.Add(new TextBlock { Text = "Body", FontName = TestFonts.SansRegularPath });

        using (doc)
        {
            string svg = doc.GenerateSvgPages()[0];
            Assert.Contains("rotate(", svg, StringComparison.Ordinal);
            // The rotated run is positioned like any other: a watermark left to the
            // viewer's own layout drifts away from the PDF it previews.
            Assert.DoesNotContain("<tspan", svg, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void RotationIsNegated_SoSvgTurnsTheSameWayAsThePdf()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.Watermark = new Watermark
        {
            Text = "DRAFT",
            FontName = TestFonts.SansRegularPath,
            FontSize = 48,
            RotationDegrees = -45,
        };
        doc.Add(new TextBlock { Text = "Body", FontName = TestFonts.SansRegularPath });

        using (doc)
        {
            Match m = RotateRegex().Match(doc.GenerateSvgPages()[0]);
            Assert.True(m.Success, "expected a rotate() transform");

            // The PDF painter rotates counter-clockwise for a positive angle in
            // PDF's y-up space; SVG's y-down space turns that same sign into a
            // clockwise turn. The exporter negates so both turn the same way -
            // without it the preview mirrors the document it previews.
            Assert.Equal(45f, float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture), 3);
        }
    }

    [Fact]
    public void JustifiedEmbeddedText_WidensSpaceAdvances_NotJustWordSpacingAttribute()
    {
        // Long enough to wrap: only a non-final line is justified, so a
        // single-line string would show no difference at all.
        const string Text =
            "one two three four five six seven eight nine ten eleven twelve thirteen fourteen "
            + "fifteen sixteen seventeen eighteen nineteen twenty twenty-one twenty-two";

        using BonniePDF.Document narrow = EmbeddedDoc(Text);
        using BonniePDF.Document justified = EmbeddedDoc(Text, TextAlignment.Justify);

        float[] plain = FirstGlyphRun(narrow.GenerateSvgPages()[0]);
        float[] spread = FirstGlyphRun(justified.GenerateSvgPages()[0]);

        // Justification is baked into the glyph origins. Leaving it to a
        // word-spacing attribute would put the glyphs somewhere the PDF does not.
        Assert.Equal(plain.Length, spread.Length);
        Assert.True(spread[^1] > plain[^1],
            $"justified run should end further right ({spread[^1]}) than the ragged one ({plain[^1]})");
    }

    [Fact]
    public void EmbeddedText_BindsTheFamilyInAStyleAttribute_WithAGenericFallback()
    {
        using BonniePDF.Document doc = EmbeddedDoc("Styled");
        string svg = doc.GenerateSvgPages()[0];

        // A style attribute outranks a host stylesheet's `* { font-family: inherit }`
        // reset, which a presentation attribute would lose to; the generic keeps text
        // broadly right if the @font-face never loads.
        Assert.Contains("style=\"font-family:'bpf-", svg, StringComparison.Ordinal);
        Assert.Matches(@"style=""font-family:'bpf-[^']+',(serif|sans-serif|monospace);font-kerning:none;font-variant-ligatures:none""", svg);
    }
}
