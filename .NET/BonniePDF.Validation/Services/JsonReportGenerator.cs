using System.Text.Json;
using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.Services;

/// <summary>
/// Generates JSON validation reports
/// </summary>
public class JsonReportGenerator : IReportGenerator
{
    /// <summary>
    /// Initializes a new instance of the JsonReportGenerator
    /// </summary>
    public JsonReportGenerator()
    {
    }

    /// <summary>
    /// Generates a validation report
    /// </summary>
    public async Task GenerateAsync(
        IEnumerable<ValidationResult> results,
        string validationProfile,
        string sourceDirectory,
        string outputPath)
    {
        var report = ValidationReport.FromResults(results, validationProfile, sourceDirectory);

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(report, options);
        await System.IO.File.WriteAllTextAsync(outputPath, json);
    }
}
