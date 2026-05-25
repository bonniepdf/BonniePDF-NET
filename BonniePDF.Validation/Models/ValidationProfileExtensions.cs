using System.Diagnostics.CodeAnalysis;

namespace BonniePDF.Validation.Models;

/// <summary>
/// Helpers for mapping <see cref="ValidationProfile"/> values to the
/// strings consumed by the VeraPDF REST API and shown to end users.
///
/// Centralising these mappings means new profiles (currently
/// <see cref="ValidationProfile.PDF_UA_1"/>) only require touching one
/// place instead of every caller that needs a profile-id or a display
/// name.
/// </summary>
public static class ValidationProfileExtensions
{
    /// <summary>
    /// Returns the short profile identifier used in VeraPDF REST URLs
    /// (e.g. <c>"1b"</c>, <c>"ua1"</c>).
    /// </summary>
    public static string GetProfileId(this ValidationProfile profile)
    {
        return profile switch
        {
            ValidationProfile.PDF_A_1b => "1b",
            ValidationProfile.PDF_A_2b => "2b",
            ValidationProfile.PDF_A_3b => "3b",
            ValidationProfile.PDF_A_1a => "1a",
            ValidationProfile.PDF_A_2a => "2a",
            ValidationProfile.PDF_A_3a => "3a",
            ValidationProfile.PDF_UA_1 => "ua1",
            _ => "1b"
        };
    }

    /// <summary>
    /// Returns the human-readable name of the profile
    /// (e.g. <c>"PDF/A-1B"</c>, <c>"PDF/UA-1"</c>). Used in console
    /// messages, HTML/JSON reports, and error strings.
    /// </summary>
    public static string GetDisplayName(this ValidationProfile profile)
    {
        return profile switch
        {
            ValidationProfile.PDF_UA_1 => "PDF/UA-1",
            _ => $"PDF/A-{profile.GetProfileId().ToUpperInvariant()}"
        };
    }

    /// <summary>
    /// Parses a profile identifier string (e.g. <c>"1b"</c>,
    /// <c>"ua1"</c>) into a <see cref="ValidationProfile"/> value.
    /// Matching is case-insensitive.
    /// </summary>
    /// <returns><c>true</c> if the string maps to a known profile; <c>false</c> otherwise.</returns>
    public static bool TryParse([NotNullWhen(true)] string? id, out ValidationProfile profile)
    {
        switch (id?.Trim().ToLowerInvariant())
        {
            case "1a": profile = ValidationProfile.PDF_A_1a; return true;
            case "1b": profile = ValidationProfile.PDF_A_1b; return true;
            case "2a": profile = ValidationProfile.PDF_A_2a; return true;
            case "2b": profile = ValidationProfile.PDF_A_2b; return true;
            case "3a": profile = ValidationProfile.PDF_A_3a; return true;
            case "3b": profile = ValidationProfile.PDF_A_3b; return true;
            case "ua1": profile = ValidationProfile.PDF_UA_1; return true;
            default: profile = default; return false;
        }
    }
}
