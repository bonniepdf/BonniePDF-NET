namespace BonniePDF.Examples.LayoutFlow.HorizontalStacks;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Horizontal Stacks...");
        using (var doc = HorizontalStacksExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_stack_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}