using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Simple Document...");
                    BonniePDF.Examples.Examples._01_Basics.SimpleDocumentExample.Generate(Path.Combine(outputDir, "simple_document.pdf"));
        Console.WriteLine("Generating Simple Document (Fluent API)...");
                    BonniePDF.Examples.Examples._01_Basics.SimpleDocumentFluentExample.Generate(Path.Combine(outputDir, "simple_document_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
