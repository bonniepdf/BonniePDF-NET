namespace BonniePDF.Examples.Basics.Margins;
internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Margins & Units...");
        using (var doc = MarginExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "margin_example.pdf"));
        }
        Console.WriteLine("Generating Margins (Fluent API)...");
        using (var doc = MarginExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "margin_example_fluent.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
