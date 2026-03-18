using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Margins & Units...");
                    BonniePDF.Examples.Examples._01_Basics.MarginExample.Generate(Path.Combine(outputDir, "margin_example.pdf"));
        Console.WriteLine("Generating Margins (Fluent API)...");
                    BonniePDF.Examples.Examples._01_Basics.MarginExampleFluent.Generate(Path.Combine(outputDir, "margin_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
