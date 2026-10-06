using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// The same document has to write the same bytes. Font objects were written in the enumeration order of a concurrent
/// dictionary keyed by font name, which follows the keys' hashes: different in every process, and for a document's own
/// font (whose name carries the document's serial) different in every document.
/// </summary>
[Collection("Serial")]
public partial class FontObjectOrderTests
{
    [GeneratedRegex(@"/ID \[[^\]]*\]")]
    private static partial Regex TrailerId();

    [GeneratedRegex("[A-Z]{6}\\+")]
    private static partial Regex SubsetPrefix();

    [GeneratedRegex(@"/(?:Creation|Mod)Date[^\n]*")]
    private static partial Regex Dates();

    private static string Masked(byte[] pdf) =>
        Dates().Replace(SubsetPrefix().Replace(TrailerId().Replace(Encoding.Latin1.GetString(pdf), "/ID []"), "S+"), "");

    [Fact]
    public void TheSameDocumentWritesItsFontsInTheSameOrderWhateverItsSerial()
    {
        byte[] regular = FontManager.GetBytesForFamily("liberation sans", "regular")
            ?? throw new InvalidOperationException("The bundled font is missing.");

        var outputs = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < 16; i++)
        {
            using var doc = new BonniePDF.Document();
            doc.Fonts.Add(regular, family: "Inter");
            doc.Add(new TextBlock { Text = "An embedded font", FontFamily = "Inter" });
            doc.Add(new TextBlock { Text = "A standard one", FontName = "Helvetica-Bold" });
            outputs.Add(Masked(doc.GenerateBytes()));
        }

        Assert.Single(outputs);
    }
}
