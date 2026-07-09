using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Elements.Shapes;
using BonniePDF.Rendering.Tagging;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Tests for the native Data Matrix (ECC200) encoder and element. Mirrors the
/// TypeScript <c>datamatrix.test.ts</c> suite: golden symbols verified against a
/// real reader, structural invariants, and element/tagging behaviour.
/// </summary>
public class DataMatrixTests
{
    private static string[] ToRows(string message, bool rectangular = false)
    {
        EncodedDataMatrix m = DataMatrixEncoder.Encode(message, rectangular);
        var rows = new string[m.Height];
        for (int y = 0; y < m.Height; y++)
        {
            var sb = new StringBuilder(m.Width);
            for (int x = 0; x < m.Width; x++) sb.Append(m.Modules[y, x] ? '#' : '.');
            rows[y] = sb.ToString();
        }
        return rows;
    }

    // ── Golden matrices ───────────────────────────────────────────────────────
    // Byte-for-byte identical to the upstream datalog/datamatrix-svg reference
    // and to the TypeScript port, and every one decodes back to its source text
    // with zxing-wasm (a real Data Matrix reader). Any drift in encodation,
    // Reed-Solomon, or module placement breaks them.

    [Fact]
    public void Encodes_Numeric_1234567890()
    {
        string[] expected =
        {
            "#.#.#.#.#.#.",
            "##..#..#.#.#",
            "##..##......",
            "##...###.###",
            "##..#...##..",
            "#.#......#.#",
            "##.#.###....",
            "###.##.##..#",
            "#####..#..#.",
            "##.###.###.#",
            "######.#..#.",
            "############",
        };
        Assert.Equal(expected, ToRows("1234567890"));
    }

    [Fact]
    public void Encodes_Text_HelloWorld()
    {
        string[] expected =
        {
            "#.#.#.#.#.#.#.#.",
            "##..#.#....#####",
            "#...#.....#..#..",
            "###.#..#.#.#.#.#",
            "#.###...#..#..#.",
            "#.#..#..##...###",
            "#.#.#......###..",
            "#..##..#...#.###",
            "#..###..#######.",
            "#####.##...###.#",
            "##.#.#.#...##...",
            "#..###...##.#..#",
            "##.##.......#.#.",
            "#.#.....####.#.#",
            "#..###.##.##..#.",
            "################",
        };
        Assert.Equal(expected, ToRows("HELLO WORLD"));
    }

    [Fact]
    public void Encodes_Unicode_Cafe_ViaBase256()
    {
        string[] expected =
        {
            "#.#.#.#.#.#.#.",
            "#.#####.###..#",
            "##...#.##.#...",
            "#.#....#..#..#",
            "#.###..#.#.##.",
            "#.#.#.#.##.###",
            "#..###..#.....",
            "#.##.#.#.#..##",
            "##..##.###.#..",
            "#...#.####.#.#",
            "#.#.###..###..",
            "#.####..#...##",
            "##.....#..##..",
            "##############",
        };
        Assert.Equal(expected, ToRows("café"));
    }

    [Fact]
    public void Encodes_Rectangular_12345()
    {
        string[] expected =
        {
            "#.#.#.#.#.#.#.#.#.",
            "##.##........##.##",
            "##...#..#..#..##..",
            "##..##.#..#.#..#.#",
            "###..##.##....#...",
            "#.##....#.##.#.###",
            "#..#.##.#.##...#..",
            "##################",
        };
        Assert.Equal(expected, ToRows("12345", rectangular: true));
    }

    // ── Structural invariants ─────────────────────────────────────────────────

    [Fact]
    public void SquareSymbol_HasValidEcc200Size()
    {
        EncodedDataMatrix m = DataMatrixEncoder.Encode("Wikipedia");
        Assert.Equal(m.Height, m.Width);
        int[] valid = { 10, 12, 14, 16, 18, 20, 22, 24, 26, 32, 36, 40, 44, 48, 52, 64, 72, 80, 88, 104, 120, 132, 144 };
        Assert.Contains(m.Width, valid);
    }

    [Fact]
    public void RectangularSymbol_IsWiderThanTall()
    {
        EncodedDataMatrix m = DataMatrixEncoder.Encode("12345", rectangular: true);
        Assert.True(m.Width > m.Height);
        var valid = new[] { (18, 8), (32, 8), (26, 12), (36, 12), (36, 16), (48, 16) };
        Assert.Contains((m.Width, m.Height), valid);
    }

    [Fact]
    public void FinderPattern_LeftColumnAndBottomRowAreSolid()
    {
        EncodedDataMatrix m = DataMatrixEncoder.Encode("FINDER PATTERN TEST");
        for (int y = 0; y < m.Height; y++) Assert.True(m.Modules[y, 0]); // solid left edge
        for (int x = 0; x < m.Width; x++) Assert.True(m.Modules[m.Height - 1, x]); // solid bottom edge
    }

    [Fact]
    public void Symbol_GrowsWithMessageLength()
    {
        EncodedDataMatrix small = DataMatrixEncoder.Encode("A");
        EncodedDataMatrix large = DataMatrixEncoder.Encode(new string('A', 120));
        Assert.True(large.Width > small.Width);
    }

    [Fact]
    public void UnicodePayload_EncodesWithoutThrowing()
    {
        var ex = Record.Exception(() => DataMatrixEncoder.Encode("日本語のテキスト"));
        Assert.Null(ex);
    }

    [Fact]
    public void MessageTooLong_Throws()
    {
        Assert.Throws<DataMatrixContentException>(() => DataMatrixEncoder.Encode(new string('9', 4000)));
    }

    // ── Element behaviour ─────────────────────────────────────────────────────

    [Fact]
    public void Document_DataMatrixRendersValidPdf()
    {
        using var doc = new BonniePDF.Document();
        doc.Add(new DataMatrix { Data = "https://www.bonniepdf.dev" });
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());
        Assert.StartsWith("%PDF-", pdf);
        Assert.Contains("%%EOF", pdf);
    }

    [Fact]
    public void SquareSymbol_MeasuresAsTheConfiguredSize()
    {
        var dm = new DataMatrix { Data = "SQUARE", Size = 120f };
        Assert.True(Math.Abs(dm.MeasureHeight(500f) - 120f) < 0.01f);
    }

    [Fact]
    public void RectangularSymbol_MeasuresShorterThanWide()
    {
        var dm = new DataMatrix { Data = "12345", Rectangular = true, Size = 180f };
        Assert.True(dm.MeasureHeight(500f) < 180f);
    }

    [Fact]
    public void DataMatrix_WithAltText_RoleIsFigure()
    {
        var dm = new DataMatrix { Data = "X", AltText = "Data Matrix linking to the order" };
        Assert.Equal(SemanticRole.Figure, dm.SemanticRole);
    }

    [Fact]
    public void DataMatrix_WithoutAltText_RoleIsArtifact()
    {
        var dm = new DataMatrix { Data = "X" };
        Assert.Equal(SemanticRole.Artifact, dm.SemanticRole);
    }

    [Fact]
    public void TaggedDocument_DataMatrixWithAltText_EmitsFigureWithAlt()
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Add(new DataMatrix
        {
            Data = "ORDER-7782",
            AltText = "Data Matrix for order ORDER-7782",
        });
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.Contains("/S /Figure", pdf);
        Assert.Contains("/Alt (Data Matrix for order ORDER-7782)", pdf);
    }
}
