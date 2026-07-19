using System.Linq;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Fluent;

namespace BonniePDF.Rendering;

/// <summary>
/// Shared helpers for two-pass Table-of-Contents resolution. A
/// <see cref="VisualToc"/> renders before the headings it references, so its
/// page numbers can only be resolved after a first (throw-away) layout pass.
/// Both the PDF serializer and the SVG exporter consult these helpers so they
/// make the identical decision about whether a second pass is required.
/// </summary>
internal static class TocLayout
{
    /// <summary>
    /// Returns <c>true</c> when the document contains a <see cref="VisualToc"/>
    /// anywhere in its element tree, which requires a two-pass layout to resolve
    /// heading page numbers before the TOC is drawn.
    /// </summary>
    public static bool RequiresTwoPass(Document document)
        => document.Pages.Any(page => page.Elements.Any(ContainsVisualToc));

    /// <summary>
    /// Recursively checks whether an element tree contains a
    /// <see cref="VisualToc"/>, descending into layout containers
    /// (<see cref="VerticalStack"/>, <see cref="HorizontalStack"/>,
    /// <see cref="Container"/>) and single-child fluent content slots.
    /// </summary>
    public static bool ContainsVisualToc(IElement element)
    {
        if (element is VisualToc) return true;
        if (element is VerticalStack vs) return vs.Children.Any(ContainsVisualToc);
        if (element is HorizontalStack hs) return hs.Children.Any(ContainsVisualToc);
        if (element is Container c && c.Child != null) return ContainsVisualToc(c.Child);
        // IContentContainer covers fluent API slots (ContentSlot) that wrap a single child.
        if (element is IContentContainer cc && cc.Child != null) return ContainsVisualToc(cc.Child);
        return false;
    }
}
