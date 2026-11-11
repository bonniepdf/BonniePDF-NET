using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Content;

/// <summary>Spec 03, RichText shared baseline: spans on a line sit on the baseline of its largest font.</summary>
[Collection("Serial")]
public sealed partial class RichTextBaselineTests
{
    private static Dictionary<string, float> Baselines()
    {
        using PdfDocument doc = new();
        doc.Add(new RichText()
            .Span("Water", new TextStyle { FontSize = 14 })
            .Span("small", new TextStyle { FontSize = 8 })
            .Span("sub", new TextStyle { FontSize = 8, BaselineAlignment = BaselineAlignment.Subscript })
            .Span("sup", new TextStyle { FontSize = 8, BaselineAlignment = BaselineAlignment.Superscript }));
        var ys = new Dictionary<string, float>();
        foreach (Match m in SvgText().Matches(doc.GenerateSvgPages()[0]))
            ys[m.Groups[2].Value.Trim()] = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        return ys;
    }

    [GeneratedRegex("<text[^>]*\\sy=\"([\\d.]+)\"[^>]*>([^<]*)</text>")]
    private static partial Regex SvgText();

    [Fact]
    public void SmallerSpan_SharesTheBaselineOfTheLargerText()
    {
        var ys = Baselines();
        Assert.Equal(ys["Water"], ys["small"], 2);
    }

    [Fact]
    public void Subscript_SitsBelowTheBaseline_AndSuperscriptAbove()
    {
        var ys = Baselines();
        Assert.True(ys["sub"] > ys["Water"]);
        Assert.True(ys["sup"] < ys["Water"]);
    }
}
