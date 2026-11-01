// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using BonniePDF.Drawing;

namespace BonniePDF;

public abstract partial class ChartBase : ElementBase, IRoleProvider, IAltTextProvider
{
    /// <summary>
    /// The structure role in a tagged PDF: <c>Figure</c> when the element has alternative text, otherwise
    /// <c>Artifact</c>. It is always <c>Artifact</c> when <see cref="IsDecorative"/> is set.
    /// </summary>
    public SemanticRole SemanticRole =>
        IsDecorative || string.IsNullOrEmpty(AltText)
            ? SemanticRole.Artifact
            : SemanticRole.Figure;

    /// <summary>
    /// When true, the chart will stretch to fill the container width.
    /// The Width property will be set automatically by the document layout engine.
    /// </summary>
    public bool StretchToFit { get; set; }

    /// <inheritdoc />
    protected override float GetPreferredContentWidth() => Width;

    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) => Height;

    /// <inheritdoc />
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (StretchToFit) Width = contentWidth;

        ICanvas canvas = ctx.GetCanvas(ctx.CurrentX, ctx.CurrentY);
        Draw(canvas);
        ctx.FlushExternalCanvas(canvas);
        ctx.CurrentY += Height;
    }
}
