using System.Linq;
using BonniePDF.Fluent;

namespace BonniePDF;

/// <summary>
/// Shared helpers for two-pass page-number resolution. A <see cref="VisualToc"/>,
/// <see cref="SectionLink"/>, or <see cref="PageNumberOf"/> can reference a page
/// that has not been laid out yet, so its page number can only be resolved after
/// a first (throw-away) layout pass. Both the PDF serializer and the SVG exporter
/// consult these helpers so they make the identical decision about whether a
/// second pass is required.
/// </summary>
internal static class TocLayout
{
    /// <summary>
    /// Returns <c>true</c> when the document contains any element whose
    /// page-number resolution depends on a completed layout (a
    /// <see cref="VisualToc"/>, <see cref="SectionLink"/>, or
    /// <see cref="PageNumberOf"/>), which requires a two-pass layout to seed the
    /// TOC registry before those elements are drawn.
    /// </summary>
    public static bool RequiresTwoPass(Document document)
        => document.Pages.Any(page => page.Elements.Any(ContainsTwoPassElement));

    /// <summary>
    /// Recursively checks whether an element tree contains an element that needs
    /// two-pass layout (a <see cref="VisualToc"/>, <see cref="SectionLink"/>, or
    /// <see cref="PageNumberOf"/>), descending into layout containers
    /// (<see cref="VerticalStack"/>, <see cref="HorizontalStack"/>,
    /// <see cref="Container"/>) and single-child fluent content slots.
    /// </summary>
    public static bool ContainsTwoPassElement(IElement element)
    {
        if (element is VisualToc or SectionLink or PageNumberOf) return true;
        if (element is VerticalStack vs) return vs.Children.Any(ContainsTwoPassElement);
        if (element is HorizontalStack hs) return hs.Children.Any(ContainsTwoPassElement);
        if (element is Container c && c.Child != null) return ContainsTwoPassElement(c.Child);
        // IContentContainer covers fluent API slots (ContentSlot) that wrap a single child.
        if (element is IContentContainer cc && cc.Child != null) return ContainsTwoPassElement(cc.Child);
        return false;
    }
}
