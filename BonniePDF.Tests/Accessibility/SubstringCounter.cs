namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// Tiny helpers shared between tagged-document tests.
/// </summary>
internal static class SubstringCounter
{
    public static int Count(string source, string needle)
    {
        int count = 0;
        int idx = 0;
        while ((idx = source.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += needle.Length;
        }
        return count;
    }
}
