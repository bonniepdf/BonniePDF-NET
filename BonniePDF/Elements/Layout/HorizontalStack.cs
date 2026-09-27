using System;
using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// A layout element that arranges children horizontally with spacing and alignment options
/// </summary>
public class HorizontalStack : ElementBase, IStatefulElement
{
    private readonly List<IElement> _children = [];
    private readonly List<float?> _widths = [];

    // State tracking for multipage rendering
    private bool _rendered;

    // While a row is split across pages: how many fragments are drawn, and which items have finished.
    private int _fragments;
    private bool[]? _done;

    /// <summary>
    /// Spacing between children (in points)
    /// </summary>
    public float Spacing { get; set; } = 10f;

    /// <summary>
    /// Horizontal alignment of children within the stack
    /// </summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Start;

    /// <summary>
    /// Width distribution mode for children
    /// </summary>
    public WidthDistribution WidthDistribution { get; set; } = WidthDistribution.Auto;

    /// <summary>
    /// Optional fixed height for the stack. If null, height is determined by the tallest child.
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// The children elements in this stack (exposed for collection initializer syntax)
    /// </summary>
    public IList<IElement> Children => _children;

    /// <inheritdoc />
    public IEnumerable<IElement> GetChildren() => _children;

    /// <summary>
    /// Adds a child element to this stack with auto width
    /// </summary>
    public void Add(IElement child)
    {
        Add(child, null);
    }

    /// <summary>
    /// Adds a child element to this stack with a specific weight (only used when WidthDistribution is Weighted)
    /// </summary>
    public void Add(IElement child, float? weight)
    {
        _children.Add(child);
        _widths.Add(weight);
    }

    /// <summary>
    /// Renders this horizontal stack with state tracking for pagination.
    /// A row that fits is drawn whole. A row that does not fit, and has an item that can split across pages
    /// (a table, a column, a paragraph), splits: each page carries the next piece of every unfinished item,
    /// side by side from the same top, until all are done. A row of items that cannot split moves to a
    /// fresh page as a whole.
    /// </summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (_fragments == 0)
        {
            if (_rendered) return RenderStatus.Complete;

            float contentWidth = GetContentWidth(width);
            float contentHeight = MeasureContentHeight(contentWidth);
            float totalHeight = contentHeight + Margin.Top + Margin.Bottom;

            bool wouldOverflow = ctx.CurrentY + totalHeight > ctx.MaxY;
            bool notAtTop = ctx.CurrentY > ctx.ContentTop;

            if (!wouldOverflow || !CanSplit)
            {
                if (wouldOverflow && notAtTop)
                {
                    // Doesn't fit but would fit on a fresh page - request new page
                    return RenderStatus.WrapRequired;
                }

                // Fits on this page, or we're at top of page (render anyway to avoid infinite loop)
                Render(ctx, width);
                _rendered = true;
                return RenderStatus.Complete;
            }
        }

        return RenderFragment(ctx, width);
    }

    private bool CanSplit => !Height.HasValue && _children.Exists(c => c is IStatefulElement);

    private RenderStatus RenderFragment(RenderContext ctx, float width)
    {
        bool first = _fragments == 0;
        _done ??= new bool[_children.Count];
        float entryX = ctx.CurrentX;
        float entryY = ctx.CurrentY;

        (List<float> childWidths, _) = CalculateChildWidths(GetContentWidth(width));
        if (first) ctx.CurrentY += Margin.Top;
        float startY = ctx.CurrentY;
        float x = ctx.CurrentX + Margin.Left;
        float endY = startY;
        bool progress = false;
        bool allDone = true;

        for (int i = 0; i < _children.Count; i++)
        {
            IElement child = _children[i];
            if (!_done[i])
            {
                ctx.CurrentX = x;
                ctx.CurrentY = startY;
                ChildTagToken token = ChildTagging.BeginChildTag(ctx, child);
                try
                {
                    if (child is IStatefulElement stateful)
                    {
                        RenderStatus status = stateful.RenderStateful(ctx, childWidths[i]);
                        if (status != RenderStatus.WrapRequired)
                        {
                            progress |= status == RenderStatus.Complete || ctx.CurrentY > startY;
                            endY = Math.Max(endY, ctx.CurrentY);
                        }

                        if (status == RenderStatus.Complete) _done[i] = true;
                    }
                    else
                    {
                        // An item that cannot split is drawn once, with the first piece.
                        child.Render(ctx, childWidths[i]);
                        endY = Math.Max(endY, startY + child.MeasureHeight(childWidths[i]));
                        progress = true;
                        _done[i] = true;
                    }
                }
                finally
                {
                    ChildTagging.EndChildTag(ctx, token);
                }
            }

            if (!_done[i]) allDone = false;
            x += childWidths[i] + Spacing;
        }

        ctx.CurrentX = entryX;

        if (!progress)
        {
            // Nothing fitted on this page: the caller starts a fresh page and asks again.
            ctx.CurrentY = entryY;
            return RenderStatus.WrapRequired;
        }

        ctx.CurrentY = endY;
        if (allDone)
        {
            ctx.CurrentY += Margin.Bottom;
            _rendered = true;
            _fragments = 0;
            _done = null;
            return RenderStatus.Complete;
        }

        _fragments++;
        return RenderStatus.Overflow;
    }

    /// <summary>
    /// Resets the internal state, preparing the element for a fresh render.
    /// </summary>
    public void ResetState()
    {
        _rendered = false;
        _fragments = 0;
        _done = null;

        foreach (IElement child in _children)
            if (child is IStatefulElement statefulChild)
                statefulChild.ResetState();
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        if (Height.HasValue) return Height.Value;

        (List<float> childWidths, float totalSpacing) = CalculateChildWidths(contentWidth);

        float maxHeight = 0;
        for (int i = 0; i < _children.Count; i++)
        {
            float childHeight = _children[i].MeasureHeight(childWidths[i]);
            if (childHeight > maxHeight)
                maxHeight = childHeight;
        }

        return maxHeight;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        (List<float> childWidths, float totalSpacing) = CalculateChildWidths(contentWidth);

        // Calculate maximum height for alignment
        float maxHeight = Height ?? 0;
        float[] childHeights = new float[_children.Count];

        for (int i = 0; i < _children.Count; i++)
        {
            childHeights[i] = _children[i].MeasureHeight(childWidths[i]);
            if (!Height.HasValue && childHeights[i] > maxHeight)
                maxHeight = childHeights[i];
        }

        float originalX = ctx.CurrentX;

        // Render each child
        for (int i = 0; i < _children.Count; i++)
        {
            IElement child = _children[i];
            float childWidth = childWidths[i];
            float childHeight = childHeights[i];

            // Calculate vertical offset based on alignment
            float verticalOffset = 0;
            if (Alignment == HorizontalAlignment.Center)
                verticalOffset = (maxHeight - childHeight) / 2;
            else if (Alignment == HorizontalAlignment.End) verticalOffset = maxHeight - childHeight;
            // Note: Stretch is not implemented for horizontal stack as it would require
            // modifying the child's height, which is not supported by the IElement interface

            // Save state and render child. Tag role-bearing children so their
            // content isn't emitted untagged (PDF/UA 7.1) - the stack itself
            // renders children directly rather than via the pipeline.
            float yBefore = ctx.CurrentY;
            ctx.CurrentY += verticalOffset;

            ChildTagToken token = ChildTagging.BeginChildTag(ctx, child);
            try
            {
                child.Render(ctx, childWidth);
            }
            finally
            {
                ChildTagging.EndChildTag(ctx, token);
            }

            ctx.CurrentY = yBefore;
            ctx.CurrentX += childWidth + Spacing;
        }

        // Restore original X and advance Y by max height
        ctx.CurrentX = originalX;
        ctx.CurrentY += maxHeight;
    }

    private (List<float> childWidths, float totalSpacing) CalculateChildWidths(float availableWidth)
    {
        List<float> childWidths = new();
        float totalSpacing = _children.Count > 0 ? (_children.Count - 1) * Spacing : 0;
        float availableForChildren = availableWidth - totalSpacing;

        if (_children.Count == 0) return (childWidths, totalSpacing);

        switch (WidthDistribution)
        {
            case WidthDistribution.Equal:
                float equalWidth = availableForChildren / _children.Count;
                for (int i = 0; i < _children.Count; i++) childWidths.Add(equalWidth);
                break;

            case WidthDistribution.Weighted:
                // Mixed Mode: Support both weighted (flex) and auto-sized elements
                float usedSpace = 0;
                float totalWeight = 0;
                float[] calculatedWidths = new float[_children.Count];
                bool[] isWeighted = new bool[_children.Count];

                // Pass 1: Measure Auto/Fixed elements (null weight) and sum weights
                for (int i = 0; i < _children.Count; i++)
                {
                    float? weight = _widths[i];
                    if (weight.HasValue && weight.Value > 0)
                    {
                        totalWeight += weight.Value;
                        isWeighted[i] = true;
                    }
                    else
                    {
                        // Auto size (weight is null or 0)
                        float w = _children[i].GetPreferredWidth();
                        calculatedWidths[i] = w;
                        usedSpace += w;
                        isWeighted[i] = false;
                    }
                }

                float remainingSpace = availableForChildren - usedSpace;

                // Ensure we don't have negative space (clip if needed)
                // In a real engine, we might shrink Auto elements here, but for now we clamp
                if (remainingSpace < 0) remainingSpace = 0;

                // Pass 2: Distribute remaining space to weighted children
                for (int i = 0; i < _children.Count; i++)
                    if (isWeighted[i])
                    {
                        float weight = _widths[i]!.Value;
                        // Use ratio of total weight
                        calculatedWidths[i] = weight / totalWeight * remainingSpace;
                    }

                childWidths.AddRange(calculatedWidths);
                break;

            case WidthDistribution.Auto:
            default:
                // Auto mode: Measure everything, scale down if needed
                float totalPreferredWidth = 0;
                float[] preferredWidths = new float[_children.Count];

                for (int i = 0; i < _children.Count; i++)
                {
                    preferredWidths[i] = _children[i].GetPreferredWidth();
                    // If preferred width is 0 (e.g. flexible element), default to equal share of available?
                    // Or keep 0? For now, if 0, we might want to fallback to equal share logic 
                    // BUT CalculateChildWidths used to default to available/count.
                    // Let's stick to true intrinsic sizing.
                    if (preferredWidths[i] == 0)
                        // Fallback for elements that don't report width: assume flexible?
                        // For backwards compatibility with old "Auto" which split equally:
                        preferredWidths[i] = availableForChildren / _children.Count;
                    totalPreferredWidth += preferredWidths[i];
                }

                // If total preferred width fits, use it; otherwise distribute proportionally
                if (totalPreferredWidth <= availableForChildren)
                {
                    for (int i = 0; i < _children.Count; i++) childWidths.Add(preferredWidths[i]);
                }
                else
                {
                    float scale = availableForChildren / totalPreferredWidth;
                    for (int i = 0; i < _children.Count; i++) childWidths.Add(preferredWidths[i] * scale);
                }

                break;
        }

        return (childWidths, totalSpacing);
    }
}
