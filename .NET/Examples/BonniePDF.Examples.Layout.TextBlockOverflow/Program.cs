using System;
using System.IO;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating TextBlock Overflow...");
                    BonniePDF.Examples.Examples._02_Layout.TextBlockOverflowExample.Generate(Path.Combine(outputDir, "textblock_overflow.pdf"));
        Console.WriteLine("Generating TextBlock Overflow (Fluent API)...");
                    BonniePDF.Examples.Examples._02_Layout.TextBlockOverflowExampleFluent.Generate(Path.Combine(outputDir, "textblock_overflow_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
