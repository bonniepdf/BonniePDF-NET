using System.Collections.Generic;

namespace BonniePDF;

/// <summary>
/// Resolves the inherited content direction through an element and its descendants.
/// </summary>
/// <remarks>
/// This is the first walk of the tree in every generation, so it is also where a tree that contains itself is
/// caught: <c>ancestors</c> holds the elements on the path from the root to the one being visited, and meeting one
/// of them again is a cycle. Without the check the walk recurses until the stack overflows.
/// </remarks>
internal static class ContentDirectionResolver
{
    internal static void Apply(IElement? element, ContentDirection inherited) =>
        Apply(element, inherited, new HashSet<IElement>(ReferenceEqualityComparer.Instance));

    private static void Apply(IElement? element, ContentDirection inherited, HashSet<IElement> ancestors)
    {
        if (element is null) return;
        if (ancestors.Contains(element)) throw new ElementCycleException(LayoutRecorder.CaptureTypeOf(element));
        if (element is ElementBase node)
        {
            node.InheritedContentDirection = inherited;
            inherited = node.EffectiveContentDirection;
        }
        ancestors.Add(element);
        try
        {
            foreach (var child in element.GetChildren()) Apply(child, inherited, ancestors);
        }
        finally
        {
            ancestors.Remove(element);
        }
    }
}
