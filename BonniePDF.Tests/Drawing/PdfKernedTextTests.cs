using System.Text;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Tests.Support;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Drawing;

[Collection("Serial")]

/// <summary>
/// Kerned PDF text rendering (<c>Tj</c> -> <c>TJ</c>).
///
/// Embedded fonts with kern pairs must paint with the <c>TJ</c> operator whose
/// array interleaves glyph hex runs with advance adjustments in thousandths of
/// an em, so the PDF ink matches the kerned widths the layout engine reserves
/// (and the kerned SVG export). Unkerned strings and standard-14 fonts
/// keep the plain <c>Tj</c> path, byte-identical to before.
///
/// Liberation Serif reference values (2048 UPEM), identical bytes to the
/// TypeScript port's asset so the two suites pin the same output:
///   A = gid 36 (0x0024), V = gid 57 (0x0039), kern(A,V) = -264 units.
///   TJ adjustment = round(264 * 1000 / 2048) = 129 (positive: TJ subtracts).
/// </summary>
public class PdfKernedTextTests
{
    private static readonly string SerifPath = TestFonts.SerifRegularPath;

    // ── PdfSyntax.ToKernedGlyphArray ────────────────────────────────

    [Fact]
    public void ToKernedGlyphArray_InterleavesNegatedEmScaledKern_BetweenKerningPair()
    {
        var font = new TtfFont(SerifPath);

        Assert.True(font.HasKerning);
        Assert.Equal("[<0024> 129 <0039>]", PdfSyntax.ToKernedGlyphArray("AV", font));
    }

    [Fact]
    public void ToKernedGlyphArray_GroupsUnkernedNeighbours_IntoSingleHexRun()
    {
        var font = new TtfFont(SerifPath);
        string Gid(char c) => font.GetGlyphId(c).ToString("X4");

        // "AVID": only A-V kerns; V-I and I-D do not, so the tail stays one run.
        Assert.Equal(0, font.GetKerning(font.GetGlyphId('V'), font.GetGlyphId('I')));
        Assert.Equal($"[<{Gid('A')}> 129 <{Gid('V')}{Gid('I')}{Gid('D')}>]",
            PdfSyntax.ToKernedGlyphArray("AVID", font));
    }

    [Fact]
    public void ToKernedGlyphArray_ReturnsNull_WhenNoPairKerns()
    {
        var font = new TtfFont(SerifPath);

        Assert.Null(PdfSyntax.ToKernedGlyphArray("ll", font));
        Assert.Null(PdfSyntax.ToKernedGlyphArray("", font));
        Assert.Null(PdfSyntax.ToKernedGlyphArray("A", font));
    }

    // ── End-to-end document output ──────────────────────────────────

    [Fact]
    public void EmbeddedFontKernPair_PaintsWithTjArray()
    {
        string pdf = Generate(doc =>
        {
            var t = new TextBlock("AV") { FontName = SerifPath, FontSize = 11 };
            doc.Add(t);
        });

        Assert.Contains("[<0024> 128.906 <0039>] TJ", pdf);
        Assert.DoesNotContain("<00240039> Tj", pdf);
    }

    [Fact]
    public void UnkernedEmbeddedFontString_StaysOnTjPath()
    {
        string pdf = Generate(doc =>
        {
            var t = new TextBlock("ll") { FontName = SerifPath };
            doc.Add(t);
        });

        Assert.DoesNotContain(" TJ", pdf);
        Assert.Matches("<[0-9A-F]+> Tj", pdf);
    }

    [Fact]
    public void Standard14Text_StaysOnLiteralStringTjPath()
    {
        string pdf = Generate(doc => doc.Add(new TextBlock("AVATAR Wave")));

        Assert.Contains("(AVATAR Wave) Tj", pdf);
        Assert.DoesNotContain(" TJ", pdf);
    }

    private static string Generate(System.Action<BonniePDF.Document> populate)
    {
        // Content streams are Flate-compressed by default; disable so the
        // Tj/TJ operators are directly assertable in the output bytes.
        InternalConfiguration.EnableCompression = false;
        try
        {
            using var doc = new BonniePDF.Document();
            populate(doc);
            using var ms = new MemoryStream();
            doc.Save(ms, true);
            return Encoding.ASCII.GetString(ms.ToArray());
        }
        finally
        {
            InternalConfiguration.EnableCompression = true;
        }
    }
}
