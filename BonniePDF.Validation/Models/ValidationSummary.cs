namespace BonniePDF.Validation.Models;

/// <summary>
/// Summary statistics for validation results
/// </summary>
public class ValidationSummary
{
    /// <summary>
    /// Gets the total number of PDFs validated
    /// </summary>
    public int TotalPdfs { get; }

    /// <summary>
    /// Gets the number of PDFs that passed validation
    /// </summary>
    public int PassedCount { get; }

    /// <summary>
    /// Gets the number of PDFs that failed validation
    /// </summary>
    public int FailedCount { get; }

    /// <summary>
    /// Gets the number of PDFs that encountered errors during validation
    /// </summary>
    public int ErrorCount { get; }

    /// <summary>
    /// Gets the pass rate as a percentage
    /// </summary>
    public double PassRate => TotalPdfs > 0 ? (double)PassedCount / TotalPdfs * 100 : 0;

    /// <summary>
    /// Gets the total time taken for all validations
    /// </summary>
    public TimeSpan TotalValidationTime { get; }

    /// <summary>
    /// Gets the average validation time per PDF
    /// </summary>
    public TimeSpan AverageValidationTime => TotalPdfs > 0
        ? TimeSpan.FromTicks(TotalValidationTime.Ticks / TotalPdfs)
        : TimeSpan.Zero;

    /// <summary>
    /// Initializes a new instance of the ValidationSummary class
    /// </summary>
    public ValidationSummary(
        int totalPdfs,
        int passedCount,
        int failedCount,
        int errorCount,
        TimeSpan totalValidationTime)
    {
        TotalPdfs = totalPdfs;
        PassedCount = passedCount;
        FailedCount = failedCount;
        ErrorCount = errorCount;
        TotalValidationTime = totalValidationTime;
    }

    /// <summary>
    /// Creates a validation summary from a collection of validation results
    /// </summary>
    public static ValidationSummary FromResults(IEnumerable<ValidationResult> results)
    {
        var resultsList = results.ToList();
        var totalValidationTime = TimeSpan.FromTicks(
            resultsList.Sum(r => r.ValidationDuration.Ticks)
        );

        return new ValidationSummary(
            totalPdfs: resultsList.Count,
            passedCount: resultsList.Count(r => r.IsSuccess && r.IsCompliant),
            failedCount: resultsList.Count(r => r.IsSuccess && !r.IsCompliant),
            errorCount: resultsList.Count(r => !r.IsSuccess),
            totalValidationTime: totalValidationTime
        );
    }
}
