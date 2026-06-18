namespace BonniePDF.Examples.Layout.VerticalStackOverflow;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating VerticalStack Overflow...");
        using (var doc = VerticalStackOverflowExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "verticalstack_overflow.pdf"));
        }
        Console.WriteLine("Generating VerticalStack Overflow (Fluent API)...");
        using (var doc = VerticalStackOverflowExampleFluent.Generate())
        {
            doc.Save(Path.Combine(outputDir, "verticalstack_overflow_fluent.pdf"));
        }

        Console.WriteLine("Generating VerticalStack Overflow (Tagged / PDF/UA-1)...");
        using (var doc = VerticalStackOverflowTaggedExample.Generate())
        {
            doc.SaveWithPreview(Path.Combine(outputDir, "verticalstack_overflow_tagged.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
