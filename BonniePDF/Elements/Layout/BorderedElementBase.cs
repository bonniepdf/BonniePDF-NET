using BonniePDF.Core;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// Base class for elements with borders and background colors.
/// Provides common functionality for drawing borders and backgrounds.
/// </summary>
public abstract class BorderedElementBase : ElementBase, IBackgroundColor
{
    /// <summary>
    /// Border widths for each side of the element
    /// </summary>
    public Border Border { get; set; } = new(0);

    /// <summary>
    /// Border colors for each side of the element
    /// </summary>
    public BorderColors BorderColors { get; set; }

    /// <summary>
    /// Background color for this element (null = transparent)
    /// </summary>
    public Color? BackgroundColor { get; set; }

    /// <summary>
    /// Corner radius for rounded rectangles. 0 = sharp corners (default).
    /// </summary>
    public float CornerRadius { get; set; }

    /// <summary>
    /// Draws a background rectangle with the specified color.
    /// Uses rounded corners when CornerRadius > 0.
    /// </summary>
    protected void DrawBackground(RenderContext ctx, float x, float y, float width, float height, Color color)
    {
        float pdfY = ctx.PageSize.Height - y - height;
        // Decorative fill — tag as an artifact so tagged (PDF/UA) output does not
        // leave it as untagged content. No-op wrapper for non-tagged documents.
        ctx.Canvas.BeginArtifact();
        try
        {
            if (CornerRadius > 0)
            {
                PdfDrawing.FillRoundedRect(ctx.CurrentStream, x, pdfY, width, height, CornerRadius, color);
            }
            else
            {
                PdfOperators.SaveState(ctx.CurrentStream);
                PdfOperators.SetFillColor(ctx.CurrentStream, color);
                PdfOperators.RectangleFill(ctx.CurrentStream, x, pdfY, width, height);
                PdfOperators.RestoreState(ctx.CurrentStream);
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

        float pdfY = ctx.PageSize.Height - y - height;

        // Decorative strokes — tag as an artifact (no-op for non-tagged output).
        ctx.Canvas.BeginArtifact();
        try
        {
            // Rounded borders: use a single rounded rect stroke (uniform border only)
            if (CornerRadius > 0)
            {
                float strokeWidth = border.Top > 0 ? border.Top : border.Left > 0 ? border.Left : border.Bottom > 0 ? border.Bottom : border.Right;
                Color strokeColor = borderColors.Top ?? borderColors.Left ?? borderColors.Bottom ?? borderColors.Right ?? Color.Black;
                PdfDrawing.StrokeRoundedRect(ctx.CurrentStream, x, pdfY, width, height, CornerRadius, strokeColor, strokeWidth);
                return;
            }

            PdfOperators.SaveState(ctx.CurrentStream);

            // Draw left border
            if (border.Left > 0 && borderColors.Left.HasValue)
                DrawLine(ctx, x, pdfY, x, pdfY + height, border.Left, borderColors.Left.Value);

            // Draw right border
            if (border.Right > 0 && borderColors.Right.HasValue)
                DrawLine(ctx, x + width, pdfY, x + width, pdfY + height, border.Right, borderColors.Right.Value);

            // Draw top border
            if (border.Top > 0 && borderColors.Top.HasValue)
                DrawLine(ctx, x, pdfY + height, x + width, pdfY + height, border.Top, borderColors.Top.Value);

            // Draw bottom border
            if (border.Bottom > 0 && borderColors.Bottom.HasValue)
                DrawLine(ctx, x, pdfY, x + width, pdfY, border.Bottom, borderColors.Bottom.Value);

            PdfOperators.RestoreState(ctx.CurrentStream);
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
        PdfOperators.SetStrokeColor(ctx.CurrentStream, color);
        PdfOperators.SetLineWidth(ctx.CurrentStream, width);
        PdfOperators.StrokeLine(ctx.CurrentStream, x1, y1, x2, y2);
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
