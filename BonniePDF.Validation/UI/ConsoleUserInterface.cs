using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.UI;

/// <summary>
/// Console-based user interface implementation
/// </summary>
public class ConsoleUserInterface
{
    /// <summary>
    /// Displays the header
    /// </summary>
    public static void DisplayHeader()
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              BonniePDF Validation Tool                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays a message
    /// </summary>
    public static void DisplayMessage(string message)
    {
        Console.WriteLine(message);
    }

    /// <summary>
    /// Displays an error message
    /// </summary>
    public static void DisplayError(string error)
    {
        Console.WriteLine($"✗ Error: {error}");
    }

    /// <summary>
    /// Displays validation progress
    /// </summary>
    public static void DisplayProgress(int current, int total, string fileName)
    {
        Console.WriteLine($"[{current}/{total}] Validating: {fileName}");
    }

    /// <summary>
    /// Displays the validation summary
    /// </summary>
    public static void DisplaySummary(ValidationSummary summary)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Validation Summary                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Total PDFs validated: {summary.TotalPdfs}");
        Console.WriteLine($"  ✓ Passed:  {summary.PassedCount} ({summary.PassRate:F1}%)");
        Console.WriteLine($"  ✗ Failed:  {summary.FailedCount}");
        Console.WriteLine($"  ⚠ Errors:  {summary.ErrorCount}");
        Console.WriteLine();
        Console.WriteLine($"Total validation time: {summary.TotalValidationTime.TotalSeconds:F2}s");
        Console.WriteLine($"Average time per PDF: {summary.AverageValidationTime.TotalSeconds:F2}s");
        Console.WriteLine();
    }

    /// <summary>
    /// Displays a validation result
    /// </summary>
    public static void DisplayResult(ValidationResult result)
    {
        if (result.IsSuccess)
        {
            if (result.IsCompliant)
            {
                Console.WriteLine($"   ✓ PASSED - {result.ComplianceLevel}");
            }
            else
            {
                Console.WriteLine($"   ✗ FAILED - Not compliant");
                
                if (result.Errors.Count > 0)
                {
                    Console.WriteLine("   Errors:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"     - {error}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"   ✗ ERROR - {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Reads a line of input from the user
    /// </summary>
    public static string? ReadLine()
    {
        return Console.ReadLine();
    }

    /// <summary>
    /// Waits for user input before exiting
    /// </summary>
    public static void WaitForExit()
    {
        try
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (InvalidOperationException)
        {
            // Console input is redirected (e.g., running in VS Code terminal)
            // Just exit without waiting
        }
    }
}
