using System.Text.RegularExpressions;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// A Container whose HorizontalAlignment is Start/Center/End establishes that as the ambient horizontal
/// text alignment for descendant text that sets none of its own (spec 01 "Document Model" §3.2). This is
/// what makes the common idiom - `x.Item().AlignCenter().Text("...")` - centre a paragraph
/// that wraps, not only a single line short enough for block positioning to already have handled.
/// </summary>
public partial class AmbientTextAlignmentTests
{
    [GeneratedRegex(@"1 0 0 1 ([\d.]+) [\d.]+ Tm[\s\S]{0,80}?\((.*?)\) Tj")]
    private static partial Regex TextRun();

    private static readonly string LongTitle =
        "This is a long enough piece of text that it will not fit on a single line and must wrap across several lines within the page margins";

    private static string Render(Action<IContentContainer> content)
    {
        using BonniePDF.Document doc = Pdf.Create(pdf => pdf.Page(page =>
        {
            page.Margin(50);
            content(page.Content());
        })).Build();
        RenderContext ctx = new(doc.Settings.GetOrientedPageSize(), doc.Settings.Margin);
        return PdfRenderPipeline.RenderPagesStreaming(doc.Pages, doc.Settings, ctx).First().ContentStream;
    }

    // The horizontal midpoint of every drawn text run's box, from its Tm x and (line-count-agnostic)
    // trailing character run - close enough for these single-run test lines.
    private static List<(float X, string Text)> TextRuns(string stream) =>
        TextRun().Matches(stream).Select(m => (float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture), m.Groups[2].Value)).ToList();

    [Fact]
    public void AWrappedParagraph_CentresEveryLine_WhenTheEnclosingContainerCentres()
    {
        // The exact reported shape: AlignCenter() before Text(), on a title long enough to wrap.
        string stream = Render(root => root.Column(c => c.Item().AlignCenter().Text(LongTitle).FontSize(13)));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2, "the title should wrap to more than one line");

        // Every line's box is centred in the 495.276pt content width starting at x=50: none flush left.
        Assert.All(runs, r => Assert.True(r.X > 55, $"line \"{r.Text}\" starts at x={r.X}, which reads as left-aligned"));

        // Genuinely centred, not just indented: shorter lines start further right than longer ones.
        Assert.True(runs[^1].X > runs[0].X, "the shorter trailing line should be indented further than the first");
    }

    [Fact]
    public void ASingleShortLine_StillCentresAsBefore()
    {
        // The case that already worked: a one-line title narrower than the page, block-positioned.
        string stream = Render(root => root.Column(c => c.Item().AlignCenter().Text("Short Title").FontSize(13)));

        (float X, string Text) run = Assert.Single(TextRuns(stream));
        Assert.True(run.X > 200, $"a short centred title should be well clear of the left margin, got x={run.X}");
    }

    [Fact]
    public void AnExplicitAlignment_AlwaysWinsOverTheAmbient()
    {
        // A TextBlock whose Alignment is explicitly Left inside a centring container: the explicit
        // setting always wins over the ambient.
        string stream = Render(root => root.Column(c =>
        {
            var block = new TextBlock { Text = LongTitle, FontSize = 13, Alignment = TextAlignment.Left };
            c.Item().AlignCenter().Element(block);
        }));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2);
        Assert.All(runs, r => Assert.Equal(50f, r.X, 1));
    }

    [Fact]
    public void AnUnsetOrStretchContainer_PassesTheAmbientThroughUnchanged()
    {
        // A plain wrapping Column between the centring container and the text does not reset the ambient.
        string stream = Render(root => root.AlignCenter().Column(c => c.Item().Text(LongTitle).FontSize(13)));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2);
        Assert.All(runs, r => Assert.True(r.X > 55));
    }

    [Fact]
    public void ANestedContainer_OverridesTheOuterAmbient()
    {
        string stream = Render(root => root.AlignCenter().Column(c =>
            c.Item().AlignLeft().Text(LongTitle).FontSize(13)));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2);
        Assert.All(runs, r => Assert.Equal(50f, r.X, 1));
    }

    [Fact]
    public void RichText_AlsoPicksUpTheAmbient()
    {
        string stream = Render(root => root.Column(c => c.Item().AlignCenter().Text(t => { t.Span(LongTitle); })));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2, "the RichText should wrap to more than one line");
        Assert.All(runs, r => Assert.True(r.X > 55));
    }

    [Fact]
    public void HorizontalStackAlignment_DoesNotFeedTheAmbient()
    {
        // HorizontalStack.Alignment is cross-axis (vertical) positioning of row children, not text
        // alignment, and MUST NOT leak into descendant text as a centring ambient.
        string stream = Render(root => root.Row(row =>
        {
            row.AlignItems(HorizontalAlignment.Center);
            row.RelativeItem().Text(LongTitle).FontSize(13);
        }));

        List<(float X, string Text)> runs = TextRuns(stream);
        Assert.True(runs.Count >= 2);
        Assert.All(runs, r => Assert.Equal(50f, r.X, 1));
    }
}
