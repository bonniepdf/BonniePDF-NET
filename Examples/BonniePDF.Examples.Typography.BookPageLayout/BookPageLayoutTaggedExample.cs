using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.BookPageLayout;

/// <summary>
/// PDF/UA-1 tagged book-page-layout example.
///
/// Renders a typeset chapter page in the style of a academic monograph. The chapter number
/// is a /P, the chapter title is an /H1, and each body paragraph is a /P. The pull-quote
/// block is a TextBlock styled with a background and border; it is tagged as /P.
///
/// Acceptance: VeraPDF profile ua1 reports zero failures.
/// </summary>
[System.ComponentModel.DisplayName("Book Page Layout (PDF/UA-1)")]
[System.ComponentModel.Category("Typography")]
[System.ComponentModel.Description("PDF/UA-1 tagged book-style chapter page with chapter heading and body paragraphs.")]
public static class BookPageLayoutTaggedExample
{
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "The Architecture of Memory - Tagged (PDF/UA-1)",
                Author   = "Professor Halloway",
                Subject  = "Chapter III of 'The Architecture of Consciousness'.",
                Keywords = "pdf, pdf/ua, accessibility, book, typography",
            },
            Settings = new DocumentSettings
            {
                PageSize      = PdfSize.A4,
                Margin        = 60,
                Version       = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.TimesRoman)
                    .WithSize(11)
                    .WithColor(Color.Slate800),
            },
        };

        // Chapter label - plain paragraph (not a heading)
        doc.Add(new TextBlock
        {
            Text      = "CHAPTER III",
            FontName  = StandardFonts.TimesBold,
            FontSize  = 11,
            TextColor = Color.Slate700,
            Alignment = TextAlignment.Center,
        });

        // Chapter title - H1
        doc.Add(new Heading("The Architecture of Memory")
        {
            TocIdentifier = "ch3",
            Level         = 1,
            FontName      = StandardFonts.TimesBold,
            FontSize      = 18,
            TextColor     = Color.Slate900,
            Alignment     = TextAlignment.Center,
            Margin        = new Thickness(0, 4, 0, 20),
        });

        // Body paragraphs
        Body(doc,
            "The morning light filtered through the high windows of the library, casting long shadows across " +
            "the ancient oak desks. Professor Halloway stood before the chalkboard, his chalk hovering " +
            "uncertainly over the surface. He had taught this course for thirty years, yet today, something " +
            "felt fundamentally different.");

        Body(doc,
            "“Memory,” he began, his voice gravelly with age, “is not a repository of facts, " +
            "neatly cataloged and retrieved at will. It is something far more complex-a living, breathing " +
            "architecture that shapes and is shaped by who we are.”",
            StandardFonts.TimesItalic);

        Body(doc,
            "A hand rose in the third row. Sarah Chen, a doctoral student whose dissertation on neural networks " +
            "had already garnered attention from several tech companies, regarded him with thoughtful eyes. " +
            "“But Professor,” she asked, “if memory is architectural, does that imply there’s " +
            "a blueprint?”");

        Body(doc,
            "Halloway smiled, and for a moment, the years seemed to fall away. “An excellent question, " +
            "Ms. Chen. Consider a cathedral. From a distance, it appears to follow a precise architectural " +
            "plan-spires, nave, transept. Yet upon closer inspection, you find modifications made by " +
            "generations of builders, each leaving their mark. Memory is much the same.”");

        Body(doc,
            "He turned to the board and drew a simple diagram-a series of interconnected nodes that " +
            "resembled both a neural network and a medieval cathedral’s flying buttresses. “We have " +
            "the foundational framework: sensory encoding, short-term consolidation, and long-term storage. " +
            "These are the nave and transept of our memory cathedral.”");

        // Pull-quote block - still tagged as /P, styled for visual emphasis
        doc.Add(new TextBlock
        {
            Text      = "We have discovered that the architectural blueprint of memory is not unique to humans. " +
                        "It exists in varying forms across all mammalian species, suggesting an ancient origin " +
                        "predating consciousness itself.",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 10,
            TextColor = Color.Slate600,
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(20, 8, 20, 8),
        });

        Body(doc,
            "The classroom was silent now. Even the typically restless students in the back row had stopped " +
            "checking their phones. Halloway could feel the weight of what he was about to share, the " +
            "culmination of a lifetime’s work that had cost him friendships, promotions, and ultimately, " +
            "his reputation in mainstream academia.");

        Body(doc,
            "“I know what you’re thinking,” Halloway said softly. “You’re thinking " +
            "about Alzheimer’s. About PTSD. About the millions of people whose internal cathedrals are " +
            "crumbling. You’re wondering why this isn’t headline news.” He looked around the " +
            "room. “The answer is the same reason I’ve been forced to hold this lecture in a rented " +
            "classroom at nine o’clock at night. Some discoveries are too dangerous.”");

        // Footer line (metadata, not content heading)
        doc.Add(new TextBlock
        {
            Text      = "THE ARCHITECTURE OF CONSCIOUSNESS · Cambridge University Press, 2024",
            FontName  = StandardFonts.TimesItalic,
            FontSize  = 8,
            TextColor = Color.Slate400,
            Alignment = TextAlignment.Center,
            Margin    = new Thickness(0, 20, 0, 0),
        });

        return doc;
    }

    private static void Body(Document doc, string text, string? fontName = null)
    {
        doc.Add(new TextBlock
        {
            Text      = text,
            FontName  = fontName ?? StandardFonts.TimesRoman,
            FontSize  = 11,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Justify,
            Margin    = new Thickness(0, 0, 0, 8),
        });
    }
}
