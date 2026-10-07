using System;
using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Base class for list elements (ordered and unordered) that provides common functionality
/// for item spacing, indentation, and nested list handling. This reduces code duplication
/// across list types and ensures consistent list rendering behavior.
/// </summary>
public abstract class ListElementBase : TextElementBase
{
    /// <summary>
    /// Indentation for nested lists (in points)
    /// </summary>
    public float Indent { get; set; } = 20f;

    /// <summary>
    /// Spacing between list items (in points)
    /// </summary>
    public float ItemSpacing { get; set; } = 5f;

    /// <summary>
    /// Child lists rendered as nested list content.
    /// </summary>
    public virtual IReadOnlyList<ListElementBase> GetNestedLists()
    {
        return [];
    }

    /// <summary>Returns nested lists as child elements.</summary>
    public IEnumerable<IElement> GetChildren() => GetNestedLists();

    /// <summary>
    /// Wraps the subclass item rendering in an <c>L</c> structure element when tagging is
    /// active, so the outer container is present in the structure tree for both top-level
    /// and nested lists. Delegates actual content to <see cref="RenderItems"/>.
    /// </summary>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        TaggingContext? tagging = ctx.Tagging;
        if (tagging is not null)
        {
            tagging.Tree.Begin(SemanticRole.L);
            ctx.Canvas.BeginContainerContent("L");
        }
        try
        {
            RenderItems(ctx, contentWidth);
        }
        finally
        {
            if (tagging is not null)
            {
                ctx.Canvas.EndMarkedContent();
                tagging.Tree.End();
            }
        }
    }

    /// <summary>
    /// Renders the list items. Implemented by <see cref="OrderedList"/> and
    /// <see cref="UnorderedList"/>; called from <see cref="RenderContent"/> which
    /// handles the outer <c>L</c> structure-element wrapper.
    /// </summary>
    protected abstract void RenderItems(RenderContext ctx, float contentWidth);

    /// <summary>
    /// Measures the height of list items, accounting for item spacing.
    /// </summary>
    /// <param name="items">The list items to measure</param>
    /// <param name="itemWidth">The width available for each item</param>
    /// <returns>The total height of all items including spacing</returns>
    protected float MeasureItemsHeight(IEnumerable<string> items, float itemWidth)
    {
        float totalHeight = 0;

        foreach (string item in items)
        {
            IList<string> lines = ComputeLines(item, itemWidth);
            totalHeight += lines.Count * ComputedLineHeight + ItemSpacing;
        }

        // Remove spacing after last item
        if (totalHeight > 0) totalHeight -= ItemSpacing;

        return totalHeight;
    }

    /// <summary>
    /// Measures text items and nested lists using the shared list indentation rules.
    /// </summary>
    /// <param name="items">The list's own text items.</param>
    /// <param name="nestedLists">The nested lists rendered beneath the items.</param>
    /// <param name="contentWidth">The width available to the list, before indentation.</param>
    /// <returns>The combined height of the items and nested lists.</returns>
    protected float MeasureListHeight(
        IEnumerable<string> items,
        IEnumerable<ListElementBase> nestedLists,
        float contentWidth)
    {
        float totalHeight = MeasureItemsHeight(items, contentWidth - Indent);

        foreach (ListElementBase nestedList in nestedLists)
            totalHeight += nestedList.MeasureHeight(contentWidth - Indent) + ItemSpacing;

        return totalHeight;
    }

    /// <summary>
    /// Renders nested lists at the shared indentation, restoring the parent cursor
    /// after each one.
    /// </summary>
    /// <param name="ctx">The active render context.</param>
    /// <param name="nestedLists">The nested lists to render.</param>
    /// <param name="contentWidth">The width available to the list, before indentation.</param>
    protected void RenderNestedLists(
        RenderContext ctx,
        IEnumerable<ListElementBase> nestedLists,
        float contentWidth)
    {
        foreach (ListElementBase nestedList in nestedLists)
        {
            float originalX = ctx.CurrentX;
            ctx.CurrentX += Indent;

            nestedList.Render(ctx, contentWidth - Indent);

            ctx.CurrentX = originalX;
            ctx.CurrentY += ItemSpacing;
        }
    }

    /// <summary>
    /// Checks if a new page is needed before rendering content.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="currentY">The current Y position (ref)</param>
    /// <param name="lineHeight">The height of the content to render</param>
    /// <returns>True if a new page was created, false otherwise</returns>
    protected static bool CheckPageBreak(RenderContext ctx, ref float currentY, float lineHeight)
    {
        if (!(currentY + lineHeight > ctx.MaxY) || !(currentY > ctx.ContentTop))
        {
            return false;
        }
        ctx.NewPage();
        currentY = ctx.CurrentY;
        return true;
    }

    /// <summary>
    /// Renders one list item with tagged <c>LI &gt; Lbl + LBody</c> structure when tagging
    /// is active, or the original untagged layout otherwise.
    ///
    /// Tagged path emits the marker only on the first wrapped line (inside <c>Lbl</c>);
    /// continuation lines are text-only inside separate <c>LBody</c> marked-content sequences
    /// that all share a single <c>LBody</c> StructElem.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="lines">The lines of text for this item</param>
    /// <param name="localY">The local Y position (will be updated)</param>
    /// <param name="renderMarker">Action to render the list marker (number or bullet)</param>
    /// <param name="markerWidth">The width of the marker</param>
    protected void RenderListItem(
        RenderContext ctx,
        IList<string> lines,
        ref float localY,
        Action<float, float> renderMarker,
        float markerWidth)
    {
        if (lines.Count == 0) return;

        TaggingContext? tagging = ctx.Tagging;

        // Open LI container around the whole item when tagging
        if (tagging is not null)
        {
            tagging.Tree.Begin(SemanticRole.LI);
            ctx.Canvas.BeginContainerContent("LI");
        }

        try
        {
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                CheckPageBreak(ctx, ref localY, ComputedLineHeight);

                float markerX = ctx.CurrentX;
                float textX = ctx.CurrentX + markerWidth + 5;

                if (tagging is null)
                {
                    // Non-tagged: original behaviour - marker drawn on every wrapped line
                    renderMarker(markerX, localY);
                    ctx.Canvas.DrawText(textX, localY, line, Color, FontSize, FontName, TextAlignment.Left);
                }
                else
                {
                    if (i == 0)
                    {
                        // Lbl - list item label (drawn once, on the first line only)
                        tagging.Tree.Begin(SemanticRole.Lbl);
                        ctx.Canvas.BeginMarkedContent("Lbl");
                        renderMarker(markerX, localY);
                        ctx.Canvas.EndMarkedContent();
                        tagging.Tree.End();

                        // Open the LBody StructElem; individual marked-content sequences
                        // below will each add their own MCID to this single StructElem.
                        tagging.Tree.Begin(SemanticRole.LBody);
                    }

                    // Each wrapped line gets its own MCID, all recorded under LBody
                    ctx.Canvas.BeginMarkedContent("LBody");
                    ctx.Canvas.DrawText(textX, localY, line, Color, FontSize, FontName, TextAlignment.Left);
                    ctx.Canvas.EndMarkedContent();
                }

                localY += ComputedLineHeight;
            }

            if (tagging is not null)
                tagging.Tree.End(); // end LBody
        }
        finally
        {
            if (tagging is not null)
            {
                ctx.Canvas.EndMarkedContent(); // end LI BMC
                tagging.Tree.End(); // end LI StructElem
            }
        }

        localY += ItemSpacing;
    }
}
