using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Export;
using BonniePDF.Rendering;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// Container FillHeight - shared conformance fixture layout/fill-height.
/// A container with FillHeight and no pinned Height arranges to the body content
/// height left on the current page, never shrinking below its natural content
/// height, and resolves per page against that page's own chrome.
///
/// Mirrors core/tests/layout/container-fill-height.test.ts.
/// </summary>
public class ContainerFillHeightTests
{
    private static readonly PdfSize Page = PdfSize.A4;
    private const float Margin = 50f;

    private static TextBlock Body() => new() { Text = "band", FontSize = 12 };

    /// <summary>A container tall enough to outgrow the space left on the page.</summary>
    private static Container Tall(float height) => new() { Height = height, Child = Body() };

    private static BonniePDF.Document NewDoc()
    {
        var doc = new BonniePDF.Document();
        doc.Settings.PageSize = Page;
        doc.Settings.Margin = Margin;
        return doc;
    }

    /// <summary>Renders the document and returns every Container node, in document order.</summary>
    private static IReadOnlyList<LayoutNode> Containers(BonniePDF.Document doc)
    {
        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);

        var found = new List<LayoutNode>();
        void Visit(LayoutNode node)
        {
            if (string.Equals(node.Type, "Container", StringComparison.Ordinal)) found.Add(node);
            foreach (LayoutNode child in node.Children) Visit(child);
        }

        Visit(snapshot.Root);
        return found;
    }

    [Fact]
    public void FillsTheWholeBodyBand_WhenItStartsAtTheTopOfThePage()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new Container { FillHeight = true, Child = Body() });

        LayoutRect rect = Containers(doc)[0].Rects[0];
        Assert.Equal(Margin, rect.Y, 3);
        Assert.Equal(Page.Height - Margin * 2f, rect.Height, 3);
    }

    [Fact]
    public void FillsOnlyWhatIsLeft_BelowContentAlreadyOnThePage()
    {
        BonniePDF.Document doc = NewDoc();
        var stack = new VerticalStack();
        stack.Add(new Container { Height = 250f, Child = Body() });
        stack.Add(new Container { FillHeight = true, Child = Body() });
        doc.Add(stack);

        LayoutRect filler = Containers(doc)[1].Rects[0];
        Assert.Equal(Page.Height - Margin - (Margin + 250f), filler.Height, 3);
    }

    [Fact]
    public void ResolvesAgainstTheReservedBand_NotTheMarginBox()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Settings.HeaderTemplate = _ => new Container { Height = 120f };
        doc.Settings.FooterTemplate = _ => new Container { Height = 90f };
        doc.Add(new Container { FillHeight = true, Child = Body() });

        LayoutRect rect = Containers(doc)[0].Rects[0];
        Assert.Equal(120f, rect.Y, 3);
        Assert.Equal(Page.Height - 90f - 120f, rect.Height, 3);
    }

    [Fact]
    public void NeverShrinksContent_BelowItsNaturalHeight()
    {
        BonniePDF.Document doc = NewDoc();
        // Content taller than a whole content band (741.89pt), so no page break
        // can rescue it: fill must report the content's height, not the band's.
        doc.Add(new Container { FillHeight = true, Child = Tall(900f) });

        LayoutRect rect = Containers(doc)[0].Rects[0];
        Assert.Equal(900f, rect.Height, 3);
    }

    [Fact]
    public void LetsAnExplicitHeightWinOutright()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new Container { FillHeight = true, Height = 120f, Child = Body() });

        Assert.Equal(120f, Containers(doc)[0].Rects[0].Height, 3);
    }

    [Fact]
    public void MeasuresToItsNaturalContentHeight_IgnoringFill()
    {
        var filling = new Container { FillHeight = true, Child = Body() };
        var plain = new Container { Child = Body() };

        Assert.Equal(plain.MeasureHeight(400f), filling.MeasureHeight(400f), 3);
    }

    [Fact]
    public void AnchorsContentToTheFootOfTheBand_WithBottomAlignment()
    {
        BonniePDF.Document doc = NewDoc();
        doc.Add(new Container
        {
            FillHeight = true,
            VerticalAlignment = VerticalAlignment.End,
            Child = Body(),
        });

        LayoutSnapshot? snapshot = doc
            .GenerateSvgDocument(new SvgPageExportOptions { CaptureLayout = true })
            .Snapshot;
        Assert.NotNull(snapshot);

        LayoutNode text = Walk(snapshot.Root)
            .First(n => string.Equals(n.Type, "TextBlock", StringComparison.Ordinal));

        // Bottom-anchored inside a band running to Page.Height - Margin.
        Assert.Equal(Page.Height - Margin, text.Rects[0].Y + text.Rects[0].Height, 1);
    }

    private static IEnumerable<LayoutNode> Walk(LayoutNode node)
    {
        yield return node;
        foreach (LayoutNode child in node.Children)
            foreach (LayoutNode d in Walk(child))
                yield return d;
    }
}
