using System.Collections.Generic;
using BonniePDF.Fluent;

namespace BonniePDF;

/// <summary>
/// What a pagination primitive decides to do with its child before rendering it
/// on the current page.
/// </summary>
public enum PaginationDecision
{
    /// <summary>Render the child here, on this page.</summary>
    Render,

    /// <summary>
    /// Nothing can be rendered on this page; move to a fresh one and ask again.
    /// The pipeline turns this into <see cref="RenderStatus.WrapRequired"/>.
    /// </summary>
    Wrap,

    /// <summary>The child does not appear on this page at all; report completion.</summary>
    Skip,
}

/// <summary>
/// Shared behaviour for the single-child pagination primitives.
/// </summary>
/// <remarks>
/// Each primitive is a thin decorator: it decides <i>whether</i> and <i>where</i>
/// its child renders, and never draws anything itself. The two hooks subclasses
/// implement — <see cref="Decide"/> before the child renders and
/// <see cref="Settle"/> after — are enough to express every primitive in the
/// catalogue, which is what keeps them consistent with each other and in step
/// with the TypeScript port.
/// <para>
/// Margins are applied here rather than by <see cref="ElementBase.Render"/>
/// because a <see cref="PaginationDecision.Wrap"/> has to be taken <i>before</i>
/// the cursor moves: a primitive that pushed its top margin onto a page it then
/// abandoned would leave a phantom gap at the foot of that page.
/// </para>
/// </remarks>
public abstract class PaginationWrapperBase : ElementBase, IContentContainer, IStatefulElement
{
    /// <summary>The element whose pagination this primitive governs.</summary>
    public IElement? Child { get; set; }

    /// <summary>
    /// True once the child has rendered part of itself and is awaiting a
    /// continuation on the next page.
    /// </summary>
    private bool _pending;

    /// <summary>Returns the wrapped child, or nothing when there is none.</summary>
    public IEnumerable<IElement> GetChildren() => Child is null ? [] : [Child];

    /// <summary>
    /// Decides what to do with the child on the page the cursor is currently on.
    /// Called once per page attempt, before any margin is applied.
    /// </summary>
    protected abstract PaginationDecision Decide(RenderContext ctx, float contentWidth);

    /// <summary>
    /// Maps the child's own status onto the status this primitive reports.
    /// The default passes it through unchanged; primitives that refuse to
    /// continue on another page (<see cref="ShowEntire"/>, <see cref="StopPaging"/>)
    /// collapse it to <see cref="RenderStatus.Complete"/> here.
    /// </summary>
    protected virtual RenderStatus Settle(RenderStatus status) => status;

    /// <summary>
    /// Runs once the child has rendered and its capture node has closed.
    /// </summary>
    /// <remarks>
    /// The hook exists so a primitive can adjust the flow cursor without
    /// falsifying the layout snapshot: <see cref="Unconstrained"/> rewinds the
    /// cursor here, so the captured rect still records where the child drew.
    /// </remarks>
    protected virtual void AfterChild(RenderContext ctx)
    {
        // Nothing by default.
    }

    /// <summary>
    /// Decides whether the child renders on the current page, then renders it and maps its status to the status
    /// this wrapper reports.
    /// </summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        if (!_pending)
        {
            PaginationDecision decision = Decide(ctx, GetContentWidth(width));
            if (decision == PaginationDecision.Wrap) return RenderStatus.WrapRequired;
            if (decision == PaginationDecision.Skip) return RenderStatus.Complete;
        }

        float originalX = ctx.CurrentX;

        if (!_pending) ctx.CurrentY += Margin.Top;
        ctx.CurrentX += Margin.Left;

        float contentWidth = GetContentWidth(width);
        RenderStatus status;
        try
        {
            OpenChildNode(ctx, contentWidth);
            RenderStatus raw = DriveChild(ctx, contentWidth);
            // Settle before closing the capture node: a primitive that refuses
            // to continue has finished with its child, so the node must close
            // complete or the recorder drops its rect entirely.
            status = Settle(raw);
            CloseChildNode(ctx, status == RenderStatus.Complete);
            AfterChild(ctx);
        }
        finally
        {
            ctx.CurrentX = originalX;
        }

        if (status == RenderStatus.Complete)
        {
            ctx.CurrentY += Margin.Bottom;
            _pending = false;
            ResetChildState();
        }
        else
        {
            _pending = true;
        }

        return status;
    }

    /// <summary>
    /// Renders one page's worth of the child, honouring its statefulness.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>VerticalStack</c>'s child-driving loop, including the "advance
    /// the cursor ourselves if the child did not" rule that atomic elements rely
    /// on. Capture bookkeeping is the caller's job, so the node can be closed
    /// against the <i>settled</i> status rather than the child's raw one.
    /// </remarks>
    protected virtual RenderStatus DriveChild(RenderContext ctx, float contentWidth)
    {
        if (Child is null) return RenderStatus.Complete;

        // The wrapper declares no role of its own, so the child's semantics (a heading, a
        // figure, a table) must be opened here, as every other layout container does for its
        // children. Without it a wrapped heading drops out of the structure tree.
        ChildTagToken token = ChildTagging.BeginChildTag(ctx, Child);
        try
        {
            if (Child is IStatefulElement stateful) return stateful.RenderStateful(ctx, contentWidth);

            float before = ctx.CurrentY;
            Child.Render(ctx, contentWidth);
            if (ctx.CurrentY == before) ctx.CurrentY += Child.MeasureHeight(contentWidth);
            return RenderStatus.Complete;
        }
        finally
        {
            ChildTagging.EndChildTag(ctx, token);
        }
    }

    private void OpenChildNode(RenderContext ctx, float contentWidth)
    {
        if (Child is null) return;
        ctx.LayoutRecorder?.OpenNode(
            Child,
            LayoutRecorder.CaptureTypeOf(Child),
            LayoutRecorder.CaptureNameOf(Child),
            ctx.CurrentPageIndex,
            ctx.CurrentX,
            ctx.CurrentY,
            contentWidth);
    }

    private void CloseChildNode(RenderContext ctx, bool complete)
    {
        if (Child is null) return;
        ctx.LayoutRecorder?.CloseNode(Child, ctx.CurrentPageIndex, ctx.CurrentY, complete);
    }

    /// <summary>Clears any continuation state the child accumulated, as the pipeline does.</summary>
    protected void ResetChildState()
    {
        if (Child is IStatefulElement stateful) stateful.ResetState();
    }

    /// <summary>Clears the continuation state of this wrapper and of its child.</summary>
    public void ResetState()
    {
        _pending = false;
        ResetChildState();
    }

    /// <inheritdoc />
    protected override float MeasureContentHeight(float contentWidth) =>
        Child?.MeasureHeight(contentWidth) ?? 0;

    /// <inheritdoc />
    protected override float GetPreferredContentWidth() => Child?.GetPreferredWidth() ?? 0;

    /// <summary>
    /// Atomic render path, used where the caller does not drive statuses — page
    /// templates and the page background / foreground slots.
    /// </summary>
    /// <remarks>
    /// The decision still applies, so a conditional primitive suppresses its
    /// child there too; <see cref="PaginationDecision.Wrap"/> degrades to
    /// <see cref="PaginationDecision.Render"/>, because a template has no flow to
    /// break. Note that a template's <i>reserved height</i> is measured without a
    /// page context, so a conditional inside one reserves its child's height on
    /// every page even where the child is suppressed — prefer returning
    /// <see langword="null"/> from the template function itself when the
    /// reservation matters.
    /// </remarks>
    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        if (Decide(ctx, contentWidth) == PaginationDecision.Skip) return;
        OpenChildNode(ctx, contentWidth);
        DriveChild(ctx, contentWidth);
        CloseChildNode(ctx, complete: true);
        AfterChild(ctx);
    }

    /// <summary>Body height still available below the cursor on the current page.</summary>
    protected static float RemainingHeight(RenderContext ctx) => ctx.MaxY - ctx.CurrentY;

    /// <summary>True when content already occupies the current page, so a break gains space.</summary>
    protected static bool CanGainSpaceByBreaking(RenderContext ctx) => ctx.CurrentY > ctx.ContentTop;
}
