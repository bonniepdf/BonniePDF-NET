using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF;

/// <summary>
/// A layout element that stacks children vertically with automatic pagination.
/// Implements IStatefulElement to support rendering children that overflow across pages.
/// </summary>
public class VerticalStack : ElementBase, IStatefulElement
{
    private readonly List<IElement> _children = [];

    // State tracking for multipage rendering
    private int _lastChildRendered = -1;

    /// <summary>
    /// Spacing between children in points. Default is 0.
    /// </summary>
    public float Spacing { get; set; }

    /// <summary>
    /// The children elements in this stack (exposed for collection initializer syntax)
    /// </summary>
    public IList<IElement> Children => _children;

    /// <inheritdoc />
    public IEnumerable<IElement> GetChildren() => _children;

    /// <summary>
    /// Renders this vertical stack with state tracking for pagination.
    /// Children that implement IStatefulElement will be rendered with overflow support.
    /// </summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Apply top margin only on first render (not on continuation pages)
        if (_lastChildRendered < 0) ctx.CurrentY += Margin.Top;

        float contentWidth = width - Margin.Left - Margin.Right;

        // Save original X for margin application
        float originalX = ctx.CurrentX;
        ctx.CurrentX += Margin.Left;

        // Start from the next unrendered child
        int startChild = _lastChildRendered + 1;

        if (startChild >= _children.Count)
        {
            // All children already rendered
            ctx.CurrentX = originalX;
            ctx.CurrentY += Margin.Bottom;
            return RenderStatus.Complete;
        }

        for (int i = startChild; i < _children.Count; i++)
        {
            IElement child = _children[i];

            // Tag children that declare a semantic role. Uses the same
            // try/finally pattern as PdfRenderPipeline so tagging is
            // closed before Overflow/WrapRequired propagates upward.
            ChildTagToken token = ChildTagging.BeginChildTag(ctx, child);

            try
            {
                if (child is IStatefulElement statefulChild)
                {
                    // Check if this child disallows breaking across pages
                    if (child is ElementBase { BreakAllowed: false })
                    {
                        float childHeight = child.MeasureHeight(contentWidth);
                        bool wouldOverflow = ctx.CurrentY + childHeight > ctx.MaxY;
                        bool notAtTop = ctx.CurrentY > ctx.ContentTop;
                        bool wouldFitOnFreshPage = childHeight <= ctx.ContentHeight;

                        if (wouldOverflow && notAtTop && wouldFitOnFreshPage)
                        {
                            // Element won't fit here but would fit on a fresh page - don't split it
                            ctx.CurrentX = originalX;
                            return i == startChild ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                        }
                        // If at top of page or too large for any page, fall through and render anyway
                    }

                    // Layout capture: open the child node before rendering its
                    // segment. Coalesced across page-resumptions by element identity;
                    // closed only when the child reports Complete.
                    ctx.LayoutRecorder?.OpenNode(
                        child,
                        LayoutRecorder.CaptureTypeOf(child),
                        LayoutRecorder.CaptureNameOf(child),
                        ctx.CurrentPageIndex,
                        ctx.CurrentX,
                        ctx.CurrentY,
                        contentWidth);

                    // Render stateful child with overflow support
                    RenderStatus status = statefulChild.RenderStateful(ctx, contentWidth);

                    ctx.LayoutRecorder?.CloseNode(child, ctx.CurrentPageIndex, ctx.CurrentY, complete: status == RenderStatus.Complete);

                    if (status == RenderStatus.Overflow)
                    {
                        // Child rendered some content but has more - always Overflow since progress was made
                        _lastChildRendered = i - 1;
                        ctx.CurrentX = originalX;
                        return RenderStatus.Overflow;
                    }

                    if (status == RenderStatus.WrapRequired)
                    {
                        // Child couldn't render anything - propagate WrapRequired if no prior progress
                        _lastChildRendered = i - 1;
                        ctx.CurrentX = originalX;
                        return i == startChild ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                    }

                    // Child completed - mark it as rendered
                    _lastChildRendered = i;

                    // Apply spacing after completed child (except after last child)
                    if (Spacing > 0 && i < _children.Count - 1)
                        ctx.CurrentY += Spacing;

                    // Check if there are more children and if the next one would start past MaxY
                    // This handles the case where children individually fit but collectively overflow
                    if (i < _children.Count - 1 && ctx.CurrentY >= ctx.MaxY)
                    {
                        ctx.CurrentX = originalX;
                        return RenderStatus.Overflow;
                    }
                }
                else
                {
                    // Non-stateful child - check if it fits before rendering
                    // IMPORTANT: Tables and nested VerticalStacks handle their own pagination,
                    // so we should let them render even if they don't "fit"
                    bool canPaginateInternally = child is Table || child is VerticalStack;

                    if (!canPaginateInternally)
                    {
                        float childHeight = child.MeasureHeight(contentWidth);

                        // Check if it fits on the current page
                        bool wouldOverflow = ctx.CurrentY + childHeight > ctx.MaxY;
                        bool notAtTop = ctx.CurrentY > ctx.ContentTop;

                        if (wouldOverflow && notAtTop)
                        {
                            // This child doesn't fit - determine if we made any progress
                            // _lastChildRendered stays at previous value - we haven't rendered child i yet
                            ctx.CurrentX = originalX;
                            // If this is the first child we attempted on this page, zero progress was made
                            return i == startChild ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                        }
                    }

                    // Save position before render
                    float yBefore = ctx.CurrentY;
                    child.Render(ctx, contentWidth);

                    // Only advance Y if the child didn't already do it
                    if (ctx.CurrentY == yBefore)
                    {
                        float childHeight = child.MeasureHeight(contentWidth);
                        ctx.CurrentY += childHeight;
                    }

                    // Mark this child as rendered
                    _lastChildRendered = i;

                    // Apply spacing after rendered child (except after last child)
                    if (Spacing > 0 && i < _children.Count - 1)
                        ctx.CurrentY += Spacing;
                }
            }
            finally
            {
                ChildTagging.EndChildTag(ctx, token);
            }
        }

        // All children rendered
        ctx.CurrentX = originalX;
        ctx.CurrentY += Margin.Bottom;
        _lastChildRendered = _children.Count - 1;
        return RenderStatus.Complete;
    }

    /// <summary>
    /// Resets the internal state, preparing the element for a fresh render.
    /// Also resets state of all stateful children.
    /// </summary>
    public void ResetState()
    {
        _lastChildRendered = -1;

        // Reset state of all stateful children
        foreach (IElement child in _children)
            if (child is IStatefulElement statefulChild)
                statefulChild.ResetState();
    }

    /// <summary>
    /// Adds a child element to this stack
    /// </summary>
    public void Add(IElement child)
    {
        _children.Add(child);
    }

    protected override float GetPreferredContentWidth()
    {
        if (_children.Count == 0) return 0;
        return _children.Max(c => c.GetPreferredWidth());
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        // Note: In a pagination scenario, measuring the total height of a stack
        // is tricky because it might span multiple pages.
        // For this simple engine, we sum it up, but the Render method decides placement.
        float totalHeight = _children.Sum(c => c.MeasureHeight(contentWidth));
        if (_children.Count > 1 && Spacing > 0)
            totalHeight += (_children.Count - 1) * Spacing;
        return totalHeight;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        // Save original X for margin application
        float originalX = ctx.CurrentX;
        ctx.CurrentX += Margin.Left;

        foreach (IElement child in _children)
        {
            float childHeight = child.MeasureHeight(contentWidth);

            // CHECK: Does it fit on the current page?
            // If CurrentY + Height > MaxY, we need a new page.
            // We also check if CurrentY > Margin to ensure we don't loop forever
            // if a single item is taller than the whole page.
            // IMPORTANT: Only trigger page break for elements that DON'T handle their own pagination.
            // Tables, nested VerticalStacks, and IStatefulElement handle pagination internally.
            bool canPaginateInternally = child is Table || child is VerticalStack || child is IStatefulElement;
            bool elementFitsOnOnePage = childHeight <= ctx.ContentHeight;
            bool wouldOverflow = ctx.CurrentY + childHeight > ctx.MaxY;
            bool notAtTop = ctx.CurrentY > ctx.ContentTop;

            // Only force new page for small elements that don't fit
            // Large elements (like tables) handle their own pagination
            if (wouldOverflow && notAtTop && elementFitsOnOnePage && !canPaginateInternally) ctx.NewPage();

            // Tag children that declare a semantic role (same pattern as RenderStateful).
            ChildTagToken token = ChildTagging.BeginChildTag(ctx, child);

            try
            {
                // Save position before render
                float yBefore = ctx.CurrentY;
                child.Render(ctx, contentWidth);

                // Only advance Y if the child didn't already do it
                // (some elements like nested VerticalStacks and Tables advance Y themselves)
                if (ctx.CurrentY == yBefore) ctx.CurrentY += childHeight;

                // Apply spacing between children
                if (Spacing > 0 && child != _children[^1])
                    ctx.CurrentY += Spacing;
            }
            finally
            {
                ChildTagging.EndChildTag(ctx, token);
            }
        }

        // Restore original X
        ctx.CurrentX = originalX;
    }
}
