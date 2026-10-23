using System;
using BonniePDF.Drawing;

namespace BonniePDF;

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

    /// <summary>
    /// The structure role in a tagged PDF: <c>Figure</c> when the element has alternative text, otherwise
    /// <c>Artifact</c>. It is always <c>Artifact</c> when <see cref="IsDecorative"/> is set.
    /// </summary>
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

    /// <summary>
    /// Fixed width for the canvas element in points. <c>0</c> (the default)
    /// fills the available content width; a positive value fixes the width and
    /// is clamped to the space available.
    /// </summary>
    public float DrawWidth { get; set; }

    /// <summary>Fixed height for the canvas element in points.</summary>
    public float DrawHeight { get; set; } = 100f;

    /// <inheritdoc />
    protected override float GetPreferredContentWidth() => DrawWidth;

    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => DrawHeight;

    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        ICanvas canvas = ctx.GetCanvas(ctx.CurrentX, ctx.CurrentY);

        // DrawWidth 0 means "fill what is available"; a positive value fixes
        // the width but never overflows the column.
        float drawWidth = DrawWidth > 0 ? Math.Min(DrawWidth, contentWidth) : contentWidth;

        canvas.SaveState();
        canvas.BeginPath();
        canvas.AddRect(0, 0, drawWidth, DrawHeight);
        canvas.ClipToCurrentPath();

        try
        {
            _drawHandler(canvas, drawWidth, DrawHeight);
        }
        finally
        {
            canvas.RestoreState();
        }

        ctx.FlushExternalCanvas(canvas);
        ctx.CurrentY += DrawHeight;
    }
}
