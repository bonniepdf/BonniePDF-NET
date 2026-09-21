using System.Runtime.InteropServices;
using BonniePDF.Rendering;

namespace BonniePDF.Elements.Layout.Pagination;

/// <summary>
/// The page facts a conditional pagination primitive decides on.
/// </summary>
/// <remarks>
/// Deliberately small and stateless: a predicate that can only see these three
/// values is a pure function of where the element landed, which is what keeps
/// layout deterministic (spec "Layout &amp; Pagination" §10) and keeps the two
/// implementations in step.
/// </remarks>
/// <param name="PageNumber">1-based number of the page the element is being rendered on.</param>
/// <param name="IsFirstPage">True on the document's first page.</param>
/// <param name="IsLastPage">
/// True on the document's last page. Only meaningful during the second layout
/// pass; on the first pass the total page count is not yet known and this
/// reports the engine's running view.
/// </param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct PageContext(int PageNumber, bool IsFirstPage, bool IsLastPage)
{
    /// <summary>Builds the context for the page currently being rendered.</summary>
    internal static PageContext From(RenderContext ctx) =>
        new(ctx.CurrentPageIndex + 1, ctx.CurrentPageIndex == 0, ctx.IsLastPage);
}
