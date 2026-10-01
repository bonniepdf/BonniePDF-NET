using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.BookPageLayout;

/// <summary>
/// A typeset chapter page in the style of an academic monograph. The chapter label
/// is a plain paragraph, the chapter title is a semantic Heading, and the body
/// is ordinary TextBlock prose. The pull-quote block is a bordered Container with outer
/// horizontal margins to inset it from the text column. PDF/A-2a + PDF/UA-1.
/// </summary>
[System.ComponentModel.DisplayName("Book Page Layout")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("Book-style typeset chapter page with Times family fonts, a pull-quote block, and a chapter footer. PDF/A-2a + PDF/UA-1.")]
public static class BookPageLayoutExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "The Architecture of Memory",
                Author   = "Professor Halloway",
                Subject  = "Chapter III of 'The Architecture of Consciousness'.",
                Keywords = "pdf, book, typography, pdf/ua, pdf/a, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                PageSize  = PdfSize.A4,
                Margin    = 60,
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.TimesRoman)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[800]),
                HeaderTemplate = _ => new TextBlock
                {
                    Text      = "THE ARCHITECTURE OF CONSCIOUSNESS",
                    FontName  = StandardFonts.TimesItalic,
                    FontSize  = 8,
                    Color = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Center,
                },
                // Viewer page counter starts at 42; footer text matches.
                PageLabel = new PageLabel { StartAt = 42 },
                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = (pageNum + 41).ToString(),
                    FontName  = StandardFonts.TimesBold,
                    FontSize  = 10,
                    Color = ColorPalette.Slate[500],
                    Alignment = TextAlignment.Center,
                },
            },
        };

        // ── Chapter heading ──────────────────────────────────────────────────

        doc.Add(new TextBlock
        {
            Text      = "CHAPTER III",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 11,
            Color = ColorPalette.Slate[700],
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 0, 0, 6),
        });

        doc.Add(new Heading("The Architecture of Memory")
        {
            TocIdentifier = "chapter-iii",
            Level         = 1,
            FontName      = StandardFonts.TimesBold,
            FontSize      = 18,
            Color     = ColorPalette.Slate[900],
            Alignment     = TextAlignment.Center,
            Margin        = new Thickness(0, 0, 0, 20),
        });

        // ── Body ─────────────────────────────────────────────────────────────

        Body(doc,
            "The morning light filtered through the high windows of the library, casting long shadows across " +
            "the ancient oak desks. Professor Halloway stood before the chalkboard, his chalk hovering " +
            "uncertainly over the surface. He had taught this course for thirty years, yet today, something " +
            "felt fundamentally different. The students watched him with expectant faces, their pens poised " +
            "over notebooks, waiting for wisdom that he had dispensed a thousand times before.");

        Body(doc,
            "“Memory,” he began, his voice gravelly with age, “is not a repository of facts, " +
            "neatly cataloged and retrieved at will. It is something far more complex - a living, breathing " +
            "architecture that shapes and is shaped by who we are.”",
            StandardFonts.TimesItalic, ColorPalette.Slate[700]);

        Body(doc,
            "A hand rose in the third row. Sarah Chen, a doctoral student whose dissertation on neural networks " +
            "had already garnered attention from several tech companies, regarded him with thoughtful eyes. " +
            "“But Professor,” she asked, “if memory is architectural, does that imply there’s " +
            "a blueprint? A fundamental structure that underlies all human recollection?”");

        Body(doc,
            "Halloway smiled, and for a moment, the years seemed to fall away. “An excellent question, " +
            "Ms. Chen. The answer is both yes and no. Consider a cathedral. From a distance, it appears to " +
            "follow a precise architectural plan - spires, nave, transept, all arranged according to " +
            "centuries-old design principles. Yet upon closer inspection, you find modifications made by " +
            "generations of builders, each leaving their mark on the structure. Memory is much the same.”");

        Body(doc,
            "He turned to the board and drew a simple diagram - a series of interconnected nodes that " +
            "resembled both a neural network and a medieval cathedral’s flying buttresses. “We have " +
            "what I call the foundational framework: sensory encoding, short-term consolidation, and long-term " +
            "storage. These are the nave and transept of our memory cathedral, present in every human mind, " +
            "shaped by millions of years of evolution.”");

        // "But then -" emphasis break
        VerticalStack emphStack = new() { Margin = new Thickness(0, 0, 0, 10) };
        emphStack.Add(new TextBlock
        {
            Text      = "But then –",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 11,
            Color = ColorPalette.Slate[800],
        });
        emphStack.Add(new TextBlock
        {
            Text      = "he continued, his voice dropping to a conspiratorial whisper –",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 11,
            Color = ColorPalette.Slate[700],
        });
        doc.Add(emphStack);

        Body(doc,
            "“there are the personal modifications. Each experience, each trauma, each moment of profound " +
            "joy adds a new room to our internal cathedral, alters an existing corridor, or sometimes - rarely " +
            "but catastrophically - brings down an entire wing.”");

        Body(doc,
            "The classroom was silent now. Even the typically restless students in the back row had stopped " +
            "checking their phones. Halloway could feel the weight of what he was about to share, the " +
            "culmination of a lifetime’s work that had cost him friendships, promotions, and ultimately, " +
            "his reputation in mainstream academia.");

        // ── Pull-quote block ─────────────────────────────────────────────────

        VerticalStack quoteInner = new();
        quoteInner.Add(new TextBlock
        {
            Text      = "We have discovered, through decades of research across multiple disciplines, that the " +
                        "architectural blueprint of memory is not unique to humans. It exists in varying forms " +
                        "across all mammalian species, suggesting an ancient origin predating consciousness " +
                        "itself. More remarkably - and this is where conventional science begins to grow " +
                        "uncomfortable - we have found evidence that this architecture can be influenced, " +
                        "modified, and in some cases, completely redesigned.",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 10,
            Color = ColorPalette.Slate[600],
            Alignment = TextAlignment.Justify,
        });

        doc.Add(new Container
        {
            Child           = quoteInner,
            BackgroundColor = ColorPalette.Slate[50],
            Border          = new Border(1),
            BorderColors    = new BorderColors(ColorPalette.Slate[300]),
            Padding         = new Thickness(20, 8, 20, 8),
            Margin          = new Thickness(20, 10, 20, 10),
        });

        Body(doc,
            "He paused, letting the implications settle over the room like dust in an abandoned house. " +
            "Sarah Chen’s hand had gone to her mouth. Another student, Marcus Webb, who had spent four " +
            "years in the Army before returning to academia, was staring at the diagram on the board as if " +
            "seeing it for the first time.");

        doc.Add(new TextBlock
        {
            Text      = "“I know what you’re thinking,” Halloway said softly. “You’re " +
                        "thinking about Alzheimer’s. About PTSD. About the millions of people whose " +
                        "internal cathedrals are crumbling, whose memories have become labyrinths with no exit. " +
                        "You’re wondering why this isn’t headline news. Why pharmaceutical companies " +
                        "aren’t beating down my door.” He looked around the room, making eye contact " +
                        "with each student in turn. “The answer, unfortunately, is the same reason I’ve " +
                        "been forced to hold this lecture in a rented classroom at nine o’clock at night. " +
                        "Some discoveries are too dangerous. Some architectures, once revealed, cannot be unseen.”",
            FontName  = StandardFonts.TimesRoman,
            FontSize  = 11,
            Color = ColorPalette.Slate[800],
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 10),
        });

        return doc;
    }

    private static void Body(Document doc, string text, string? fontName = null, Color? color = null)
    {
        doc.Add(new TextBlock
        {
            Text      = text,
            FontName  = fontName ?? StandardFonts.TimesRoman,
            FontSize  = 11,
            Color = color ?? ColorPalette.Slate[800],
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 10),
        });
    }
}
