using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating VerticalStack Overflow...");
                    BonniePDF.Examples.Examples._02_Layout.VerticalStackOverflowExample.Generate(Path.Combine(outputDir, "verticalstack_overflow.pdf"));
        Console.WriteLine("Generating VerticalStack Overflow (Fluent API)...");
                    BonniePDF.Examples.Examples._02_Layout.VerticalStackOverflowExampleFluent.Generate(Path.Combine(outputDir, "verticalstack_overflow_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
