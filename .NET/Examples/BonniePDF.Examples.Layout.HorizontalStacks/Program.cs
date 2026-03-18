using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Horizontal Stacks...");
                    BonniePDF.Examples.Examples._02_Layout.HorizontalStackExample.Generate(Path.Combine(outputDir, "horizontal_stack_example.pdf"));
        Console.WriteLine("Generating Horizontal Stacks (Fluent API)...");
                    BonniePDF.Examples.Examples._02_Layout.HorizontalStackExampleFluent.Generate(Path.Combine(outputDir, "horizontal_stack_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
