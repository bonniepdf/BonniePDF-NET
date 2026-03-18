namespace BonniePDF.Examples.Layout.HorizontalStacks;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Horizontal Stacks...");
        HorizontalStackExample.Generate(Path.Combine(outputDir, "horizontal_stack_example.pdf"));
        Console.WriteLine("Generating Horizontal Stacks (Fluent API)...");
        HorizontalStackExampleFluent.Generate(Path.Combine(outputDir, "horizontal_stack_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}