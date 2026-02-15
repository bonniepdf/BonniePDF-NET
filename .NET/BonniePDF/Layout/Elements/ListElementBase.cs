using System;
using System.Collections.Generic;
using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

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
            List<string> lines = ComputeLines(item, itemWidth);
            totalHeight += lines.Count * LineHeight + ItemSpacing;
        }

        // Remove spacing after last item
        if (totalHeight > 0) totalHeight -= ItemSpacing;

        return totalHeight;
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
        if (!(currentY + lineHeight > ctx.MaxY) || !(currentY > ctx.Margin))
        {
            return false;
        }
        ctx.NewPage();
        currentY = ctx.CurrentY;
        return true;
    }

    /// <summary>
    /// Renders a list item with proper pagination handling.
    /// </summary>
    /// <param name="ctx">The render context</param>
    /// <param name="lines">The lines of text for this item</param>
    /// <param name="localY">The local Y position (will be updated)</param>
    /// <param name="renderMarker">Action to render the list marker (number or bullet)</param>
    /// <param name="markerWidth">The width of the marker</param>
    protected void RenderListItem(
        RenderContext ctx,
        List<string> lines,
        ref float localY,
        Action<float, float> renderMarker,
        float markerWidth)
    {
        foreach (string line in lines)
        {
            // Check if we need a new page
            CheckPageBreak(ctx, ref localY, LineHeight);

            // Render marker at the left edge
            float markerX = ctx.CurrentX + Margin.Left;
            renderMarker(markerX, localY);

            // Render text after the marker
            float textX = ctx.CurrentX + Margin.Left + markerWidth + 5;
            ctx.Canvas.DrawText(textX, localY, line, TextColor, FontSize, FontName, TextAlignment.Left);

            localY += LineHeight;
        }

        // Add spacing after the item
        localY += ItemSpacing;
    }
}