namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating VerticalStack Overflow...");
        VerticalStackOverflowExample.Generate(Path.Combine(outputDir, "verticalstack_overflow.pdf"));
        Console.WriteLine("Generating VerticalStack Overflow (Fluent API)...");
        VerticalStackOverflowExampleFluent.Generate(Path.Combine(outputDir, "verticalstack_overflow_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}