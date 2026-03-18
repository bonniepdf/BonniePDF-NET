namespace BonniePDF.Examples.Elements.Lists;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Lists...");
        ListExample.Generate(Path.Combine(outputDir, "list_example.pdf"));
        
        Console.WriteLine("Generating Lists (Fluent API)...");
        ListFluentExample.Generate(Path.Combine(outputDir, "list_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}