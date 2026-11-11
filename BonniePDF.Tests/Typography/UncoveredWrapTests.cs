using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Typography;

/// <summary>
/// Spec 19, Layout: a wrapping paragraph whose primary face lacks a character that fallback draws
/// (Noto Sans Hebrew has no full stop or digits) lays out instead of failing in the line breaker.
/// </summary>
[Collection("Serial")]
public sealed partial class UncoveredWrapTests
{
    private const string Paragraph =
        "תודה על הזמנתכם. ההזמנה אושרה ותגיע לכתובתכם בתוך 5 ימי עסקים. נא לעיין בפרטי ההזמנה שלהלן ולשמור מכתב זה.";

    [Theory]
    [InlineData(ContentDirection.LeftToRight)]
    [InlineData(ContentDirection.RightToLeft)]
    public void WrappingParagraph_GeneratesAndWraps(ContentDirection direction)
    {
        using BonniePDF.Document doc = new();
        var face = doc.Fonts.Add(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Typography", "Fixtures", "NotoSansHebrew-Regular.ttf")));
        doc.Add(new TextBlock(Paragraph) { FontName = face.FontName, FontSize = 14, ContentDirection = direction });
        string svg = doc.GenerateSvgPages()[0];
        Assert.NotEmpty(doc.GenerateBytes());
        // More than one line, so the line breaker ran.
        Assert.True(SvgTextElement().Count(svg) > 1);
    }

    [GeneratedRegex("<text\\b")]
    private static partial Regex SvgTextElement();
}
