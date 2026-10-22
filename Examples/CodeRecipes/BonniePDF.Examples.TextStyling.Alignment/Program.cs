namespace BonniePDF.Examples.TextStyling.Alignment;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Text Alignment...");
        using (var doc = TextAlignmentExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "text_alignment.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}