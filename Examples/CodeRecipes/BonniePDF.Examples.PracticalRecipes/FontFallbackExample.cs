using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Use local fallback faces and strict missing-character checks.</summary>
public static class FontFallbackExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings {
            Language = "en-GB", MissingGlyphs = MissingGlyphs.Throw, DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12) }) { Metadata = new DocumentMetadata { Title = "Font fallback and missing characters" } };
        doc.Fonts.Add(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", "NotoSansMath-Regular.ttf")), family: "Math fallback");
        doc.Add(new Heading("Font fallback") { Level = 1, FontFamily = "Helvetica", FontWeight = FontWeight.Bold, FontSize = 24 });
        doc.Add(new TextBlock("Regular primary font: invoice, delivery and payment."));
        doc.Add(new TextBlock("Bold primary font: approved.") { FontWeight = FontWeight.Bold, Margin = new Thickness(0, 12, 0, 12) });
        doc.Add(new TextBlock("Mathematical symbols use the registered fallback: ∑ ∫ ∞ ℝ."));
        doc.Add(new TextBlock("The document requests strict glyph checking. An unsupported character raises MissingGlyphException instead of silently disappearing.") { Margin = new Thickness(0, 12, 0, 0) });
        return doc;
    }

    public static async Task VerifyMissingCharacterAsync()
    {
        using var doc = Generate();
        doc.Add(new TextBlock(char.ConvertFromUtf32(0x10ffff)));
        try { await doc.GenerateBytesAsync(); }
        catch (MissingGlyphException) { return; }
        throw new InvalidOperationException("The unsupported character should fail strict glyph checking.");
    }
}
