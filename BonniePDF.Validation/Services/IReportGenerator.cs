using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.Services;

/// <summary>
/// Abstraction for generating validation reports
/// </summary>
public interface IReportGenerator
{
    /// <summary>
    /// Generates a validation report
    /// </summary>
    /// <param name="results">The validation results</param>
    /// <param name="validationProfile">The validation profile's display name (e.g. <c>"PDF/A-1B"</c>, <c>"PDF/UA-1"</c>) — embedded verbatim into the report</param>
    /// <param name="sourceDirectory">The source directory of the PDFs</param>
    /// <param name="outputPath">The output path for the report</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task GenerateAsync(
        IEnumerable<ValidationResult> results,
        string validationProfile,
        string sourceDirectory,
        string outputPath);
}
