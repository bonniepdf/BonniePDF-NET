using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace BonniePDF.Examples;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           BonniePDF - PDF Generation Examples                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Create output directory
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        // Track generated files for opening
        List<string> generatedFiles = new();

        // Run standard examples
        int exampleNumber = 1;
        foreach (var category in ExampleRunner.Categories)
        {
            Console.WriteLine($"\n--- Category: {category.Name} ---");
            foreach (var example in category.Examples)
            {
                string filePath = RunExample(example, outputDir, exampleNumber++);
                if (filePath != null)
                    generatedFiles.Add(filePath);
            }
        }

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    All examples generated!                   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Output directory: {outputDir}");
        Console.WriteLine();

        // Open all generated PDFs
        OpenPdfs(generatedFiles);

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static string? RunExample(Models.ExampleInfo example, string outputDir, int number)
    {
        Console.WriteLine($"Generating Example {number}: {example.Description}...");
        string filePath = Path.Combine(outputDir, example.FileName);
        example.GenerateAction?.Invoke(filePath);
        Console.WriteLine($"   ✓ Created: {filePath}");

        if (example.AdditionalInfo != null)
            foreach (string info in example.AdditionalInfo)
                Console.WriteLine($"   {info}");

        return filePath;
    }

    private static void OpenPdfs(IEnumerable<string> filePaths)
    {
        foreach (string filePath in filePaths) OpenPdf(filePath);
    }

    private static void OpenPdf(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"   ✗ File not found: {filePath}");
                return;
            }

            ProcessStartInfo psi = new()
            {
                FileName = filePath,
                UseShellExecute = true
            };

            Process.Start(psi);
            Console.WriteLine($"   ✓ Opening: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Error opening {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }
}