using System;
using BonniePDF.Drawing;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Drawing;

/// <summary>
/// A leaf element that exposes the raw ICanvas for freeform drawing.
/// The handler receives (canvas, width, height) and can use any ICanvas method
/// including the path-building, transform, and clipping APIs.
/// </summary>
public class CanvasElement : ElementBase, IRoleProvider, IAltTextProvider
{
    private readonly Action<ICanvas, float, float> _drawHandler;

    /// <summary>
    /// Creates a new canvas element with the given draw handler.
    /// </summary>
    /// <param name="drawHandler">Callback that receives (canvas, contentWidth, canvasHeight).</param>
    public CanvasElement(Action<ICanvas, float, float> drawHandler)
    {
        _drawHandler = drawHandler ?? throw new ArgumentNullException(nameof(drawHandler));
    }

    /// <summary>Alt text for the canvas content (makes it a Figure instead of Artifact).</summary>
    public string AltText { get; set; } = string.Empty;

    /// <summary>When true, tagged as /Artifact regardless of AltText.</summary>
    public bool IsDecorative { get; set; }

    /// <inheritdoc />
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

    /// <summary>
    /// Fixed width for the canvas element in points. <c>0</c> (the default)
    /// fills the available content width; a positive value fixes the width and
    /// is clamped to the space available.
    /// </summary>
    public float CanvasWidth { get; set; }

    /// <summary>Fixed height for the canvas element in points.</summary>
    public float CanvasHeight { get; set; } = 100f;

    /// <inheritdoc />
    protected override float GetPreferredContentWidth() => CanvasWidth;

    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => CanvasHeight;

    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ICanvas canvas = ctx.GetCanvas(ctx.CurrentX, ctx.CurrentY);

        // CanvasWidth 0 means "fill what is available"; a positive value fixes
        // the width but never overflows the column.
        float drawWidth = CanvasWidth > 0 ? Math.Min(CanvasWidth, contentWidth) : contentWidth;

        canvas.SaveState();
        canvas.BeginPath();
        canvas.AddRect(0, 0, drawWidth, CanvasHeight);
        canvas.ClipToCurrentPath();

        try
        {
            _drawHandler(canvas, drawWidth, CanvasHeight);
        }
        finally
        {
            canvas.RestoreState();
        }

        ctx.CurrentY += CanvasHeight;
    }
}
