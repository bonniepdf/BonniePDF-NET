using BonniePDF;

namespace BonniePDF.Examples.ScriptsEmoji.ColourEmojis;

/// <summary>Opt-in colour emoji in a team update, with complete Unicode sequences and embedded vector artwork.</summary>
[System.ComponentModel.DisplayName("Colour Emojis")]
[System.ComponentModel.Category("Scripts & Emoji")]
[System.ComponentModel.Description("A team update where each item is led by a colour emoji, plus joined families, skin tones, flags and keycaps kept whole. PDF/A-2b and PDF/UA-1.")]
public static class ColourEmojiExample
{
    /// <summary>The update's items: the emoji that marks each one, its headline and its text.</summary>
    private static readonly (string Mark, string Title, string Body)[] Items =
    [
        ("🚀", "We launch on Saturday", "The release goes out at 9am. Please keep the morning free for questions from customers."),
        ("👩‍💻", "Welcome, Priya", "Priya joins the platform team on Monday as a senior engineer. Say hello in the kitchen at 11."),
        ("🇬🇧", "A new office in London", "The Shoreditch office opens on 1 December, with desks for twelve and a meeting room."),
        ("👍🏽", "Thank you, support team", "Response times fell below two hours this month, the best since the service began."),
        ("1️⃣", "One priority this week", "Finish the onboarding guide by Friday, so it is ready for the first customers."),
    ];

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
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily(text).WithSize(11).WithColor(ColorPalette.Slate[700]);

        doc.Add(new Heading("Team update") { Level = 1, FontSize = 26, Color = ColorPalette.Slate[900] });
        doc.Add(new TextBlock("Week 45  ·  5 November 2026") { Color = ColorPalette.Slate[500], Margin = new Thickness(0, 2, 0, 18) });

        // Each item is a row: the emoji large in its own column, the news beside it.
        VerticalStack card = new() { Spacing = 16 };
        foreach (var (mark, title, body) in Items)
        {
            HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted, Spacing = 14 };
            row.Add(new TextBlock(mark) { FontName = emoji, FontSize = 30 }, 1);
            VerticalStack news = new() { Spacing = 3 };
            news.Add(new TextBlock(title) { FontSize = 13, Color = ColorPalette.Slate[900] });
            news.Add(new TextBlock(body));
            row.Add(news, 8);
            card.Add(row);
        }
        doc.Add(new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(ColorPalette.Slate[200]),
            CornerRadius = 10,
            Padding = new Thickness(20),
            Margin = new Thickness(0, 0, 0, 22),
            Child = card,
        });

        doc.Add(new Heading("Sequences stay whole") { Level = 2, FontSize = 14, Color = ColorPalette.Slate[900], Margin = new Thickness(0, 0, 0, 6) });
        doc.Add(new TextBlock("Joined families, professions, skin tones, flags and keycaps are single characters to a reader, so they never break across lines or lose a part:")
            { Margin = new Thickness(0, 0, 0, 8) });
        doc.Add(new TextBlock("👨‍👩‍👧‍👦 👩‍🚀 👨‍🍳 👩‍🔬 👨‍💻 👋🏿 🇯🇵 #️⃣") { FontName = emoji, FontSize = 32 });
        doc.Add(new TextBlock("Colour is an explicit choice; the Emojis & Unicode example uses monochrome outlines. Emoji artwork: Twemoji (CC BY 4.0); font by Mozilla. github.com/twitter/twemoji")
            { FontSize = 8, Color = ColorPalette.Slate[500], Margin = new Thickness(0, 22, 0, 0) });
        return doc;
    }
}
