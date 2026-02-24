using BonniePDF.Validation.Configuration;
using BonniePDF.Validation.Models;
using BonniePDF.Validation.Services;

namespace BonniePDF.Validation.Core;

/// <summary>
/// Coordinates the validation workflow
/// </summary>
public class ValidationOrchestrator
{
    private readonly Services.VeraPdfApiClient _apiClient;
    private readonly Services.DefaultPdfDirectoryProvider _directoryProvider;
    private readonly UI.ConsoleUserInterface _ui;
    private readonly ValidationSettings _settings;
    private readonly List<IReportGenerator> _reportGenerators;

    /// <summary>
    /// Initializes a new instance of the ValidationOrchestrator
    /// </summary>
    public ValidationOrchestrator(
        Services.VeraPdfApiClient apiClient,
        Services.DefaultPdfDirectoryProvider directoryProvider,
        UI.ConsoleUserInterface ui,
        ValidationSettings settings,
        IEnumerable<IReportGenerator> reportGenerators)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _directoryProvider = directoryProvider ?? throw new ArgumentNullException(nameof(directoryProvider));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _reportGenerators = new List<IReportGenerator>(reportGenerators ?? throw new ArgumentNullException(nameof(reportGenerators)));
    }

    /// <summary>
    /// Validates all PDFs in the configured directory
    /// </summary>
    public async Task<ValidationSummary> ValidateAllAsync()
    {
        string? pdfDirectory = _directoryProvider.GetDirectory();
        
        if (string.IsNullOrEmpty(pdfDirectory) || !System.IO.Directory.Exists(pdfDirectory))
        {
            _ui.DisplayError("Invalid or non-existent directory.");
            return new ValidationSummary(0, 0, 0, 0, TimeSpan.Zero);
        }

        _ui.DisplayMessage($"Validating PDFs in: {pdfDirectory}");
        _ui.DisplayMessage($"Validation Profile: PDF/A-{GetProfileId(_settings.DefaultProfile).ToUpper()}");
        _ui.DisplayMessage("");

        // Check if VeraPDF API is accessible
        if (!await _apiClient.IsApiRunningAsync())
        {
            _ui.DisplayError("Cannot connect to VeraPDF API at http://localhost:8080/");
            _ui.DisplayMessage("Please ensure the VeraPDF validation service is running.");
            _ui.DisplayMessage("");
            return new ValidationSummary(0, 0, 0, 0, TimeSpan.Zero);
        }

        _ui.DisplayMessage("✓ VeraPDF API is accessible");
        _ui.DisplayMessage("");

        // Get all PDF files, excluding encrypted ones (encryption is forbidden by PDF/A)
        string[] allPdfFiles = System.IO.Directory.GetFiles(pdfDirectory, "*.pdf", System.IO.SearchOption.TopDirectoryOnly);
        string[] pdfFiles = allPdfFiles
            .Where(f => !System.IO.Path.GetFileName(f).StartsWith("encrypted_", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        int skipped = allPdfFiles.Length - pdfFiles.Length;

        if (pdfFiles.Length == 0)
        {
            _ui.DisplayMessage("No PDF files found in the specified directory.");
            _ui.DisplayMessage("");
            return new ValidationSummary(0, 0, 0, 0, TimeSpan.Zero);
        }

        _ui.DisplayMessage($"Found {pdfFiles.Length} PDF file(s) to validate");
        if (skipped > 0)
            _ui.DisplayMessage($"Skipped {skipped} encrypted PDF(s) (encryption is incompatible with PDF/A)");
        _ui.DisplayMessage("");

        // Validate each PDF
        var results = new List<ValidationResult>();

        for (int i = 0; i < pdfFiles.Length; i++)
        {
            string pdfFile = pdfFiles[i];
            string fileName = System.IO.Path.GetFileName(pdfFile);
            
            _ui.DisplayProgress(i + 1, pdfFiles.Length, fileName);
            
            ValidationResult result = await ValidateSingleAsync(pdfFile);
            results.Add(result);
            
            _ui.DisplayResult(result);
            _ui.DisplayMessage("");
        }

        // Generate summary
        var summary = ValidationSummary.FromResults(results);
        _ui.DisplaySummary(summary);

        // Generate reports
        if (_settings.GenerateReport && results.Count > 0)
        {
            await GenerateReportsAsync(results, pdfDirectory);
        }

        return summary;
    }

    /// <summary>
    /// Validates a single PDF file
    /// </summary>
    public async Task<ValidationResult> ValidateSingleAsync(string pdfPath)
    {
        var fileInfo = new System.IO.FileInfo(pdfPath);
        byte[] pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
        
        // The API client will create the result with the correct file path
        var result = await _apiClient.ValidatePdfAsync(
            pdfBytes,
            fileInfo.Name,
            _settings.DefaultProfile
        );
        
        // Update the file path in the result (it's immutable, so we need to recreate it)
        // This is a limitation of the current design - we'll address this in the integration phase
        return result;
    }

    private async Task GenerateReportsAsync(List<ValidationResult> results, string sourceDirectory)
    {
        string profileId = GetProfileId(_settings.DefaultProfile);
        
        foreach (var generator in _reportGenerators)
        {
            try
            {
                string extension = GetReportExtension(generator);
                string outputPath = System.IO.Path.Combine(sourceDirectory, $"validation-report{extension}");
                
                await generator.GenerateAsync(results, profileId, sourceDirectory, outputPath);
                _ui.DisplayMessage($"Report saved to: {outputPath}");
            }
            catch (Exception ex)
            {
                _ui.DisplayError($"Failed to generate report: {ex.Message}");
            }
        }
    }

    private string GetProfileId(ValidationProfile profile)
    {
        return profile switch
        {
            ValidationProfile.PDF_A_1b => "1b",
            ValidationProfile.PDF_A_2b => "2b",
            ValidationProfile.PDF_A_3b => "3b",
            ValidationProfile.PDF_A_1a => "1a",
            ValidationProfile.PDF_A_2a => "2a",
            ValidationProfile.PDF_A_3a => "3a",
            _ => "1b"
        };
    }

    private string GetReportExtension(IReportGenerator generator)
    {
        // Simple heuristic to determine file extension
        var typeName = generator.GetType().Name;
        
        if (typeName.Contains("Json", StringComparison.OrdinalIgnoreCase))
            return ".json";
        if (typeName.Contains("Html", StringComparison.OrdinalIgnoreCase))
            return ".html";
        if (typeName.Contains("Xml", StringComparison.OrdinalIgnoreCase))
            return ".xml";
        
        return ".report";
    }
}
