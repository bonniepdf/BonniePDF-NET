namespace BonniePDF.Examples.Layout.TextBlockOverflow;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating TextBlock Overflow...");
        TextBlockOverflowExample.Generate(Path.Combine(outputDir, "textblock_overflow.pdf"));
        Console.WriteLine("Generating TextBlock Overflow (Fluent API)...");
        TextBlockOverflowExampleFluent.Generate(Path.Combine(outputDir, "textblock_overflow_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}