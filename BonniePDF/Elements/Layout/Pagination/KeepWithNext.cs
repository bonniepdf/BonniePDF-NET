using System.Collections.Generic;

namespace BonniePDF;

internal static class KeepWithNext
{
    public static float Height(IList<IElement> elements, int index, float width, float spacing = 0)
    {
        float height = 0;
        int next = index;
        for (; next + 1 < elements.Count && elements[next] is ElementBase { KeepWithNext: true }; next++)
            height += elements[next].MeasureHeight(width) + spacing;

        if (next == index) return 0;
        IElement following = elements[next];
        if (following is PageBreak) return 0;
        return height + (following is ElementBase element ? element.MinimumStartHeight(width) : following.MeasureHeight(width));
    }
}
