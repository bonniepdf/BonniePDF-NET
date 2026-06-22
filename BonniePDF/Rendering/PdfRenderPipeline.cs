using System;
using System.Collections.Generic;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF.Rendering;

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
    internal static void RenderElementWithOverflow(IElement element, RenderContext ctx, float contentWidth)
    {
        // when tagging is on and the element declares a structure
        // role, push the role onto the in-memory tree and wrap the
        // element's render in a /role MCID BDC ... EMC pair so the
        // canvas's draw calls attach as marked content. Pop after.
        //
        // No-op when tagging is off (Tagging == null) or the element
        // isn't an IRoleProvider - pre-G-1 elements behave exactly as
        // they did.
        //
        // Shared with the layout containers via BeginChildTag/EndChildTag so
        // every child-render site tags consistently (and suppresses structure
        // tags inside /Artifact regions per PDF/UA 7.1-2).
        //
        ChildTagToken token = ChildTagging.BeginChildTag(ctx, element);
        try
        {
            RenderElementWithOverflowCore(element, ctx, contentWidth, token);
        }
        finally
        {
            ChildTagging.EndChildTag(ctx, token);
        }
    }

    private static void RenderElementWithOverflowCore(IElement element, RenderContext ctx, float contentWidth, ChildTagToken token = default)
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
                            // Element cannot fit even on a fresh page - skip it to avoid infinite loop
                            statefulElement.ResetState();
                            return;
                        }
                    }
                    else
                    {
                        // Overflow means progress was made, reset wrap counter
                        wrapAttempts = 0;
                    }

                    // Each page's content stream must have balanced BDC/EMC operators.
                    // Close the canvas-side marked-content scope before switching pages,
                    // then reopen it on the new page. The structure-tree node stays open
                    // throughout; BeginMarkedContent on the new page allocates a fresh MCID
                    // and attaches it to the same structure element.
                    ctx.Canvas.RestoreState();
                    ChildTagging.PauseChildTag(ctx, token);
                    ctx.NewPage();
                    ctx.Canvas.SaveState();
                    ChildTagging.ResumeChildTag(ctx, element, token);
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
        // (headers/footers) can inadvertently trigger ctx.NewPage() - e.g. a Container's
        // pagination check fires when CurrentY is near MaxY during footer rendering.
        // If we evaluated ctx.Pages.Count on each iteration, new pages would be added
        // endlessly, causing an infinite loop.
        int totalPages = ctx.Pages.Count;
        for (int i = 0; i < totalPages; i++)
        {
            // Get dimensions for this page (use stored dimensions or defaults)
            PdfSize pageSize = ctx.PageSize;

            if (pageDimensions.TryGetValue(i, out var dimensions))
            {
                pageSize = dimensions.Size;
            }

            // Prepare context for template rendering
            // Set dimensions BEFORE SwitchToPage so the canvas uses correct PageHeight
            ctx.PageSize = pageSize;
            ctx.Margin = pageDimensions.TryGetValue(i, out dimensions) ? dimensions.Margin : settings.Margin;
            ctx.SwitchToPage(i);

            // full-page background slot. Painted behind everything,
            // ignoring margins. Prepended so it ends up at the very back of
            // the painter's-model stream.
            RenderPageSlot(ctx, settings.PageBackground, i, isBackground: true);

            // Render Templates (Headers/Footers)
            RenderTemplate(ctx, settings, i);

            // The watermark paints on top of the body content and header/footer
            // so a "DRAFT" / "CONFIDENTIAL" overlay is always visible and never
            // occluded by opaque page elements. Only the explicit foreground
            // slot sits above it.
            RenderWatermark(ctx, settings, i, pageSize.Width, pageSize.Height);

            // full-page foreground slot. Painted on top of everything,
            // ignoring margins (explicit stamps / overlays).
            RenderPageSlot(ctx, settings.PageForeground, i, isBackground: false);

            PageRenderOutput output = new()
            {
                ContentStream = ctx.Pages[i].ToString(),
                Size = pageSize,
                // Capture hyperlinks for this page index
                Hyperlinks = ctx.PageHyperlinks.TryGetValue(i, out var hyperlinks)
                    ? hyperlinks
                    : new List<Hyperlink>()
            };

            yield return output;
        }
    }

    /// <summary>
    /// Renders a text watermark over page content by appending PDF operators to the page stream.
    /// Uses ExtGState for opacity and the cm operator for rotation.
    /// Bypasses ICanvas because rotation is not supported by the canvas abstraction.
    /// </summary>
    private static void RenderWatermark(RenderContext ctx, DocumentSettings settings, int pageIndex,
        float pageWidth, float pageHeight)
    {
        if (settings.Watermark == null || string.IsNullOrEmpty(settings.Watermark.Text))
            return;

        if (ctx.IsSvgMode)
        {
            RenderWatermarkSvg(ctx, settings.Watermark, pageIndex, pageWidth, pageHeight);
            return;
        }

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

        // Determine text encoding - embedded fonts use glyph ID hex strings
        TtfFont embeddedFont = ctx.ResolveEmbeddedFont(wm.FontName);
        string textOperand = embeddedFont != null
            ? PdfSyntax.ToGlyphIdHexString(wm.Text, embeddedFont)
            : $"({PdfSyntax.Escape(wm.Text)})";

        // Build PDF operators for the watermark
        // The operators are appended to the page stream so they render on top of all content
        StringBuilder wmOps = new();
        PdfOperators.SaveState(wmOps);
        PdfOperators.SetGraphicsState(wmOps, ctx.RegisterAlphaGraphicsState(wm.Opacity));
        PdfOperators.ConcatMatrix(wmOps, cos, sin, -sin, cos, cx, cy);
        PdfOperators.BeginText(wmOps);
        PdfOperators.SetFont(wmOps, fontResourceName, wm.FontSize);

        // PDF/A-1 forbids transparency (ISO 19005-1 §6.4), so its /ca alpha is forced
        // to 1.0 and a real transparency would render as solid text. Simulate the
        // faint watermark by blending the colour toward white instead. PDF/A-2/3/4
        // keep their real /ca alpha, so no blending is needed there.
        Color fillColor = wm.TextColor;
        bool isPdfA1 = settings.Conformance is PdfAConformance.PdfA1b or PdfAConformance.PdfA1a;
        if (isPdfA1 && wm.Opacity < 1.0f)
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

        // wrap watermark in /Artifact BMC ... EMC so screen readers skip it.
        if (ctx.Tagging != null)
        {
            wmOps.Insert(0, "/Artifact BMC\n");
            wmOps.AppendLf("EMC");
        }

        // Append to page stream (painter's model: last drawn = on top), so the
        // watermark overlays the content rather than being hidden behind it.
        ctx.Pages[pageIndex].Append(wmOps);
    }

    /// <summary>
    /// Renders the watermark as an SVG group appended to the page body so it
    /// paints on top of content. Mirrors the PDF watermark: centred, rotated,
    /// semi-transparent text.
    /// </summary>
    private static void RenderWatermarkSvg(RenderContext ctx, WatermarkSettings wm, int pageIndex,
        float pageWidth, float pageHeight)
    {
        float cx = pageWidth / 2f;
        float cy = pageHeight / 2f;
        string hex = wm.TextColor.ToHexString();
        string text = XmlEscape.EscapeXml(wm.Text);

        // rotate() in SVG is clockwise-positive; the layout model treats positive
        // as counter-clockwise (matching the PDF rotate operator), so negate.
        string fmt(float v) => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string markup =
            $"<g transform=\"rotate({fmt(-wm.RotationDegrees)} {fmt(cx)} {fmt(cy)})\" opacity=\"{fmt(wm.Opacity)}\">" +
            $"<text x=\"{fmt(cx)}\" y=\"{fmt(cy)}\" fill=\"{hex}\" font-size=\"{fmt(wm.FontSize)}\" " +
            $"font-family=\"{wm.FontName}\" text-anchor=\"middle\" dominant-baseline=\"central\">{text}</text>" +
            "</g>\n";

        ctx.Pages[pageIndex].Append(markup);
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

            // Render Header - centred within the top-margin band so it sits in
            // its own region with breathing room above and below, rather than
            // flush against the page edge.
            if (hasHeader)
            {
                IElement headerElement = settings.HeaderTemplate!(pageNumber);
                if (headerElement != null)
                {
                    float headerHeight = headerElement.MeasureHeight(contentWidth);
                    ctx.CurrentX = startX;
                    ctx.CurrentY = MathF.Max(0f, (ctx.Margin - headerHeight) / 2f);
                    if (ctx.Tagging != null) ctx.Canvas.BeginArtifact();
                    headerElement.Render(ctx, contentWidth);
                    if (ctx.Tagging != null) ctx.Canvas.EndArtifact();
                }
            }

            // Render Footer - symmetric: centred within the bottom-margin band.
            if (hasFooter)
            {
                IElement footerElement = settings.FooterTemplate!(pageNumber);
                if (footerElement != null)
                {
                    float footerHeight = footerElement.MeasureHeight(contentWidth);
                    float bandTop = ctx.PageSize.Height - ctx.Margin;
                    ctx.CurrentX = startX;
                    ctx.CurrentY = bandTop + MathF.Max(0f, (ctx.Margin - footerHeight) / 2f);
                    if (ctx.Tagging != null) ctx.Canvas.BeginArtifact();
                    footerElement.Render(ctx, contentWidth);
                    if (ctx.Tagging != null) ctx.Canvas.EndArtifact();
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

    /// <summary>
    /// Renders a full-page slot element (this feature page Background / Foreground).
    /// The slot spans the entire page and ignores the margins, so the element
    /// is rendered at the page origin (0, 0) with the full page width.
    ///
    /// When <paramref name="isBackground"/> is <c>true</c> the slot's operators
    /// are prepended to the page stream so they paint behind all existing
    /// content (including the watermark); otherwise they are appended so they
    /// paint on top of everything.
    /// </summary>
    private static void RenderPageSlot(RenderContext ctx, Func<int, IElement?>? template, int pageIndex, bool isBackground)
    {
        if (template == null) return;

        IElement? element = template(pageIndex + 1);
        if (element == null) return;

        // For the background slot we render into the freshly-emptied stream and
        // re-append the captured content afterwards, leaving the slot operators
        // at the very front. The foreground slot renders straight onto the end.
        StringBuilder stream = ctx.Pages[pageIndex];
        string? captured = null;
        if (isBackground)
        {
            captured = stream.ToString();
            stream.Clear();
        }

        ICanvas originalCanvas = ctx.Canvas;
        float originalX = ctx.CurrentX;
        float originalY = ctx.CurrentY;

        // Fixed canvas writes to the current page stream with absolute positioning.
        ctx.SetCanvas(ctx.GetFixedCanvas());
        try
        {
            // Slots ignore margins: full page width, origin at the top-left corner.
            float fullWidth = ctx.PageSize.Width;
            ctx.CurrentX = 0;
            ctx.CurrentY = 0;

            // Page slots are decorative chrome - keep them out of the structure
            // tree (PDF/UA) by wrapping in an /Artifact marked-content section.
            if (ctx.Tagging != null) ctx.Canvas.BeginArtifact();
            element.Render(ctx, fullWidth);
            if (ctx.Tagging != null) ctx.Canvas.EndArtifact();
        }
        finally
        {
            ctx.SetCanvas(originalCanvas);
            ctx.CurrentX = originalX;
            ctx.CurrentY = originalY;

            if (isBackground && captured != null) stream.Append(captured);
        }
    }
}
