using System.IO.Compression;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Tests.Support;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests.Typography;

[Collection("Serial")]
public class FontMetricScopeTests
{
    private const string ProbeText = "Revenue by segment";

    [Fact]
    public void PdfUaLayout_IsIdenticalAcrossSequentialDocuments()
    {
        string first = BuildProbe().GenerateFirstPageSvg();
        string second = BuildProbe().GenerateFirstPageSvg();

        Assert.Equal(first, second);
        Assert.Equal(2, Count(first, "<text "));
    }

    [Fact]
    public async Task PdfUaLayout_IsIdenticalAcrossSequentialAsyncDocuments()
    {
        byte[] first = await BuildProbe().GenerateBytesAsync();
        byte[] second = await BuildProbe().GenerateBytesAsync();

        Assert.Equal(TextShowCount(first), TextShowCount(second));
        Assert.Equal(2, TextShowCount(first));
    }

    [Fact]
    public void PdfALayout_IsIdenticalAcrossSequentialDocuments()
    {
        byte[] first = BuildProbe(PdfAConformance.PdfA2b).GenerateBytes();
        byte[] second = BuildProbe(PdfAConformance.PdfA2b).GenerateBytes();

        Assert.Equal(TextShowCount(first), TextShowCount(second));
        Assert.Equal(2, TextShowCount(first));
    }

    [Fact]
    public async Task DocumentScopes_IsolateAliasesAcrossConcurrentAsyncFlows()
    {
        TtfFont sans = new(TestFonts.SansRegularPath);
        TtfFont serif = new(TestFonts.SerifRegularPath);
        using Barrier ready = new(2);

        Task<float> sansWidth = Task.Run(() => MeasureInScope(sans, ready));
        Task<float> serifWidth = Task.Run(() => MeasureInScope(serif, ready));
        await Task.WhenAll(sansWidth, serifWidth);

        Assert.Equal(FontMetrics.GetKernedStringWidth(ProbeText, sans, 11), sansWidth.Result, 3);
        Assert.Equal(FontMetrics.GetKernedStringWidth(ProbeText, serif, 11), serifWidth.Result, 3);
        Assert.NotEqual(sansWidth.Result, serifWidth.Result);
    }

    [Fact]
    public void FileAlias_IsUsedInsideDocumentScope()
    {
        TtfFont serif = new(TestFonts.SerifRegularPath);
        float expected = FontMetrics.GetKernedStringWidth(ProbeText, serif, 11);

        using (FontMetrics.BeginDocumentScope(embedStandardFonts: false))
        {
            FontMetrics.RegisterFontAlias("ScopedAlias", TestFonts.SerifRegularPath);
            Assert.Equal(expected, FontMetrics.GetStringWidth(ProbeText, "ScopedAlias", 11), 3);
        }
    }

    [Fact]
    public void ReplacingRegisteredFont_InvalidatesParsedMetrics()
    {
        byte[] serifBytes = File.ReadAllBytes(TestFonts.SerifRegularPath);
        byte[] sansBytes = File.ReadAllBytes(TestFonts.SansRegularPath);
        TtfFont serif = TtfFont.LoadFromBytes(serifBytes);
        TtfFont sans = TtfFont.LoadFromBytes(sansBytes);

        try
        {
            FontManager.RegisterFont(serifBytes, "Liberation Sans", "Regular");
            Assert.Equal(
                FontMetrics.GetKernedStringWidth(ProbeText, serif, 11),
                FontMetrics.GetKernedStringWidth(ProbeText, FontManager.GetParsedStandardFont(StandardFonts.Helvetica)!, 11),
                3);

            FontManager.RegisterFont(sansBytes, "Liberation Sans", "Regular");
            Assert.Equal(
                FontMetrics.GetKernedStringWidth(ProbeText, sans, 11),
                FontMetrics.GetKernedStringWidth(ProbeText, FontManager.GetParsedStandardFont(StandardFonts.Helvetica)!, 11),
                3);
        }
        finally
        {
            FontManager.RestoreBundledFonts();
        }
    }

    private static float MeasureInScope(TtfFont font, Barrier ready)
    {
        using (FontMetrics.BeginDocumentScope(embedStandardFonts: false))
        {
            FontMetrics.RegisterFontInstance("ScopedAlias", font);
            ready.SignalAndWait();
            return FontMetrics.GetStringWidth(ProbeText, "ScopedAlias", 11);
        }
    }

    private static BonniePDF.Document BuildProbe(PdfAConformance conformance = PdfAConformance.None)
    {
        BonniePDF.Document doc = new(new DocumentSettings(PdfSize.A4, 20)
        {
            Accessibility = conformance == PdfAConformance.None
                ? AccessibilityMode.PdfUa1
                : AccessibilityMode.None,
            Conformance = conformance,
            Language = "en-GB",
        });
        doc.Add(new Container
        {
            Width = 104.945f,
            Border = new Border(0.5f),
            Child = new TextBlock(ProbeText)
            {
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
            },
        });
        return doc;
    }

    private static int TextShowCount(byte[] bytes)
    {
        string pdf = Encoding.Latin1.GetString(bytes);
        int total = 0;
        int index = 0;
        while (true)
        {
            int start = pdf.IndexOf("stream", index, StringComparison.Ordinal);
            if (start < 0) break;
            int contentStart = start + "stream".Length;
            if (contentStart < pdf.Length && pdf[contentStart] == '\r') contentStart++;
            if (contentStart < pdf.Length && pdf[contentStart] == '\n') contentStart++;
            int end = pdf.IndexOf("endstream", contentStart, StringComparison.Ordinal);
            if (end < 0) break;

            byte[] raw = Encoding.Latin1.GetBytes(pdf.Substring(contentStart, end - contentStart));
            string stream = Decompress(raw);
            total += Count(stream, " Tj") + Count(stream, " TJ");
            index = end + "endstream".Length;
        }
        return total;
    }

    private static string Decompress(byte[] raw)
    {
        try
        {
            using MemoryStream input = new(raw);
            using ZLibStream zlib = new(input, CompressionMode.Decompress);
            using StreamReader reader = new(zlib, Encoding.Latin1);
            return reader.ReadToEnd();
        }
        catch (InvalidDataException)
        {
            return Encoding.Latin1.GetString(raw);
        }
    }

    private static int Count(string value, string token)
    {
        int count = 0;
        int index = 0;
        while ((index = value.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }
}
