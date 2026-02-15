using BonniePDF.Validation.Configuration;
using BonniePDF.Validation.Core;
using BonniePDF.Validation.Services;
using System.Diagnostics;

namespace BonniePDF.Validation;

/// <summary>
/// BonniePDF PDF/A Validation Tool - Entry Point
/// </summary>
internal class Program
{
    private static async Task Main(string[] args)
    {
        // Create settings
        var settings = ValidationSettings.Default;
        var veraPdfSettings = Configuration.VeraPdfSettings.Default;
        
        // Create services
        var ui = new UI.ConsoleUserInterface();
        
        // Determine PDF directory
        string? pdfDirectory = null;
        if (args.Length > 0 && System.IO.Directory.Exists(args[0]))
        {
            pdfDirectory = args[0];
        }
        
        var directoryProvider = string.IsNullOrEmpty(pdfDirectory)
            ? DefaultPdfDirectoryProvider.CreateDefault()
            : new DefaultPdfDirectoryProvider(pdfDirectory);
        
        var apiClient = new VeraPdfApiClient(veraPdfSettings);
        
        // Create report generators
        var reportGenerators = new List<IReportGenerator>();
        
        if (settings.GenerateJsonReport)
        {
            reportGenerators.Add(new JsonReportGenerator());
        }
        
        if (settings.GenerateHtmlReport)
        {
            reportGenerators.Add(new HtmlReportGenerator());
        }
        
        // Create orchestrator
        var orchestrator = new ValidationOrchestrator(
            apiClient,
            directoryProvider,
            ui,
            settings,
            reportGenerators
        );
        
        // Display header
        ui.DisplayHeader();
        
        // Check if directory exists, if not prompt user
        if (string.IsNullOrEmpty(pdfDirectory))
        {
            pdfDirectory = directoryProvider.GetDirectory();
            if (string.IsNullOrEmpty(pdfDirectory) || !System.IO.Directory.Exists(pdfDirectory))
            {
                ui.DisplayMessage("Default PDF directory not found.");
                ui.DisplayMessage("Enter the path to the directory containing PDFs to validate:");
                pdfDirectory = ui.ReadLine();
                
                if (string.IsNullOrEmpty(pdfDirectory) || !System.IO.Directory.Exists(pdfDirectory))
                {
                    ui.DisplayError("Invalid directory. Exiting.");
                    ui.WaitForExit();
                    return;
                }
                
                // Update directory provider with user input
                directoryProvider = new DefaultPdfDirectoryProvider(pdfDirectory);
            }
        }
        
        // Run validation
        await orchestrator.ValidateAllAsync();
        
        // Open HTML report if it was generated
        if (settings.GenerateHtmlReport)
        {
            string htmlReportPath = System.IO.Path.Combine(pdfDirectory, "validation-report.html");
            if (System.IO.File.Exists(htmlReportPath))
            {
                ui.DisplayMessage("");
                ui.DisplayMessage("Opening validation report in browser...");
                
                try
                {
                    // Open HTML report in default browser
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = htmlReportPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    ui.DisplayError($"Failed to open report: {ex.Message}");
                    ui.DisplayMessage($"Report location: {htmlReportPath}");
                }
            }
        }
        
        // Wait for exit
        ui.WaitForExit();
    }
}
