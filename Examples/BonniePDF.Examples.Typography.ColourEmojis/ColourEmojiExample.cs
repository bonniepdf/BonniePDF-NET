using BonniePDF;

namespace BonniePDF.Examples.Typography.ColourEmojis;

/// <summary>Opt-in colour emoji with complete Unicode sequences and embedded vector artwork.</summary>
[System.ComponentModel.DisplayName("Colour Emojis")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Colour emoji alongside regular text, including joined families, skin tones, flags and keycaps. PDF/A-2b and PDF/UA-1.")]
public static class ColourEmojiExample
{
    /// <summary>Builds the document. Supply the two font assets or use the bundled example fonts.</summary>
    public static Document Generate(IReadOnlyDictionary<string, byte[]>? assets = null)
    {
        Document doc = new();
        doc.Settings.EmojiRendering = EmojiRendering.Colour;
        doc.Settings.Conformance = PdfAConformance.PdfA2b;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Metadata.Title = "Colour Emojis";
        doc.Metadata.Author = "BonniePDF";
        string Font(string name) => doc.Fonts.Add(assets is not null && assets.TryGetValue("fonts/" + name, out var bytes)
            ? bytes : File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", name))).FontName;
        string text = Font("NotoSans-Regular.ttf"), emoji = Font("TwemojiMozilla.ttf");
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily(text).WithSize(12);
        doc.Add(new Heading("Colour Emojis") { Level = 1, FontSize = 24 });
        doc.Add(new TextBlock("Colour is an explicit choice. The Emojis & Unicode example continues to use monochrome outlines.") { Margin = new Thickness(0, 12, 0, 18) });
        doc.Add(new TextBlock("😀 👩‍💻 👍🏽 🇬🇧 ❤️ 1️⃣") { FontName = emoji, FontSize = 36, Margin = new Thickness(0, 0, 0, 18) });
        doc.Add(new RichText().Span("Ready for launch ").Span("🚀", new TextStyle().WithFamily(emoji)).Span(" with searchable, selectable emoji."));
        doc.Add(new TextBlock("Family and profession sequences stay together when text wraps:") { Margin = new Thickness(0, 18, 0, 8) });
        doc.Add(new TextBlock("👨‍👩‍👧‍👦 👩‍🚀 👨‍🍳 👩‍🔬 👨‍💻") { FontName = emoji, FontSize = 36 });
        doc.Add(new TextBlock("Emoji artwork: Twemoji (CC BY 4.0); font by Mozilla. github.com/twitter/twemoji") { FontSize = 8, Margin = new Thickness(0, 18, 0, 0) });
        return doc;
    }
}
