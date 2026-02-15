using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.Configuration;

/// <summary>
/// Configuration settings for validation behavior
/// </summary>
public class ValidationSettings
{
    /// <summary>
    /// Gets the default validation profile to use
    /// </summary>
    public ValidationProfile DefaultProfile { get; init; } = ValidationProfile.PDF_A_1b;

    /// <summary>
    /// Gets the output directory for reports
    /// </summary>
    public string? OutputDirectory { get; init; }

    /// <summary>
    /// Gets whether to generate reports
    /// </summary>
    public bool GenerateReport { get; init; } = true;

    /// <summary>
    /// Gets whether to generate HTML reports
    /// </summary>
    public bool GenerateHtmlReport { get; init; } = true;

    /// <summary>
    /// Gets whether to generate JSON reports
    /// </summary>
    public bool GenerateJsonReport { get; init; } = true;

    /// <summary>
    /// Creates a new instance with default settings
    /// </summary>
    public static ValidationSettings Default => new();
}
