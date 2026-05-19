namespace BonniePDF.Validation.Models;

/// <summary>
/// Aggregated validation report data for serialization
/// </summary>
public class ValidationReport
{
    /// <summary>
    /// Gets the timestamp when the report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets the validation profile used
    /// </summary>
    public string ValidationProfile { get; set; } = string.Empty;

    /// <summary>
    /// Gets the total number of PDFs validated
    /// </summary>
    public int TotalPdfs { get; set; }

    /// <summary>
    /// Gets the number of PDFs that passed validation
    /// </summary>
    public int Passed { get; set; }

    /// <summary>
    /// Gets the number of PDFs that failed validation
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Gets the number of PDFs that encountered errors
    /// </summary>
    public int Errors { get; set; }

    /// <summary>
    /// Gets the source directory where PDFs were validated
    /// </summary>
    public string SourceDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Gets the total time taken for validation
    /// </summary>
    public double TotalValidationTimeSeconds { get; set; }

    /// <summary>
    /// Gets the individual validation results
    /// </summary>
    public IList<ValidationResultItem> Results { get; set; } = new List<ValidationResultItem>();

    /// <summary>
    /// Creates a validation report from a collection of results
    /// </summary>
    public static ValidationReport FromResults(
        IEnumerable<ValidationResult> results,
        string validationProfile,
        string sourceDirectory)
    {
        var resultsList = results.ToList();
        var summary = ValidationSummary.FromResults(resultsList);

        return new ValidationReport
        {
            GeneratedAt = DateTime.UtcNow,
            ValidationProfile = validationProfile,
            TotalPdfs = summary.TotalPdfs,
            Passed = summary.PassedCount,
            Failed = summary.FailedCount,
            Errors = summary.ErrorCount,
            SourceDirectory = sourceDirectory,
            TotalValidationTimeSeconds = summary.TotalValidationTime.TotalSeconds,
            Results = resultsList.Select(r => new ValidationResultItem
            {
                IsSuccess = r.IsSuccess,
                IsCompliant = r.IsCompliant,
                ComplianceLevel = r.ComplianceLevel,
                ErrorMessage = r.ErrorMessage,
                Errors = r.Errors.ToList(),
                PdfFileName = r.PdfFileName,
                FilePath = r.FilePath,
                FileSize = r.FileSize,
                ValidatedAt = r.ValidatedAt,
                ValidationDurationSeconds = r.ValidationDuration.TotalSeconds
            }).ToList()
        };
    }
}

/// <summary>
/// Individual validation result item for serialization
/// </summary>
public class ValidationResultItem
{
    public bool IsSuccess { get; set; }
    public bool IsCompliant { get; set; }
    public string? ComplianceLevel { get; set; }
    public string? ErrorMessage { get; set; }
    public IList<string> Errors { get; set; } = new List<string>();
    public string PdfFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime ValidatedAt { get; set; }
    public double ValidationDurationSeconds { get; set; }
}
