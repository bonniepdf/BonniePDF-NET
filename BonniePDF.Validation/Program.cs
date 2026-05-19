using BonniePDF.Validation.Configuration;
using BonniePDF.Validation.Core;
using BonniePDF.Validation.Services;
using System.Diagnostics;

namespace BonniePDF.Validation;

/// <summary>
/// BonniePDF PDF/A Validation Tool - Entry Point
/// </summary>
internal sealed class Program
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
        
        // Display header
        UI.ConsoleUserInterface.DisplayHeader();
        
        // Check if directory exists, if not prompt user
        if (string.IsNullOrEmpty(pdfDirectory))
        {
            pdfDirectory = directoryProvider.GetDirectory();
            if (string.IsNullOrEmpty(pdfDirectory) || !System.IO.Directory.Exists(pdfDirectory))
            {
                UI.ConsoleUserInterface.DisplayMessage("Default PDF directory not found.");
                UI.ConsoleUserInterface.DisplayMessage("Enter the path to the directory containing PDFs to validate:");
                pdfDirectory = UI.ConsoleUserInterface.ReadLine();
                
                if (string.IsNullOrEmpty(pdfDirectory) || !System.IO.Directory.Exists(pdfDirectory))
                {
                    UI.ConsoleUserInterface.DisplayError("Invalid directory. Exiting.");
                    UI.ConsoleUserInterface.WaitForExit();
                    return;
                }
                
                // Update directory provider with user input
                directoryProvider = new DefaultPdfDirectoryProvider(pdfDirectory);
            }
        }
        
        // Create orchestrator
        var orchestrator = new ValidationOrchestrator(
            apiClient,
            directoryProvider,
            ui,
            settings,
            reportGenerators
        );

        // Delete existing reports
        if (!string.IsNullOrEmpty(pdfDirectory))
        {
            string htmlReportPath = System.IO.Path.Combine(pdfDirectory, "validation-report.html");
            if (System.IO.File.Exists(htmlReportPath))
            {
                System.IO.File.Delete(htmlReportPath);
            }

            string jsonReportPath = System.IO.Path.Combine(pdfDirectory, "validation-report.json");
            if (System.IO.File.Exists(jsonReportPath))
            {
                System.IO.File.Delete(jsonReportPath);
            }
        }
        
        // Run validation
        await orchestrator.ValidateAllAsync();
        
        // Open HTML report if it was generated
        if (settings.GenerateHtmlReport)
        {
            string htmlReportPath = System.IO.Path.Combine(pdfDirectory, "validation-report.html");
            if (File.Exists(htmlReportPath))
            {
                UI.ConsoleUserInterface.DisplayMessage("");
                UI.ConsoleUserInterface.DisplayMessage("Opening validation report in browser...");
                
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
                    UI.ConsoleUserInterface.DisplayError($"Failed to open report: {ex.Message}");
                    UI.ConsoleUserInterface.DisplayMessage($"Report location: {htmlReportPath}");
                }
            }
        }
        
        // Wait for exit
        UI.ConsoleUserInterface.WaitForExit();
    }
}
