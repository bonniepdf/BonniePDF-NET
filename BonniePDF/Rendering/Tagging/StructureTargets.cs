using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Chooses the structure element an in-document destination targets (spec 04 §3.4):
/// the element that registered the anchor, when the destination names one and that
/// element was tagged; otherwise an element on the target page.
/// </summary>
internal static class StructureTargets
{
    /// <summary>The element for a destination to <paramref name="pageIndex"/>, naming <paramref name="anchor"/> when it has one.</summary>
    public static StructureElement Resolve(TaggingContext tagging, string? anchor, int pageIndex)
    {
        if (anchor != null &&
            tagging.AnchorOwners.TryGetValue(anchor, out IElement? owner) &&
            tagging.ElementStructure.TryGetValue(owner, out StructureElement? element))
        {
            return element;
        }
        return ForPage(tagging.Tree.Root, pageIndex);
    }

    /// <summary>
    /// In reading order (the tree's pre-order), the first element whose first marked
    /// content is on the page; failing that, the first with any marked content on it;
    /// failing that, the document element.
    /// </summary>
    public static StructureElement ForPage(StructureElement root, int pageIndex)
    {
        StructureElement? continuing = null;
        var stack = new Stack<StructureElement>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            StructureElement element = stack.Pop();
            IList<int> pages = element.McIdPages;
            if (pages.Count > 0)
            {
                if (pages[0] == pageIndex) return element;
                if (continuing == null && pages.Contains(pageIndex)) continuing = element;
            }
            for (int i = element.Children.Count - 1; i >= 0; i--) stack.Push(element.Children[i]);
        }
        return continuing ?? root;
    }
}
