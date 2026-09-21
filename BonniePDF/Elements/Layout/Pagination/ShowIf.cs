using System;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout.Pagination;

/// <summary>
/// Renders its child only on the pages a predicate accepts.
/// </summary>
/// <remarks>
/// The predicate sees a <see cref="PageContext"/> — the page number and whether
/// this is the first or last page — and nothing else, so the decision stays a
/// pure function of where the element landed. That is what keeps layout
/// deterministic and what lets the same document produce the same pages in both
/// implementations.
/// <para>
/// Most useful inside a page template, where the same tree is built for every
/// page:
/// </para>
/// <code>
/// doc.Settings.HeaderTemplate = _ =&gt; new ShowIf(p =&gt; !p.IsFirstPage, runningHead);
/// </code>
/// <para>
/// <see cref="PageContext.IsLastPage"/> is only reliable in the second layout
/// pass; on the first pass the engine does not yet know the total page count.
/// </para>
/// </remarks>
public class ShowIf : PaginationWrapperBase
{
    /// <summary>Creates a show-if with no predicate and no child.</summary>
    public ShowIf() { }

    /// <summary>Creates a show-if governed by <paramref name="predicate"/>.</summary>
    public ShowIf(Func<PageContext, bool> predicate) => Predicate = predicate;

    /// <summary>Creates a show-if around <paramref name="child"/>.</summary>
    public ShowIf(Func<PageContext, bool> predicate, IElement child)
    {
        Predicate = predicate;
        Child = child;
    }

    /// <summary>
    /// The page test. <see langword="null"/> means "always render", so a
    /// <c>ShowIf</c> with no predicate is a transparent wrapper rather than an
    /// element that silently disappears.
    /// </summary>
    public Func<PageContext, bool>? Predicate { get; set; }

    /// <inheritdoc />
    protected override PaginationDecision Decide(RenderContext ctx, float contentWidth)
    {
        if (Predicate is null) return PaginationDecision.Render;
        return Predicate(PageContext.From(ctx)) ? PaginationDecision.Render : PaginationDecision.Skip;
    }
}
