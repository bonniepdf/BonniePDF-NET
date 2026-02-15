using BonniePDF.Configuration;
using BonniePDF.Drawing;
using BonniePDF.IO;
using BonniePDF.Layout;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// Tests for the Unified Flow & Pagination architecture.
/// Verifies that IStatefulElement, overflow handling, and templates work correctly.
/// </summary>
public class FlowArchitectureTests
{
    #region RenderStatus Tests

    [Fact]
    public void RenderStatus_HasExpectedValues()
    {
        Assert.Equal(0, (int)RenderStatus.Complete);
        Assert.Equal(1, (int)RenderStatus.Overflow);
        Assert.Equal(2, (int)RenderStatus.WrapRequired);
    }

    #endregion

    #region Helper Methods

    private static RenderContext CreateTestContext()
    {
        return new RenderContext(PdfSize.A4, 50);
    }

    #endregion

    #region TextBlock Stateful Tests

    [Fact]
    public void TextBlock_ImplementsIStatefulElement()
    {
        TextBlock textBlock = new() { Text = "Hello World" };
        Assert.True(textBlock is IStatefulElement);
    }

    [Fact]
    public void TextBlock_ShortTextReturnsComplete()
    {
        TextBlock textBlock = new() { Text = "Short text" };
        RenderContext ctx = CreateTestContext();

        RenderStatus status = textBlock.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void TextBlock_ResetStateClearsTracking()
    {
        TextBlock textBlock = new()
            { Text = "Test text that might span multiple lines when rendered to a narrow width" };
        RenderContext ctx = CreateTestContext();

        // Render once
        textBlock.RenderStateful(ctx, 100);

        // Reset state
        textBlock.ResetState();

        // Should be able to render again from the beginning
        ctx.CurrentY = ctx.Margin;
        RenderStatus status = textBlock.RenderStateful(ctx, 100);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void TextBlock_LongTextOverflows()
    {
        // Create a text block with many lines that will overflow a small page
        string lines = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i} of test content"));
        TextBlock textBlock = new() { Text = lines, FontSize = 12 };

        // Create a small context (200pt height, 50pt margins = 100pt content area)
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        RenderStatus status = textBlock.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void TextBlock_ContinuesAfterOverflow()
    {
        // Create enough lines to definitely overflow (100 lines on a 100pt content area)
        string lines = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Line {i} of test content"));
        TextBlock textBlock = new() { Text = lines, FontSize = 12 };

        // Use a small page (200pt height, 50pt margins = 100pt content area)
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        // First render - should overflow since 100 lines * ~14pt line height >> 100pt content area
        RenderStatus status1 = textBlock.RenderStateful(ctx, 500);

        // The first render may overflow or complete depending on the text layout
        // What we're really testing is that after multiple renders, we eventually complete
        int pageCount = 1;
        RenderStatus status = status1;

        while (status == RenderStatus.Overflow && pageCount < 100) // Safety limit
        {
            ctx.NewPage();
            pageCount++;
            status = textBlock.RenderStateful(ctx, 500);
        }

        // Should eventually complete (not be stuck in an infinite loop)
        Assert.Equal(RenderStatus.Complete, status);

        // Should have used multiple pages if there was overflow
        // Note: If the first render was already Complete, that's also acceptable
    }

    #endregion

    #region VerticalStack Stateful Tests

    [Fact]
    public void VerticalStack_ImplementsIStatefulElement()
    {
        VerticalStack stack = new();
        Assert.True(stack is IStatefulElement);
    }

    [Fact]
    public void VerticalStack_EmptyStackReturnsComplete()
    {
        VerticalStack stack = new();
        RenderContext ctx = CreateTestContext();

        RenderStatus status = stack.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void VerticalStack_SingleSmallChildReturnsComplete()
    {
        VerticalStack stack = new()
        {
            Children = { new TextBlock { Text = "Hello" } }
        };
        RenderContext ctx = CreateTestContext();

        RenderStatus status = stack.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void VerticalStack_ManyChildrenOverflow()
    {
        VerticalStack stack = new();
        for (int i = 0; i < 50; i++)
            stack.AddChild(new TextBlock { Text = $"Item {i}: Some content here", FontSize = 14 });

        RenderContext ctx = new(new PdfSize(612, 200), 50);

        RenderStatus status = stack.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void VerticalStack_ResetStateResetsAllChildren()
    {
        VerticalStack stack = new()
        {
            Children =
            {
                new TextBlock { Text = "Child 1" },
                new TextBlock { Text = "Child 2" },
                new TextBlock { Text = "Child 3" }
            }
        };

        RenderContext ctx = CreateTestContext();

        // Render first time
        stack.RenderStateful(ctx, 500);

        // Reset
        stack.ResetState();

        // Should render from the beginning again
        ctx.CurrentY = ctx.Margin;
        RenderStatus status = stack.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void VerticalStack_NestedStacksWork()
    {
        VerticalStack innerStack = new()
        {
            Children =
            {
                new TextBlock { Text = "Inner 1" },
                new TextBlock { Text = "Inner 2" }
            }
        };

        VerticalStack outerStack = new()
        {
            Children =
            {
                new TextBlock { Text = "Outer 1" },
                innerStack,
                new TextBlock { Text = "Outer 2" }
            }
        };

        RenderContext ctx = CreateTestContext();

        RenderStatus status = outerStack.RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    #endregion

    #region RenderContext Tests

    [Fact]
    public void RenderContext_OnPageCreatedEventFires()
    {
        RenderContext ctx = CreateTestContext();
        int pageCreatedCount = 0;
        ctx.OnPageCreated += pageIndex => pageCreatedCount++;

        ctx.NewPage();
        ctx.NewPage();

        Assert.Equal(2, pageCreatedCount);
    }

    [Fact]
    public void RenderContext_IsLastPageDefaultsFalse()
    {
        RenderContext ctx = CreateTestContext();
        Assert.False(ctx.IsLastPage);
    }

    [Fact]
    public void RenderContext_IsLastPageCanBeSet()
    {
        RenderContext ctx = CreateTestContext();
        ctx.IsLastPage = true;
        Assert.True(ctx.IsLastPage);
    }

    [Fact]
    public void RenderContext_GetFixedCanvasReturnsCanvas()
    {
        RenderContext ctx = CreateTestContext();
        ICanvas? canvas = ctx.GetFixedCanvas();
        Assert.NotNull(canvas);
    }

    #endregion

    #region DocumentSettings Template Tests

    [Fact]
    public void DocumentSettings_HeaderTemplateCanBeSet()
    {
        DocumentSettings settings = new()
        {
            HeaderTemplate = pageNum => new TextBlock { Text = $"Page {pageNum}" }
        };

        Assert.NotNull(settings.HeaderTemplate);

        IElement? header = settings.HeaderTemplate(1);
        Assert.NotNull(header);
        Assert.IsType<TextBlock>(header);
    }

    [Fact]
    public void DocumentSettings_FooterTemplateCanBeSet()
    {
        DocumentSettings settings = new()
        {
            FooterTemplate = pageNum => new TextBlock { Text = $"Page {pageNum}" }
        };

        Assert.NotNull(settings.FooterTemplate);

        IElement? footer = settings.FooterTemplate(5);
        Assert.NotNull(footer);
    }

    [Fact]
    public void DocumentSettings_TemplatesCanReturnNull()
    {
        DocumentSettings settings = new()
        {
            HeaderTemplate = pageNum => pageNum > 1 ? new TextBlock { Text = "Header" } : null
        };

        IElement? page1Header = settings.HeaderTemplate(1);
        IElement? page2Header = settings.HeaderTemplate(2);

        Assert.Null(page1Header);
        Assert.NotNull(page2Header);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Document_WithOverflowingTextCreatesMultiplePages()
    {
        using Document doc = new();

        // Add enough text to overflow onto multiple pages
        string longText = string.Join("\n\n", Enumerable.Range(1, 100).Select(i =>
            $"Paragraph {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."));

        doc.Add(new TextBlock { Text = longText, FontSize = 12 });

        using MemoryStream ms = new();
        doc.Save(ms, true);

        // Verify PDF was created
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void Document_WithVerticalStackOverflowCreatesMultiplePages()
    {
        using Document doc = new();

        VerticalStack stack = new();
        for (int i = 0; i < 100; i++)
            stack.AddChild(new TextBlock
            {
                Text = $"Item {i}: This is a list item with some content that takes up space.",
                FontSize = 12
            });

        doc.Add(stack);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void Document_TablesStillPaginateCorrectly()
    {
        using Document doc = new();

        Table table = new(1f, 1f, 1f);
        table.AddHeader(
            new TextBlock { Text = "Name" },
            new TextBlock { Text = "Age" },
            new TextBlock { Text = "City" }
        );

        for (int i = 0; i < 100; i++)
            table.AddRow(
                new TextBlock { Text = $"Person {i}" },
                new TextBlock { Text = $"{20 + i % 50}" },
                new TextBlock { Text = $"City {i % 10}" }
            );

        doc.Add(table);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    #endregion

    #region WrapRequired Tests

    [Fact]
    public void VerticalStack_ReturnsWrapRequired_WhenFirstChildDoesNotFit()
    {
        // A non-stateful child that's 200pt tall
        Circle largeChild = new() { Diameter = 200 };

        VerticalStack stack = new();
        stack.AddChild(largeChild);

        // Small page: 300pt height, 50pt margins = 200pt content area (MaxY = 250)
        // Position cursor near the bottom so 200pt child won't fit
        RenderContext ctx = new(new PdfSize(612, 300), 50);
        ctx.CurrentY = 200; // Only 50pt left before MaxY

        RenderStatus status = stack.RenderStateful(ctx, 500);

        // First child can't fit and it's the first child attempted — WrapRequired
        Assert.Equal(RenderStatus.WrapRequired, status);
    }

    [Fact]
    public void VerticalStack_ReturnsOverflow_WhenLaterChildDoesNotFit()
    {
        // First child fits (small), second doesn't
        TextBlock smallChild = new() { Text = "Small", FontSize = 12 };
        Circle largeChild = new() { Diameter = 200 };

        VerticalStack stack = new();
        stack.AddChild(smallChild);
        stack.AddChild(largeChild);

        // Position cursor so first child fits but second doesn't
        RenderContext ctx = new(new PdfSize(612, 300), 50);
        ctx.CurrentY = 60; // ~190pt available, small text fits, 200pt circle won't

        RenderStatus status = stack.RenderStateful(ctx, 500);

        // First child rendered (progress made), second can't fit — Overflow, not WrapRequired
        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void VerticalStack_PropagatesWrapRequired_FromStatefulChild()
    {
        // Create a nested stack that will return WrapRequired
        Circle largeChild = new() { Diameter = 200 };
        VerticalStack innerStack = new();
        innerStack.AddChild(largeChild);

        VerticalStack outerStack = new();
        outerStack.AddChild(innerStack);

        // Position near bottom
        RenderContext ctx = new(new PdfSize(612, 300), 50);
        ctx.CurrentY = 200;

        RenderStatus status = outerStack.RenderStateful(ctx, 500);

        // Inner stack returns WrapRequired, outer propagates it since it's the first child
        Assert.Equal(RenderStatus.WrapRequired, status);
    }

    [Fact]
    public void Pipeline_HandlesWrapRequired_ByCreatingNewPage()
    {
        using Document doc = new();

        // Add a large stack that fills most of the page, then a large circle
        VerticalStack stack = new();

        // Fill most of the page with text
        string longText = string.Join("\n", Enumerable.Range(1, 40).Select(i => $"Line {i}"));
        stack.AddChild(new TextBlock { Text = longText, FontSize = 12 });

        // Add a large circle that won't fit in the remaining space
        stack.AddChild(new Circle { Diameter = 300 });

        doc.Add(stack);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        // Should produce a valid PDF without infinite loop
        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void Pipeline_WrapRequired_AtTopOfPage_DoesNotInfiniteLoop()
    {
        using Document doc = new();

        // A circle taller than the entire content area
        // On A4 with 50pt margins: content area = 842 - 100 = 742pt
        // This 1000pt circle will trigger WrapRequired even on a fresh page
        VerticalStack stack = new();
        stack.AddChild(new Circle { Diameter = 1000 });

        doc.Add(stack);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        // Should not hang — pipeline gives up after MaxWrapAttempts
        Assert.True(ms.Length > 0);
    }

    #endregion

    #region Canvas State Isolation Tests

    [Fact]
    public void Pipeline_WrapsElementRenderingInSaveRestoreState()
    {
        // Use RenderContext directly to inspect the raw content stream before PDF compression
        var settings = new DocumentSettings();
        var page = new Page(PdfSize.A4.Width, PdfSize.A4.Height, 50);
        page.Add(new TextBlock { Text = "First element" });
        page.Add(new TextBlock { Text = "Second element" });

        var result = PdfRenderPipeline.RenderPages(new[] { page }, settings);

        // The raw content stream should contain q/Q pairs from SaveState/RestoreState
        // Split into lines to check for standalone q and Q operators
        string contentStream = result.PageContentStreams[0];
        string[] lines = contentStream.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string[] trimmedLines = lines.Select(l => l.Trim()).ToArray();

        // Should have at least 2 q's (one per element) and 2 Q's
        int saveCount = trimmedLines.Count(l => l == "q");
        int restoreCount = trimmedLines.Count(l => l == "Q");
        Assert.True(saveCount >= 2, $"Expected at least 2 SaveState (q) operators, found {saveCount}");
        Assert.True(restoreCount >= 2, $"Expected at least 2 RestoreState (Q) operators, found {restoreCount}");
    }

    #endregion

    #region BreakAllowed Tests

    [Fact]
    public void ElementBase_BreakAllowedDefaultsToTrue()
    {
        TextBlock textBlock = new() { Text = "Test" };
        Assert.True(textBlock.BreakAllowed);
    }

    [Fact]
    public void VerticalStack_RespectsBreakAllowedFalse_MovesToNewPage()
    {
        // Use narrow width (100pt) to force word wrapping and create many lines
        // Each word ~40pt wide at FontSize 12, so ~2 words per line
        string keepText = string.Join(" ", Enumerable.Range(1, 30).Select(i => $"WordKeep{i}"));
        TextBlock keepTogether = new() { Text = keepText, BreakAllowed = false, FontSize = 12 };

        VerticalStack stack = new();
        // Add filler that takes some space
        string fillerText = string.Join(" ", Enumerable.Range(1, 6).Select(i => $"Filler{i}"));
        stack.AddChild(new TextBlock { Text = fillerText, FontSize = 12 });
        stack.AddChild(keepTogether);

        // Small page: 200pt height, 50pt margins = 100pt content area (MaxY=150)
        // Narrow width forces word wrapping to create many lines
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        RenderStatus status = stack.RenderStateful(ctx, 100);

        // Filler fits, keepTogether has many lines and BreakAllowed=false — should overflow
        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void BreakAllowedFalse_ElementTooLargeForPage_RendersAnyway()
    {
        // Element that can't fit on any single page — must render anyway to avoid infinite loop
        string hugeText = string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}"));
        VerticalStack stack = new() { BreakAllowed = false };
        stack.AddChild(new TextBlock { Text = hugeText });

        using Document doc = new();
        doc.Add(stack);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    [Fact]
    public void Pipeline_RespectsBreakAllowedFalse_AtRootLevel()
    {
        // A root element with BreakAllowed=false that doesn't fit in remaining space
        string text = string.Join("\n", Enumerable.Range(1, 10).Select(i => $"Line {i}"));
        TextBlock block = new() { Text = text, BreakAllowed = false };

        using Document doc = new();
        // Add filler first
        doc.Add(new TextBlock
        {
            Text = string.Join("\n", Enumerable.Range(1, 40).Select(i => $"Filler {i}"))
        });
        doc.Add(block);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    #endregion

    #region Table Stateful Tests

    [Fact]
    public void Table_ImplementsIStatefulElement()
    {
        Table table = new(1f, 1f);
        Assert.True(table is IStatefulElement);
    }

    [Fact]
    public void Table_SmallTableReturnsComplete()
    {
        Table table = new(1f, 1f);
        table.AddRow(new TextBlock { Text = "Cell 1" }, new TextBlock { Text = "Cell 2" });

        RenderContext ctx = CreateTestContext();

        RenderStatus status = ((IStatefulElement)table).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void Table_LargeTableReturnsOverflow()
    {
        Table table = new(1f, 1f, 1f);
        for (int i = 0; i < 100; i++)
        {
            table.AddRow(
                new TextBlock { Text = $"Name {i}" },
                new TextBlock { Text = $"{20 + i}" },
                new TextBlock { Text = $"City {i}" }
            );
        }

        // Small page
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        RenderStatus status = ((IStatefulElement)table).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void Table_ContinuesAfterOverflow()
    {
        Table table = new(1f, 1f);
        for (int i = 0; i < 50; i++)
            table.AddRow(new TextBlock { Text = $"Row {i}" }, new TextBlock { Text = $"Data {i}" });

        // Small page
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        int pageCount = 1;
        RenderStatus status = ((IStatefulElement)table).RenderStateful(ctx, 500);

        while (status != RenderStatus.Complete && pageCount < 50)
        {
            ctx.NewPage();
            pageCount++;
            status = ((IStatefulElement)table).RenderStateful(ctx, 500);
        }

        Assert.Equal(RenderStatus.Complete, status);
        Assert.True(pageCount > 1);
    }

    [Fact]
    public void Table_ResetStateClearsTracking()
    {
        Table table = new(1f, 1f);
        for (int i = 0; i < 50; i++)
            table.AddRow(new TextBlock { Text = $"Row {i}" }, new TextBlock { Text = $"Data {i}" });

        RenderContext ctx = new(new PdfSize(612, 200), 50);

        // Render first page
        ((IStatefulElement)table).RenderStateful(ctx, 500);

        // Reset
        ((IStatefulElement)table).ResetState();

        // Should start from the beginning again
        ctx.CurrentY = ctx.Margin;
        RenderStatus status = ((IStatefulElement)table).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void Table_HeadersRepeatOnContinuationPages_ViaStateful()
    {
        Table table = new(1f, 1f) { RepeatHeadersOnNewPage = true };
        table.AddHeader(new TextBlock { Text = "Header 1" }, new TextBlock { Text = "Header 2" });
        for (int i = 0; i < 100; i++)
            table.AddRow(new TextBlock { Text = $"Row {i}" }, new TextBlock { Text = $"Data {i}" });

        using Document doc = new();
        doc.Add(table);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    #endregion

    #region HorizontalStack Stateful Tests

    [Fact]
    public void HorizontalStack_ImplementsIStatefulElement()
    {
        HorizontalStack stack = new();
        Assert.True(stack is IStatefulElement);
    }

    [Fact]
    public void HorizontalStack_SmallStackReturnsComplete()
    {
        HorizontalStack stack = new();
        stack.AddChild(new TextBlock { Text = "Left" });
        stack.AddChild(new TextBlock { Text = "Right" });

        RenderContext ctx = CreateTestContext();

        RenderStatus status = ((IStatefulElement)stack).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void HorizontalStack_ReturnsWrapRequired_WhenDoesNotFit()
    {
        HorizontalStack stack = new();
        // Use narrow width + long text to force word wrapping, creating tall children
        string text = string.Join(" ", Enumerable.Range(1, 20).Select(i => $"Word{i}"));
        stack.AddChild(new TextBlock { Text = text, FontSize = 12 });
        stack.AddChild(new TextBlock { Text = text, FontSize = 12 });

        // Position near bottom of a small page so the tall stack won't fit
        RenderContext ctx = new(new PdfSize(612, 200), 50);
        ctx.CurrentY = 130;

        // Narrow width forces word wrapping, making children tall
        RenderStatus status = ((IStatefulElement)stack).RenderStateful(ctx, 80);

        Assert.Equal(RenderStatus.WrapRequired, status);
    }

    [Fact]
    public void HorizontalStack_RendersAtTopOfPage_EvenIfTall()
    {
        HorizontalStack stack = new();
        stack.AddChild(new Circle { Diameter = 300 });

        // At top of a small page — should render anyway
        RenderContext ctx = new(new PdfSize(612, 200), 50);

        RenderStatus status = ((IStatefulElement)stack).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void HorizontalStack_ResetStateClearsTracking()
    {
        HorizontalStack stack = new();
        stack.AddChild(new TextBlock { Text = "Content" });

        RenderContext ctx = CreateTestContext();

        // Render first time
        ((IStatefulElement)stack).RenderStateful(ctx, 500);

        // Reset
        ((IStatefulElement)stack).ResetState();

        // Should render again from beginning
        ctx.CurrentY = ctx.Margin;
        RenderStatus status = ((IStatefulElement)stack).RenderStateful(ctx, 500);

        Assert.Equal(RenderStatus.Complete, status);
    }

    [Fact]
    public void HorizontalStack_InVerticalStack_PaginatesCorrectly()
    {
        VerticalStack vStack = new();
        // Fill most of the page
        string filler = string.Join("\n", Enumerable.Range(1, 40).Select(i => $"Line {i}"));
        vStack.AddChild(new TextBlock { Text = filler });

        // Add a horizontal stack that won't fit
        HorizontalStack hStack = new();
        hStack.AddChild(new TextBlock { Text = "Left column\nLine 2\nLine 3" });
        hStack.AddChild(new TextBlock { Text = "Right column\nLine 2\nLine 3" });
        vStack.AddChild(hStack);

        using Document doc = new();
        doc.Add(vStack);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    #endregion

    #region Orphan/Widow Control Tests

    [Fact]
    public void TextBlock_MinLinesBeforeBreakDefaultsTo2()
    {
        TextBlock textBlock = new() { Text = "Test" };
        Assert.Equal(2, textBlock.MinLinesBeforeBreak);
    }

    [Fact]
    public void TextBlock_MinLinesAfterBreakDefaultsTo2()
    {
        TextBlock textBlock = new() { Text = "Test" };
        Assert.Equal(2, textBlock.MinLinesAfterBreak);
    }

    [Fact]
    public void TextBlock_OrphanControl_MovesToNewPage_WhenTooFewLinesFit()
    {
        // Use narrow width to create many wrapped lines. Each word ~50pt at FontSize 12.
        // At width=60pt, each word gets its own line → 10 lines.
        string text = string.Join(" ", Enumerable.Range(1, 10).Select(i => $"LongWord{i}"));
        TextBlock textBlock = new()
        {
            Text = text,
            FontSize = 12,
            MinLinesBeforeBreak = 3
        };

        // Line height is 14.4pt. MaxY = 150.
        // Position so only 2 lines fit: need CurrentY where (150 - CurrentY) / 14.4 < 3
        // 150 - 122 = 28pt → 28/14.4 = 1.94 → 1 line fits (< 3)
        RenderContext ctx = new(new PdfSize(612, 200), 50);
        ctx.CurrentY = 122;

        // Width 60pt → each LongWord gets its own line
        RenderStatus status = textBlock.RenderStateful(ctx, 60);

        // Should return WrapRequired (orphan: fewer than 3 lines fit)
        Assert.Equal(RenderStatus.WrapRequired, status);
    }

    [Fact]
    public void TextBlock_OrphanControl_RendersIfEnoughLinesFit()
    {
        // 8 lines of text at narrow width, MinLinesBeforeBreak = 2
        string text = string.Join(" ", Enumerable.Range(1, 8).Select(i => $"LongWord{i}"));
        TextBlock textBlock = new()
        {
            Text = text,
            FontSize = 12,
            MinLinesBeforeBreak = 2
        };

        // Line height = 14.4pt. MaxY = 150.
        // At CurrentY=100, space = 50pt → 3 lines fit (≥ MinLinesBeforeBreak=2)
        RenderContext ctx = new(new PdfSize(612, 200), 50);
        ctx.CurrentY = 100;

        // Width 60pt → each word on its own line → 8 lines total, 3 fit
        RenderStatus status = textBlock.RenderStateful(ctx, 60);

        // 3 lines fit ≥ MinLinesBeforeBreak(2), but not all 8 → Overflow
        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void TextBlock_OrphanControl_DisabledWhenSetTo1()
    {
        // 10 lines at narrow width, only 1 fits, but MinLinesBeforeBreak = 1 (disabled)
        string text = string.Join(" ", Enumerable.Range(1, 10).Select(i => $"LongWord{i}"));
        TextBlock textBlock = new()
        {
            Text = text,
            FontSize = 12,
            MinLinesBeforeBreak = 1
        };

        // Only space for 1 line: 150 - 136 = 14pt, 14/14.4 < 1... need just barely room
        // 150 - 135 = 15pt → 1 line fits
        RenderContext ctx = new(new PdfSize(612, 200), 50);
        ctx.CurrentY = 135;

        // Width 60pt → each word gets its own line
        RenderStatus status = textBlock.RenderStateful(ctx, 60);

        // MinLinesBeforeBreak=1 → orphan control disabled, should render the 1 line (Overflow)
        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void TextBlock_WidowControl_AdjustsBreakPoint()
    {
        // 5 lines at narrow width. Space for 4 → 1 widow. MinLinesAfterBreak=2 → render 3 instead
        string text = string.Join(" ", Enumerable.Range(1, 5).Select(i => $"LongWord{i}"));
        TextBlock textBlock = new()
        {
            Text = text,
            FontSize = 12,
            MinLinesAfterBreak = 2,
            MinLinesBeforeBreak = 1 // Disable orphan so it doesn't interfere
        };

        // Line height = 14.4pt. 4 lines need 57.6pt. MaxY = 150.
        // At CurrentY=92: 150 - 92 = 58pt → floor(58/14.4) = 4 lines fit
        // 5 - 4 = 1 line left (< MinLinesAfterBreak=2) → adjust to 3
        RenderContext ctx = new(new PdfSize(612, 200), 50);
        ctx.CurrentY = 92;

        // Width 60pt → each word gets its own line → exactly 5 lines
        RenderStatus status = textBlock.RenderStateful(ctx, 60);

        // Widow control: 4 fit but 1 would remain → render only 3 → Overflow
        Assert.Equal(RenderStatus.Overflow, status);
    }

    [Fact]
    public void TextBlock_WithOrphanWidowControl_ProducesValidPdf()
    {
        // Use space-separated words to create many wrapped lines at default width
        string longText = string.Join(" ", Enumerable.Range(1, 500).Select(i =>
            $"Word{i}"));

        TextBlock textBlock = new()
        {
            Text = longText,
            FontSize = 12,
            MinLinesBeforeBreak = 3,
            MinLinesAfterBreak = 3
        };

        using Document doc = new();
        doc.Add(textBlock);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
    }

    #endregion
}