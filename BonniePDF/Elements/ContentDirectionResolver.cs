namespace BonniePDF;

internal static class ContentDirectionResolver
{
    internal static void Apply(IElement? element, ContentDirection inherited)
    {
        if (element is null) return;
        if (element is ElementBase node)
        {
            node.InheritedContentDirection = inherited;
            inherited = node.EffectiveContentDirection;
        }
        foreach (var child in element.GetChildren()) Apply(child, inherited);
    }
}
