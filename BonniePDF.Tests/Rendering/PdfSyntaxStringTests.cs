using System.Text;
using BonniePDF.Rendering.Pdf;
using Xunit;

namespace BonniePDF.Tests.Rendering;

public sealed class PdfSyntaxStringTests
{
    [Theory]
    [InlineData("", "()")]
    [InlineData("a(b)\\c\r\n\t\b\f", "(a\\(b\\)\\\\c\\r\\n\\t\\b\\f)")]
    [InlineData("caf\u00e9", "(caf\u00e9)")]
    public void AppendsLiteralStringsWithoutChangingEscapes(string input, string expected)
    {
        var result = new StringBuilder("prefix");
        PdfSyntax.AppendMetadataString(result, input);
        Assert.Equal("prefix" + expected, result.ToString());
        Assert.Equal(expected, PdfSyntax.EncodeMetadataString(input));
    }

    [Fact]
    public void UnicodeEncodingMatchesRuntimeIncludingUnpairedSurrogates()
    {
        var random = new Random(42);
        for (int i = 0; i < 1000; i++)
        {
            var chars = new char[32];
            for (int j = 0; j < chars.Length; j++) chars[j] = (char)random.Next(0x10000);
            // Guarantee Unicode hex encoding, with valid and invalid surrogate sequences.
            chars[0] = '\u0100';
            chars[1] = '\ud83d'; chars[2] = '\ude00'; chars[3] = '\ud800'; chars[4] = 'x';
            string text = new(chars);
            string expected = "<FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(text)) + ">";
            var result = new StringBuilder();
            PdfSyntax.AppendMetadataString(result, text);
            Assert.Equal(expected, result.ToString());
            Assert.Equal(expected, PdfSyntax.EncodeMetadataString(text));
        }
    }
}
