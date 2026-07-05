namespace BonniePDF.Examples.Layout.TextBlockOverflow;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating TextBlock Overflow...");
        using (var doc = TextBlockOverflowExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "textblock_overflow.pdf"));
        }
        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
