
using System;

namespace BonniePDF;

/// <summary>
/// Base class for elements with borders and background colors.
/// Provides common functionality for drawing borders and backgrounds.
/// </summary>
public abstract class BorderedElementBase : ElementBase, IBackgroundColor
{
    // Border, colours, background and radius, shared with every element that has the same values.
    private BoxOptions _box = BoxOptions.Default;

    /// <summary>
    /// Border widths for each side of the element
    /// </summary>
    public Border Border
    {
        get => _box.Border;
        set => _box = _box.With(value, static (o, v) => o with { Border = v });
    }

    /// <summary>
    /// Border colors for each side of the element
    /// </summary>
    public BorderColors BorderColors
    {
        get => _box.BorderColors;
        set => _box = _box.With(value, static (o, v) => o with { BorderColors = v });
    }

    /// <summary>
    /// Background color for this element (null = transparent)
    /// </summary>
    public Color? BackgroundColor
    {
        get => _box.BackgroundColor;
        set => _box = _box.With(value, static (o, v) => o with { BackgroundColor = v });
    }

    /// <summary>
    /// Corner radius for rounded rectangles. 0 = sharp corners (default).
    /// </summary>
    public float CornerRadius
    {
        get => _box.CornerRadius;
        set => _box = _box.With(value, static (o, v) => o with { CornerRadius = v });
    }

    /// <summary>
    /// Draws a background rectangle with the specified color.
    /// Uses rounded corners when CornerRadius > 0.
    /// </summary>
    protected void DrawBackground(RenderContext ctx, float x, float y, float width, float height, Color color)
    {
        // Decorative fill - tag as an artifact so tagged (PDF/UA) output does not
        // leave it as untagged content. No-op wrapper for non-tagged documents.
        ctx.Canvas.BeginArtifact();
        try
        {
            if (CornerRadius > 0)
            {
                ctx.Canvas.FillRoundedRect(x, y, width, height, CornerRadius, color);
            }
            else
            {
                ctx.Canvas.FillRect(x, y, width, height, color);
            }
        }
        finally
        {
            ctx.Canvas.EndArtifact();
        }
    }

    /// <summary>
    /// Draws borders around a rectangle using the specified border widths and colors.
    /// When CornerRadius > 0, draws a single rounded stroke instead of individual sides.
    /// Otherwise each side is drawn independently, allowing for different widths and colors per side.
    /// </summary>
    protected void DrawBorder(RenderContext ctx, float x, float y, float width, float height, Border border,
        BorderColors borderColors)
    {
        if (!border.HasAnyBorder) return;

        // Decorative strokes - tag as an artifact (no-op for non-tagged output).
        ctx.Canvas.BeginArtifact();
        try
        {
            // Rounded borders: use a single rounded rect stroke (uniform border only)
            if (CornerRadius > 0)
            {
                float strokeWidth = MathF.Max(MathF.Max(border.Top, border.Left), MathF.Max(border.Bottom, border.Right));
                Color strokeColor = borderColors.Top ?? borderColors.Left ?? borderColors.Bottom ?? borderColors.Right ?? Color.Black;
                ctx.Canvas.StrokeRoundedRect(x, y, width, height, CornerRadius, strokeColor, strokeWidth);
                return;
            }

            // Draw left border
            if (border.Left > 0 && borderColors.Left.HasValue)
                DrawLine(ctx, x, y, x, y + height, border.Left, borderColors.Left.Value);

            // Draw right border
            if (border.Right > 0 && borderColors.Right.HasValue)
                DrawLine(ctx, x + width, y, x + width, y + height, border.Right, borderColors.Right.Value);

            // Draw top border
            if (border.Top > 0 && borderColors.Top.HasValue)
                DrawLine(ctx, x, y, x + width, y, border.Top, borderColors.Top.Value);

            // Draw bottom border
            if (border.Bottom > 0 && borderColors.Bottom.HasValue)
                DrawLine(ctx, x, y + height, x + width, y + height, border.Bottom, borderColors.Bottom.Value);
        }
        finally
        {
            ctx.Canvas.EndArtifact();
        }
    }

    /// <summary>
    /// Draws a single line with the specified width and color.
    /// </summary>
    protected static void DrawLine(RenderContext ctx, float x1, float y1, float x2, float y2, float width, Color color)
    {
        ctx.Canvas.DrawLine(x1, y1, x2, y2, color, width);
    }

    /// <summary>
    /// Checks if this element has any border to draw.
    /// </summary>
    protected bool HasAnyBorder()
    {
        return Border.Left > 0 || Border.Right > 0 || Border.Top > 0 || Border.Bottom > 0;
    }

    /// <summary>
    /// Calculates the total width occupied by horizontal borders (left + right).
    /// </summary>
    protected float GetHorizontalBorderWidth()
    {
        return Border.Left + Border.Right;
    }

    /// <summary>
    /// Calculates the total height occupied by vertical borders (top + bottom).
    /// </summary>
    protected float GetVerticalBorderWidth()
    {
        return Border.Top + Border.Bottom;
    }
}
