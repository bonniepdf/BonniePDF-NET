using System.Text;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Standard (unembedded) fonts use WinAnsiEncoding: typographic punctuation has its own byte,
/// not an encoder fallback.
/// </summary>
[Collection("Serial")]
public sealed class StandardFontEncodingTests
{
    private static string ContentOf(string text)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using BonniePDF.Document doc = new();
            doc.Add(new TextBlock(text));
            return Encoding.Latin1.GetString(doc.GenerateBytes());
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void TypographicPunctuation_MapsToWindows1252Byte() =>
        Assert.Contains("(It\u0092s \u0093quoted\u0094 \u0096 \u00805\u0085)",
            ContentOf("It’s “quoted” – €5…"), System.StringComparison.Ordinal);

    [Fact]
    public void Latin1Characters_AreWrittenUnchanged() =>
        Assert.Contains("(£5 café)", ContentOf("£5 café"), System.StringComparison.Ordinal);

    [Fact]
    public void CharacterOutsideWinAnsi_IsWrittenAsQuestionMark() =>
        Assert.Contains("(a?b?c)", ContentOf("a日b\U0001F600c"), System.StringComparison.Ordinal);

    private static float Width(string text, string font) => FontMetrics.GetStringWidth(text, font, 1000);

    [Fact]
    public void LatinAndWindows1252Characters_AreMeasuredFromTheFontTables()
    {
        Assert.Equal(556f, Width("\u00a3", "Helvetica"));
        Assert.Equal(556f, Width("\u00e9", "Helvetica"));
        Assert.Equal(222f, Width("\u2019", "Helvetica"));
        Assert.Equal(1000f, Width("\u2014", "Helvetica"));
        Assert.Equal(333f, Width("\u2019", "Times-Roman"));
        Assert.Equal(444f, Width("\u00e9", "Times-Bold"));
    }

    [Fact]
    public void CharacterWrittenAsQuestionMark_IsMeasuredAsQuestionMark()
    {
        Assert.Equal(Width("?", "Helvetica"), Width("\u65e5", "Helvetica"));
        Assert.Equal(Width("?", "Times-Roman"), Width("\U0001F600", "Times-Roman"));
    }

    [Fact]
    public void AsciiMeasurement_IsUnchanged() =>
        Assert.Equal(722f + 556 + 278 + 278 + 611, Width("Hello", "Helvetica-Bold"));

    [Fact]
    public void Courier_IsMeasuredPerCharacter() =>
        Assert.Equal(1200f, Width("a\U0001F600", "Courier"));
}
