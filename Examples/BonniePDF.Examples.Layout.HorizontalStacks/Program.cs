namespace BonniePDF.Examples.Layout.HorizontalStacks;

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
        Console.WriteLine("Generating Horizontal Stacks (Fluent API)...");
        using (var doc = HorizontalStacksExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_stack_example_fluent.pdf"));
        }

        Console.WriteLine("Generating Horizontal Stacks (Tagged / PDF/UA-1)...");
        using (var doc = HorizontalStacksTaggedExample.Generate())
        {
            doc.SaveWithPreview(Path.Combine(outputDir, "horizontal_stack_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
