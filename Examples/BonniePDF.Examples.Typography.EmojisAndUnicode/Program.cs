namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Emojis & Unicode...");
        using (var doc = EmojiExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "emoji_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}