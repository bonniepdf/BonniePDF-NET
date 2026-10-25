using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF;

/// <summary>
/// The face-matching rules shared by a document's fonts and the global font registrations: weight names in a
/// subfamily, and the CSS font-matching order that picks a face for a requested weight and style.
/// </summary>
internal static class FaceMatching
{
    private static readonly (string Name, int Weight)[] SubfamilyWeights =
    [
        ("thin", 100), ("extralight", 200), ("ultralight", 200), ("light", 300),
        ("regular", 400), ("book", 400), ("normal", 400), ("medium", 500),
        ("semibold", 600), ("demibold", 600), ("extrabold", 800), ("ultrabold", 800),
        ("bold", 700), ("black", 900), ("heavy", 900),
    ];

    /// <summary>The weight a subfamily name gives (<c>Bold Italic</c> is 700), or 400 when it names none.</summary>
    internal static int SubfamilyWeight(string subfamily)
    {
        string words = subfamily.ToLowerInvariant().Replace(" ", "", StringComparison.Ordinal)
            .Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        foreach ((string name, int weight) in SubfamilyWeights)
            if (words.Contains(name, StringComparison.Ordinal)) return weight;
        return 400;
    }

    /// <summary>Whether a subfamily name is italic or oblique.</summary>
    internal static bool SubfamilyItalic(string subfamily)
    {
        string lower = subfamily.ToLowerInvariant();
        return lower.Contains("italic", StringComparison.Ordinal) || lower.Contains("oblique", StringComparison.Ordinal);
    }

    /// <summary>
    /// The face for a requested weight and style among one family's faces: style first (an upright request is met
    /// by an italic-only family and the reverse), then weight in the CSS font-matching order. Among faces of the
    /// same weight and style the first in <paramref name="candidates"/> wins, so callers pass them in a stable order.
    /// </summary>
    internal static T? Match<T>(IReadOnlyList<T> candidates, int wanted, bool italic, Func<T, int> weightOf, Func<T, bool> italicOf)
        where T : class
    {
        if (candidates.Count == 0) return null;
        List<T> styled = candidates.Where(f => italicOf(f) == italic).ToList();
        List<T> pool = styled.Count > 0 ? styled : candidates.ToList();

        T? exact = pool.Find(f => weightOf(f) == wanted);
        if (exact is not null) return exact;

        T? heavier = pool.Where(f => weightOf(f) > wanted).OrderBy(weightOf).FirstOrDefault();
        T? lighter = pool.Where(f => weightOf(f) < wanted).OrderByDescending(weightOf).FirstOrDefault();
        if (wanted is >= 400 and <= 500)
            return pool.Where(f => weightOf(f) > wanted && weightOf(f) <= 500).OrderBy(weightOf).FirstOrDefault() ?? lighter ?? heavier;
        return wanted < 400 ? lighter ?? heavier : heavier ?? lighter;
    }
}
