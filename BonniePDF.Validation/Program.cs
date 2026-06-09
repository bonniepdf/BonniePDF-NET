using BonniePDF.Validation.Configuration;
using BonniePDF.Validation.Core;
using BonniePDF.Validation.Models;
using BonniePDF.Validation.Services;
using System.Diagnostics;

namespace BonniePDF.Validation;

/// <summary>
/// BonniePDF Validation Tool - Entry Point.
/// Validates PDFs against a VeraPDF profile (PDF/A 1a/1b/2a/2b/3a/3b or PDF/UA-1).
/// </summary>
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        // Parse CLI args: optional --profile <id> / -p <id>, optional positional directory.
        if (!TryParseArgs(args, out string? pdfDirectory, out ValidationProfile profile, out string? cliError))
        {
            // Empty error string = --help requested; don't print "ERROR: " line.
            if (!string.IsNullOrEmpty(cliError))
                UI.ConsoleUserInterface.DisplayError(cliError);
            PrintUsage();
            UI.ConsoleUserInterface.WaitForExit();
            return;
        }

        // Create settings (defaults for everything except the selected profile).
        var settings = new ValidationSettings { DefaultProfile = profile };
        var veraPdfSettings = Configuration.VeraPdfSettings.Default;

        // Create services
        var ui = new UI.ConsoleUserInterface();
        
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

    /// <summary>
    /// Parses the CLI argument vector into a profile and an optional PDF directory.
    /// Accepts <c>--profile &lt;id&gt;</c> / <c>-p &lt;id&gt;</c> (id is one of <c>1a</c>,
    /// <c>1b</c>, <c>2a</c>, <c>2b</c>, <c>3a</c>, <c>3b</c>, <c>ua1</c>) and a single
    /// positional directory argument. Order-independent.
    /// </summary>
    private static bool TryParseArgs(
        string[] args,
        out string? pdfDirectory,
        out ValidationProfile profile,
        out string? error)
    {
        pdfDirectory = null;
        profile = ValidationProfile.PDF_A_1b;
        error = null;

        for (int i = 0; i < args.Length; i++)
        {
            string a = args[i];

            if (a == "--profile" || a == "-p")
            {
                if (i + 1 >= args.Length)
                {
                    error = "--profile requires a value (e.g. 1b, 2b, 3b, 1a, 2a, 3a, ua1).";
                    return false;
                }

                string id = args[++i];
                if (!ValidationProfileExtensions.TryParse(id, out profile))
                {
                    error = $"Unknown profile '{id}'. Supported: 1a, 1b, 2a, 2b, 3a, 3b, ua1.";
                    return false;
                }
            }
            else if (a == "--help" || a == "-h")
            {
                error = ""; // empty-string error = "print usage and exit, no error message"
                return false;
            }
            else if (pdfDirectory is null)
            {
                pdfDirectory = a;
            }
            else
            {
                error = $"Unexpected argument '{a}'.";
                return false;
            }
        }

        return true;
    }

    private static void PrintUsage()
    {
        UI.ConsoleUserInterface.DisplayMessage("");
        UI.ConsoleUserInterface.DisplayMessage("Usage:");
        UI.ConsoleUserInterface.DisplayMessage("  dotnet run --project BonniePDF.Validation -- [--profile <id>] [<pdf-directory>]");
        UI.ConsoleUserInterface.DisplayMessage("");
        UI.ConsoleUserInterface.DisplayMessage("Options:");
        UI.ConsoleUserInterface.DisplayMessage("  --profile, -p <id>   Validation profile. Default: 1b.");
        UI.ConsoleUserInterface.DisplayMessage("                       Supported: 1a, 1b, 2a, 2b, 3a, 3b, ua1.");
        UI.ConsoleUserInterface.DisplayMessage("  --help, -h           Print this usage and exit.");
        UI.ConsoleUserInterface.DisplayMessage("");
        UI.ConsoleUserInterface.DisplayMessage("Examples:");
        UI.ConsoleUserInterface.DisplayMessage("  ./run-validator.sh");
        UI.ConsoleUserInterface.DisplayMessage("  ./run-validator.sh --profile ua1");
        UI.ConsoleUserInterface.DisplayMessage("  ./run-validator.sh --profile 3b /path/to/pdfs");
    }
}
