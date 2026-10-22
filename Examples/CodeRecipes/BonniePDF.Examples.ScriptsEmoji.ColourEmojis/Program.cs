using BonniePDF.Examples.ScriptsEmoji.ColourEmojis;
string output = Path.Combine(AppContext.BaseDirectory, "GeneratedPdfs");
Directory.CreateDirectory(output);
using var doc = ColourEmojiExample.Generate();
doc.Save(Path.Combine(output, "colour-emojis.pdf"));
File.WriteAllText(Path.Combine(output, "colour-emojis.svg"), doc.GenerateSvgPages()[0]);
Console.WriteLine(output);
