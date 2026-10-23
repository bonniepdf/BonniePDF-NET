using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Pagination control primitives - shared conformance fixture
/// layout/pagination-primitives.
///
/// Every case is expressed with Container heights rather than text, so the
/// numbers are font-independent and the TypeScript suite asserts exactly the
/// same ones. A4 portrait with a 50pt margin gives a body band of 741.89pt
/// (841.89 - 2 x 50), which every "how much is left" figure below derives from.
///
/// Mirrors core/tests/layout/pagination-primitives.test.ts.
/// </summary>
public class PaginationPrimitivesTests
{
    private static readonly PdfSize Page = PdfSize.A4;
    private const float Margin = 50f;

    /// <summary>Body content height of one page: 841.89 - 2 x 50.</summary>
    private const float Band = 741.89f;

    private static BonniePDF.Document NewDoc()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.PageSize = Page;
        doc.Settings.Margin = Margin;
        return doc;
    }

    /// <summary>A block of an exact height, independent of any font metric.</summary>
    private static Container Block(float height, string name) =>
        new() { Height = height, CaptureName = name };

    /// <summary>Text tall enough to be split by the engine, measured in whole lines.</summary>
    private static TextBlock Lines(int count, string name) => new()
    {
        Text = string.Join(
            "\n",
            Enumerable.Range(1, count).Select(i => "line " + i.ToString(CultureInfo.InvariantCulture))),
        FontSize = 12,
        CaptureName = name,
    };

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct Placement(int PageIndex, float Y, float Height);

    /// <summary>Every rect the named node occupied, in page order.</summary>
    private static IReadOnlyList<Placement> Placements(BonniePDF.Document doc, string name)
    {
        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);

        var found = new List<LayoutNode>();
        void Visit(LayoutNode node)
        {
            if (string.Equals(node.Name, name, StringComparison.Ordinal)) found.Add(node);
            foreach (LayoutNode child in node.Children) Visit(child);
        }

        Visit(snapshot.Root);
        return found
            .SelectMany(n => n.Rects)
            .Select(r => new Placement(r.Page, Round(r.Y), Round(r.Height)))
            .ToList();
    }

    private static int PageCount(BonniePDF.Document doc)
    {
        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);
        return snapshot.Pages.Count;
    }

    private static float Round(float value) => MathF.Round(value * 100f) / 100f;

    /// <summary>A stack that fills `used` points of the page, then the element under test.</summary>
    private static VerticalStack AfterFiller(float used, IElement subject)
    {
        var stack = new VerticalStack();
        stack.Add(Block(used, "filler"));
        stack.Add(subject);
        return stack;
    }

    /// <summary>
    /// A stack whose leading text overflows onto a second page, so the element
    /// under test is reached with the cursor already on page 2. A Container
    /// filler cannot do this - it breaks whole rather than splitting, which
    /// leaves the cursor at the top of the new page before the filler, not after.
    /// </summary>
    private static VerticalStack OnSecondPage(IElement subject)
    {
        var stack = new VerticalStack();
        stack.Add(Lines(70, "filler"));
        stack.Add(subject);
        return stack;
    }

    // --- BreakAllowed (the PreventPageBreak property form) ---

    [Fact]
    public void BreakAllowed_SplitsAStatefulChildThatAllowsBreaking()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, Lines(20, "body")));

        IReadOnlyList<Placement> rects = Placements(doc, "body");
        Assert.True(rects.Count > 1);
        Assert.Equal(0, rects[0].PageIndex);
        Assert.Equal(1, rects[1].PageIndex);
    }

    [Fact]
    public void BreakAllowed_MovesAChildThatRefusesToBreakWholeOntoTheNextPage()
    {
        BonniePDF.Document doc = NewDoc();
        TextBlock body = Lines(20, "body");
        body.BreakAllowed = false;
        doc.Add(AfterFiller(Band - 100f, body));

        Placement rect = Assert.Single(Placements(doc, "body"));
        Assert.Equal(1, rect.PageIndex);
        Assert.Equal(Margin, rect.Y);
    }

    [Fact]
    public void BreakAllowed_StillSplitsAChildTooTallForAnyPage()
    {
        BonniePDF.Document doc = NewDoc();
        TextBlock body = Lines(120, "body");
        body.BreakAllowed = false;
        doc.Add(body);

        Assert.True(Placements(doc, "body").Count > 1);
    }

    // --- EnsureSpace ---

    [Fact]
    public void EnsureSpace_BreaksWhenLessThanTheRequiredSpaceRemains()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, new EnsureSpace(150f, Block(40f, "heading"))));

        Placement rect = Assert.Single(Placements(doc, "heading"));
        Assert.Equal(1, rect.PageIndex);
    }

    [Fact]
    public void EnsureSpace_StaysPutWhenTheRequiredSpaceIsAvailable()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 200f, new EnsureSpace(150f, Block(40f, "heading"))));

        Placement rect = Assert.Single(Placements(doc, "heading"));
        Assert.Equal(0, rect.PageIndex);
    }

    [Fact]
    public void EnsureSpace_NeverBreaksAtTheTopOfAPage()
    {
        BonniePDF.Document doc = NewDoc();
        // Demanding more than a whole band can ever offer.
        doc.Add(new EnsureSpace(Band + 200f, Block(40f, "heading")));

        Assert.Equal(0, Placements(doc, "heading")[0].PageIndex);
        Assert.Equal(1, PageCount(doc));
    }

    // --- PreventPageBreak ---

    [Fact]
    public void PreventPageBreak_MovesAChildThatWouldStraddleTheBoundary()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, new PreventPageBreak(Lines(20, "body"))));

        Placement rect = Assert.Single(Placements(doc, "body"));
        Assert.Equal(1, rect.PageIndex);
    }

    [Fact]
    public void PreventPageBreak_LeavesAChildThatFitsWhereItIs()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 300f, new PreventPageBreak(Lines(20, "body"))));

        Assert.Equal(0, Placements(doc, "body")[0].PageIndex);
    }

    [Fact]
    public void PreventPageBreak_StillSplitsAChildTallerThanAWholePage()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new PreventPageBreak(Lines(120, "body")));

        Assert.True(Placements(doc, "body").Count > 1);
    }

    // --- ShowEntire ---

    [Fact]
    public void ShowEntire_MovesAChildThatDoesNotFitOntoAFreshPage()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, new ShowEntire(Lines(20, "panel"))));

        Assert.Equal(1, Placements(doc, "panel")[0].PageIndex);
    }

    [Fact]
    public void ShowEntire_RefusesToSplit_DroppingWhatDoesNotFit()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new ShowEntire(Lines(120, "panel")));

        Placement rect = Assert.Single(Placements(doc, "panel"));
        Assert.Equal(0, rect.PageIndex);
        Assert.Equal(1, PageCount(doc));
    }

    // --- StopPaging ---

    [Fact]
    public void StopPaging_RendersWhatFitsAndDiscardsTheRest()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, new StopPaging(Lines(60, "appendix"))));

        Placement rect = Assert.Single(Placements(doc, "appendix"));
        Assert.Equal(0, rect.PageIndex);
        Assert.Equal(1, PageCount(doc));
    }

    [Fact]
    public void StopPaging_DoesNotMoveToAFreshPageFirst()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(AfterFiller(Band - 100f, new StopPaging(Lines(20, "appendix"))));

        Assert.Equal(0, Placements(doc, "appendix")[0].PageIndex);
    }

    // --- ShowIf / ShowOnce / SkipOnce ---

    [Fact]
    public void ShowIf_RendersWhenThePredicateAcceptsThePage()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new ShowIf(p => p.IsFirstPage, Block(40f, "note")));

        Assert.Single(Placements(doc, "note"));
    }

    [Fact]
    public void ShowIf_RendersNothingWhenThePredicateRejectsThePage()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new ShowIf(p => !p.IsFirstPage, Block(40f, "note")));

        Assert.Empty(Placements(doc, "note"));
    }

    [Fact]
    public void ShowIf_PassesTheOneBasedPageNumberToThePredicate()
    {
        var seen = new List<int>();
        BonniePDF.Document doc = NewDoc();
        doc.Settings.HeaderTemplate = _ => new ShowIf(
            p =>
            {
                seen.Add(p.PageNumber);
                return true;
            },
            Block(20f, "running-head"));
        doc.Add(Lines(120, "body"));
        PageCount(doc);

        Assert.NotEmpty(seen);
        Assert.Equal(1, seen.Min());
    }

    [Fact]
    public void ShowIf_TreatsAMissingPredicateAsATransparentWrapper()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new ShowIf { Child = Block(40f, "note") });

        Assert.Single(Placements(doc, "note"));
    }

    [Fact]
    public void ShowOnce_RendersOnTheFirstPageOnly()
    {
        BonniePDF.Document onPageOne = NewDoc();
        onPageOne.Add(new ShowOnce(Block(40f, "cover-note")));
        Assert.Single(Placements(onPageOne, "cover-note"));

        BonniePDF.Document onPageTwo = NewDoc();
        onPageTwo.Add(OnSecondPage(new ShowOnce(Block(40f, "cover-note"))));
        Assert.Empty(Placements(onPageTwo, "cover-note"));
    }

    [Fact]
    public void SkipOnce_IsTheMirrorOfShowOnce()
    {
        BonniePDF.Document onPageOne = NewDoc();
        onPageOne.Add(new SkipOnce(Block(40f, "running-note")));
        Assert.Empty(Placements(onPageOne, "running-note"));

        BonniePDF.Document onPageTwo = NewDoc();
        onPageTwo.Add(OnSecondPage(new SkipOnce(Block(40f, "running-note"))));
        Placement rect = Assert.Single(Placements(onPageTwo, "running-note"));
        Assert.Equal(1, rect.PageIndex);
    }

    // --- Unconstrained ---

    [Fact]
    public void Unconstrained_ConsumesNoFlowHeight()
    {
        BonniePDF.Document doc = NewDoc();
        var stack = new VerticalStack();
        stack.Add(new Unconstrained(Block(200f, "stamp")));
        stack.Add(Block(40f, "after"));
        doc.Add(stack);

        IReadOnlyList<Placement> stamp = Placements(doc, "stamp");
        IReadOnlyList<Placement> after = Placements(doc, "after");
        Assert.Equal(Margin, stamp[0].Y);
        Assert.Equal(Margin, after[0].Y);
        Assert.Equal(0, after[0].PageIndex);
    }

    [Fact]
    public void Unconstrained_MeasuresAsZeroHeightWhateverTheChildMeasures()
    {
        Assert.Equal(0f, new Unconstrained(Block(400f, "stamp")).MeasureHeight(500f));
        Assert.Equal(400f, Block(400f, "stamp").MeasureHeight(500f));
    }

    [Fact]
    public void Unconstrained_LeavesTheCursorUntouchedSoFollowingContentOverlaps()
    {
        BonniePDF.Document doc = NewDoc();
        var stack = new VerticalStack();
        stack.Add(new Unconstrained(Lines(4, "stamp")));
        stack.Add(Lines(4, "over"));
        doc.Add(stack);

        Assert.Equal(Placements(doc, "stamp")[0].Y, Placements(doc, "over")[0].Y);
    }
}
