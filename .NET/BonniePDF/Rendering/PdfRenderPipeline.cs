using System;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Drawing;
using BonniePDF.Layout;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;
using BonniePDF.Typography;

namespace BonniePDF.IO;

/// <summary>
/// Shared render pipeline that performs the layout and rendering phase.
/// This eliminates code duplication between sync and async save methods.
/// The rendering phase is CPU-bound and synchronous, producing a PdfRenderResult
/// that can then be written by either sync or async writers.
/// Supports stateful elements (IStatefulElement) that can flow across page boundaries.
/// </summary>
public static class PdfRenderPipeline
{
    /// <summary>
    /// Renders all pages and produces a PdfRenderResult containing all the data
    /// needed to write the PDF file.
    /// This method:
    /// 1. Creates and configures a RenderContext
    /// 2. Iterates through all pages and renders their elements
    /// 3. Tracks font usage, image usage, and hyperlinks
    /// 4. Returns a complete render result
    /// </summary>
    /// <param name="pages">The pages to render</param>
    /// <param name="settings">The document settings</param>
    /// <returns>A PdfRenderResult containing all rendered data</returns>
    public static PdfRenderResult RenderPages(IReadOnlyList<Page> pages, DocumentSettings settings)
    {
        if (pages == null || pages.Count == 0)
            throw new ArgumentException("Document must have at least one page", nameof(pages));

        PdfSize orientedPageSize = settings.GetOrientedPageSize();
        RenderContext ctx = new(orientedPageSize, settings.Margin);

        PdfRenderResult result = new()
        {
            RenderContext = ctx,
            OrientedPageSize = orientedPageSize
        };

        // PHASE 1: Render all pages to content streams
        // This populates ctx.FontNameMap and glyph usage trackers for embedded fonts
        int currentOutputIndex = 0;

        foreach (Page page in pages)
        {
            // Ensure we have a page builder for currentOutputIndex
            while (ctx.Pages.Count <= currentOutputIndex) ctx.Pages.Add(new StringBuilder());

            // IMPORTANT: Update RenderContext dimensions BEFORE SwitchToPage
            // SwitchToPage calls UpdateCanvas() which creates a PdfCanvas using PageHeight
            // for Y-axis coordinate transformation. If we set dimensions after, the canvas
            // will use stale values from the previous page, causing incorrect text positioning.
            float pageWidth = page.Width;
            float pageHeight = page.Height;
            float pageMargin = page.Margin;

            ctx.PageSize = page.Size;
            ctx.Margin = pageMargin;

            ctx.SwitchToPage(currentOutputIndex);

            // Reset cursor position
            ctx.CurrentX = pageMargin;
            ctx.CurrentY = pageMargin;

            // Calculate content width based on actual dimensions
            float contentWidth = pageWidth - pageMargin * 2;

            // Store page dimensions for the start page
            result.PageDimensions[currentOutputIndex] = new PageDimensions
            {
                Size = page.Size,
                Margin = pageMargin
            };

            // Render all elements with overflow support
            RenderElementsWithOverflow(page.Elements, ctx, contentWidth);

            // Propagate dimensions to any overflow pages created
            for (int i = currentOutputIndex + 1; i <= ctx.CurrentPageIndex; i++)
                result.PageDimensions[i] = new PageDimensions
                {
                    Size = page.Size,
                    Margin = pageMargin
                };

            // Advance output index for next input page
            currentOutputIndex = ctx.CurrentPageIndex + 1;
        }

        // Collect all content streams
        foreach (StringBuilder stream in ctx.Pages) result.PageContentStreams.Add(stream.ToString());

        return result;
    }

    /// <summary>
    /// Renders a collection of elements, handling overflow for stateful elements.
    /// </summary>
    private static void RenderElementsWithOverflow(IEnumerable<IElement> elements, RenderContext ctx,
        float contentWidth)
    {
        foreach (IElement element in elements) RenderElementWithOverflow(element, ctx, contentWidth);
    }

    /// <summary>
    /// Maximum number of consecutive WrapRequired results before giving up.
    /// Prevents infinite loops when an element can never fit.
    /// </summary>
    private const int MaxWrapAttempts = 2;

    /// <summary>
    /// Renders a single element, handling overflow for stateful elements.
    /// If the element implements IStatefulElement and returns Overflow,
    /// a new page is created and rendering continues.
    /// Each element's rendering is wrapped in SaveState/RestoreState to prevent
    /// graphics state leaks between elements.
    /// </summary>
    private static void RenderElementWithOverflow(IElement element, RenderContext ctx, float contentWidth)
    {
        if (element is IStatefulElement statefulElement)
        {
            // Check if this element disallows breaking across pages
            if (element is ElementBase { BreakAllowed: false })
            {
                float elementHeight = element.MeasureHeight(contentWidth);
                bool wouldOverflow = ctx.CurrentY + elementHeight > ctx.MaxY;
                bool notAtTop = ctx.CurrentY > ctx.Margin;
                bool wouldFitOnFreshPage = elementHeight <= ctx.MaxY - ctx.Margin;

                if (wouldOverflow && notAtTop && wouldFitOnFreshPage)
                {
                    ctx.NewPage();
                }
            }

            int wrapAttempts = 0;

            // Wrap in SaveState/RestoreState to isolate graphics state.
            // try/finally ensures RestoreState is always called even if Render throws,
            // preventing graphics state corruption on subsequent elements.
            ctx.Canvas.SaveState();
            try
            {
                RenderStatus status = statefulElement.RenderStateful(ctx, contentWidth);

                while (status != RenderStatus.Complete)
                {
                    if (status == RenderStatus.WrapRequired)
                    {
                        wrapAttempts++;
                        if (wrapAttempts > MaxWrapAttempts)
                        {
                            // Element cannot fit even on a fresh page — skip it to avoid infinite loop
                            statefulElement.ResetState();
                            return;
                        }
                    }
                    else
                    {
                        // Overflow means progress was made, reset wrap counter
                        wrapAttempts = 0;
                    }

                    // Close state on current page, start new page, open state on new page
                    ctx.Canvas.RestoreState();
                    ctx.NewPage();
                    ctx.Canvas.SaveState();
                    status = statefulElement.RenderStateful(ctx, contentWidth);
                }
            }
            finally
            {
                ctx.Canvas.RestoreState();
            }

            statefulElement.ResetState();
        }
        else
        {
            // Non-stateful element - render normally, wrapped in SaveState/RestoreState.
            // try/finally ensures RestoreState is always called even if Render throws.
            ctx.Canvas.SaveState();
            try
            {
                element.Render(ctx, contentWidth);
            }
            finally
            {
                ctx.Canvas.RestoreState();
            }
        }
    }

    /// <summary>
    /// Renders pages lazily (streaming), yielding results one by one.
    /// This keeps memory usage low for large documents.
    /// </summary>
    public static IEnumerable<PageRenderOutput> RenderPagesStreaming(
        IEnumerable<Page> pages,
        DocumentSettings settings,
        RenderContext ctx)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(ctx);

        // Get the oriented page size from settings - this respects orientation changes
        // made after the document was created (e.g., doc.Settings.Orientation = Landscape)
        PdfSize orientedPageSize = settings.GetOrientedPageSize();

        // Track page dimensions for each rendered page
        Dictionary<int, RenderedPageDimensions> pageDimensions = new();

        // First, render all input pages (this may create additional pages dynamically)
        int currentOutputIndex = 0;
        foreach (Page page in pages)
        {
            // Ensure RenderContext has a buffer for this page
            while (ctx.Pages.Count <= currentOutputIndex) ctx.Pages.Add(new StringBuilder());
            // IMPORTANT: Update dimensions BEFORE SwitchToPage so the canvas
            // is created with the correct PageHeight for Y-axis transformation
            float pageWidth = page.Width;
            float pageHeight = page.Height;
            float pageMargin = page.Margin;

            ctx.PageSize = page.Size;
            ctx.Margin = pageMargin;

            ctx.SwitchToPage(currentOutputIndex);
            ctx.CurrentX = pageMargin;
            ctx.CurrentY = pageMargin;

            // Calculate content width based on actual dimensions
            float contentWidth = pageWidth - pageMargin * 2;

            // Store page dimensions for this index
            pageDimensions[currentOutputIndex] = new RenderedPageDimensions
            {
                Size = page.Size,
                Margin = pageMargin
            };

            // Render Elements with overflow support
            RenderElementsWithOverflow(page.Elements, ctx, contentWidth);

            // Propagate dimensions to any overflow pages created
            for (int i = currentOutputIndex + 1; i <= ctx.CurrentPageIndex; i++)
                pageDimensions[i] = new RenderedPageDimensions
                {
                    Size = page.Size,
                    Margin = pageMargin
                };

            // Advance output index for next input page
            currentOutputIndex = ctx.CurrentPageIndex + 1;
        }

        // Now yield all rendered pages (including dynamically created ones)
        // IMPORTANT: Snapshot the count BEFORE the yield loop. Template rendering
        // (headers/footers) can inadvertently trigger ctx.NewPage() — e.g. a Container's
        // pagination check fires when CurrentY is near MaxY during footer rendering.
        // If we evaluated ctx.Pages.Count on each iteration, new pages would be added
        // endlessly, causing an infinite loop.
        int totalPages = ctx.Pages.Count;
        for (int i = 0; i < totalPages; i++)
        {
            // Get dimensions for this page (use stored dimensions or defaults)
            PdfSize pageSize = ctx.PageSize;

            if (pageDimensions.ContainsKey(i))
            {
                pageSize = pageDimensions[i].Size;
            }

            // Prepare context for template rendering
            // Set dimensions BEFORE SwitchToPage so the canvas uses correct PageHeight
            ctx.PageSize = pageSize;
            ctx.Margin = pageDimensions.ContainsKey(i) ? pageDimensions[i].Margin : settings.Margin;
            ctx.SwitchToPage(i);

            // Render Watermark (behind all content — prepended to stream)
            RenderWatermark(ctx, settings, i, pageSize.Width, pageSize.Height);

            // Render Templates (Headers/Footers)
            RenderTemplate(ctx, settings, i);

            PageRenderOutput output = new()
            {
                ContentStream = ctx.Pages[i].ToString(),
                Size = pageSize,
                // Capture hyperlinks for this page index
                Hyperlinks = ctx.PageHyperlinks.ContainsKey(i)
                    ? ctx.PageHyperlinks[i]
                    : new List<Hyperlink>()
            };

            yield return output;
        }
    }

    /// <summary>
    /// Renders a text watermark behind page content by prepending PDF operators to the page stream.
    /// Uses ExtGState for opacity and the cm operator for rotation.
    /// Bypasses ICanvas because rotation is not supported by the canvas abstraction.
    /// </summary>
    private static void RenderWatermark(RenderContext ctx, DocumentSettings settings, int pageIndex,
        float pageWidth, float pageHeight)
    {
        if (settings.Watermark == null || string.IsNullOrEmpty(settings.Watermark.Text))
            return;

        WatermarkSettings wm = settings.Watermark;

        // Register the watermark font so it gets a resource name and is included in the PDF
        string fontResourceName = ctx.RegisterFont(wm.FontName);

        // Track glyph usage for font subsetting (must happen after RegisterFont)
        ctx.TrackGlyphUsage(wm.FontName, wm.Text);

        // Calculate rotation matrix components
        float radians = wm.RotationDegrees * (float)(Math.PI / 180.0);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);

        // Page center in PDF coordinates (bottom-left origin)
        float cx = pageWidth / 2f;
        float cy = pageHeight / 2f;

        // Approximate text width for centering
        float textWidth = FontMetrics.GetStringWidth(wm.Text, StandardFonts.Helvetica, (int)wm.FontSize);

        // Determine text encoding — embedded fonts use glyph ID hex strings
        TtfFont embeddedFont = ctx.ResolveEmbeddedFont(wm.FontName);
        string textOperand = embeddedFont != null
            ? PdfSyntax.ToGlyphIdHexString(wm.Text, embeddedFont)
            : $"({PdfSyntax.Escape(wm.Text)})";

        // Build PDF operators for the watermark
        // The operators are prepended to the page stream so they render behind all content
        StringBuilder wmOps = new();
        PdfOperators.SaveState(wmOps);
        PdfOperators.SetGraphicsState(wmOps, "/GS_WM");
        PdfOperators.ConcatMatrix(wmOps, cos, sin, -sin, cos, cx, cy);
        PdfOperators.BeginText(wmOps);
        PdfOperators.SetFont(wmOps, fontResourceName, wm.FontSize);

        // PDF/A-1B forbids transparency — blend the color toward white to simulate opacity
        Color fillColor = wm.TextColor;
        if (ctx.PdfAEnabled && wm.Opacity < 1.0f)
        {
            float o = wm.Opacity;
            fillColor = Color.FromScRgb(
                o * fillColor.R / 255f + (1f - o),
                o * fillColor.G / 255f + (1f - o),
                o * fillColor.B / 255f + (1f - o));
        }

        PdfOperators.SetFillColor(wmOps, fillColor);
        PdfOperators.SetTextPosition(wmOps, -textWidth / 2f, 0);
        PdfOperators.ShowText(wmOps, textOperand);
        PdfOperators.EndText(wmOps);
        PdfOperators.RestoreState(wmOps);

        // Prepend to page stream (painter's model: first drawn = behind)
        ctx.Pages[pageIndex].Insert(0, wmOps.ToString());
    }

    /// <summary>
    /// Renders header and footer templates onto the current page.
    /// Uses a fixed canvas to isolate coordinates from the page content flow.
    /// </summary>
    private static void RenderTemplate(RenderContext ctx, DocumentSettings settings, int pageIndex)
    {
        // 1-based page number
        int pageNumber = pageIndex + 1;

        bool hasHeader = settings.HeaderTemplate != null;
        bool hasFooter = settings.FooterTemplate != null;

        if (!hasHeader && !hasFooter) return;

        // Save current state
        ICanvas originalCanvas = ctx.Canvas;
        float originalX = ctx.CurrentX;
        float originalY = ctx.CurrentY;

        // Use fixed canvas for absolute positioning
        ctx.SetCanvas(ctx.GetFixedCanvas());

        try
        {
            float contentWidth = ctx.PageSize.Width - ctx.Margin * 2;
            float startX = ctx.Margin;

            // Render Header
            if (hasHeader)
            {
                IElement headerElement = settings.HeaderTemplate!(pageNumber);
                if (headerElement != null)
                {
                    // Position at top of page (0)
                    ctx.CurrentX = startX;
                    ctx.CurrentY = 0;
                    headerElement.Render(ctx, contentWidth);
                }
            }

            // Render Footer
            if (hasFooter)
            {
                IElement footerElement = settings.FooterTemplate!(pageNumber);
                if (footerElement != null)
                {
                    // Position at start of bottom margin
                    ctx.CurrentX = startX;
                    ctx.CurrentY = ctx.PageSize.Height - ctx.Margin;
                    footerElement.Render(ctx, contentWidth);
                }
            }
        }
        finally
        {
            // Restore state
            ctx.SetCanvas(originalCanvas);
            ctx.CurrentX = originalX;
            ctx.CurrentY = originalY;
        }
    }
}