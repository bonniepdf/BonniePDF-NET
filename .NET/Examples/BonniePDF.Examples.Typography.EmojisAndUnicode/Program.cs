namespace BonniePDF.Examples.Typography.EmojisAndUnicode;

class Program
{
    static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Emojis & Unicode...");
        EmojiExample.Generate(Path.Combine(outputDir, "emoji_example.pdf"));
        Console.WriteLine("Generating Emojis & Unicode (Fluent API)...");
        EmojisAndUnicodeFluentExample.Generate(Path.Combine(outputDir, "emoji_example_fluent.pdf"));

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}