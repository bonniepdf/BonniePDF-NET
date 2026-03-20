using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Descriptors;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.BookPageLayout;

/// <summary>
/// Renders a typeset book page using the Fluent API.
/// All VerticalStack / Container / property-initialiser constructs from the original are replaced
/// with chained Column/Row descriptors and inline style calls.
/// The document default is set to Times Roman 11pt so body paragraphs need no font calls.
/// </summary>
public static class BookPageLayoutFluentExample
{

    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "The Architecture of Memory";
                metadata.Author = "Professor Halloway";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Body paragraphs: Times Roman 11pt Slate800, justified
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.TimesRoman)
                .WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // ── Chapter header ───────────────────────────────────────────
                    column.Item()
                        .Text("CHAPTER III")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(11)
                        .TextColor(Color.Slate700)
                        .AlignCenter();

                    column.Item()
                        .Text("The Architecture of Memory")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(18)
                        .TextColor(Color.Slate900)
                        .AlignCenter();

                    column.Item()
                        .Text("--------------------------------------------------------------------")
                        .Font(StandardFonts.Courier)
                        .FontSize(8)
                        .TextColor(Color.Slate400)
                        .AlignCenter();

                    column.Item().Spacer(20);

                    // ── Body paragraphs ──────────────────────────────────────────

                    // Paragraph 1
                    Paragraph(column,
                        "The morning light filtered through the high windows of the library, casting long shadows across the ancient oak desks. Professor Halloway stood before the chalkboard, his chalk hovering uncertainly over the surface. He had taught this course for thirty years, yet today, something felt fundamentally different. The students watched him with expectant faces, their pens poised over notebooks, waiting for wisdom that he had dispensed a thousand times before.");

                    // Paragraph 2 — italicised dialogue
                    column.Item()
                        .Text("\"Memory,\" he began, his voice gravelly with age, \"is not a repository of facts, neatly cataloged and retrieved at will. It is something far more complex—a living, breathing architecture that shapes and is shaped by who we are.\"")
                        .Font(StandardFonts.TimesItalic)
                        .TextColor(Color.Slate700)
                        .AlignJustify();

                    // Paragraph 3
                    Paragraph(column,
                        "A hand rose in the third row. Sarah Chen, a doctoral student whose dissertation on neural networks had already garnered attention from several tech companies, regarded him with thoughtful eyes. \"But Professor,\" she asked, \"if memory is architectural, does that imply there's a blueprint? A fundamental structure that underlies all human recollection?\"");

                    // Paragraph 4
                    Paragraph(column,
                        "Halloway smiled, and for a moment, the years seemed to fall away. \"An excellent question, Ms. Chen. The answer is both yes and no. Consider a cathedral. From a distance, it appears to follow a precise architectural plan—spires, nave, transept, all arranged according to centuries-old design principles. Yet upon closer inspection, you find modifications made by generations of builders, each leaving their mark on the structure. Memory is much the same.\"");

                    // Paragraph 5
                    Paragraph(column,
                        "He turned to the board and drew a simple diagram—a series of interconnected nodes that resembled both a neural network and a medieval cathedral's flying buttresses. \"We have what I call the foundational framework: sensory encoding, short-term consolidation, and long-term storage. These are the nave and transept of our memory cathedral, present in every human mind, shaped by millions of years of evolution.\"");

                    // Paragraph 6 — mixed bold + italic in one run
                    column.Item().Margin(0, 0, 0, 0).Text(text =>
                    {
                        text.Span("But then—").Bold();
                        text.Span("he continued, his voice dropping to a conspiratorial whisper—").Italic();
                    });

                    // Paragraph 7
                    Paragraph(column,
                        "\"there are the personal modifications. Each experience, each trauma, each moment of profound joy adds a new room to our internal cathedral, alters an existing corridor, or sometimes—rarely but catastrophically—brings down an entire wing.\"");

                    // Paragraph 8
                    Paragraph(column,
                        "The classroom was silent now. Even the typically restless students in the back row had stopped checking their phones. Halloway could feel the weight of what he was about to share, the culmination of a lifetime's work that had cost him friendships, promotions, and ultimately, his reputation in mainstream academia.");

                    // Paragraph 9 — pull-quote block
                    column.Item()
                        .Margin(5)
                        .Padding(20, 8, 20, 8)
                        .Background(Color.Slate50)
                        .Border(1, Color.Slate300)
                        .Text("We have discovered, through decades of research across multiple disciplines, that the architectural blueprint of memory is not unique to humans. It exists in varying forms across all mammalian species, suggesting an ancient origin predating consciousness itself. More remarkably—and this is where conventional science begins to grow uncomfortable—we have found evidence that this architecture can be influenced, modified, and in some cases, completely redesigned.")
                        .Italic()
                        .FontSize(10)
                        .TextColor(Color.Slate600)
                        .AlignJustify();

                    // Paragraph 10
                    Paragraph(column,
                        "He paused, letting the implications settle over the room like dust in an abandoned house. Sarah Chen's hand had gone to her mouth. Another student, Marcus Webb, who had spent four years in the Army before returning to academia, was staring at the diagram on the board as if seeing it for the first time.");

                    // Paragraph 11 — final
                    Paragraph(column,
                        "\"I know what you're thinking,\" Halloway said softly. \"You're thinking about Alzheimer's. About PTSD. About the millions of people whose internal cathedrals are crumbling, whose memories have become labyrinths with no exit. You're wondering why this isn't headline news. Why pharmaceutical companies aren't beating down my door.\" He looked around the room, making eye contact with each student in turn. \"The answer, unfortunately, is the same reason I've been forced to hold this lecture in a rented classroom at nine o'clock at night. Some discoveries are too dangerous. Some architectures, once revealed, cannot be unseen.\"");

                    // ── Page footer ──────────────────────────────────────────────
                    column.Item().Spacer(16);

                    column.Item()
                        .Text("--------------------------------------------------------------------")
                        .Font(StandardFonts.Courier)
                        .FontSize(8)
                        .TextColor(Color.Slate400)
                        .AlignCenter();

                    column.Item()
                        .Text("42")
                        .Font(StandardFonts.TimesBold)
                        .FontSize(10)
                        .TextColor(Color.Slate500)
                        .AlignCenter();

                    column.Item()
                        .Text("THE ARCHITECTURE OF CONSCIOUSNESS | Cambridge University Press, 2024")
                        .Font(StandardFonts.TimesItalic)
                        .FontSize(8)
                        .TextColor(Color.Slate400)
                        .AlignCenter();
                });
            });
        }).Save(outputPath);

        return outputPath;
    }

    /// <summary>
    /// Adds a justified body paragraph in Times Roman 11pt Slate800 to the column.
    /// All three properties are inherited from the document default.
    /// </summary>
    private static void Paragraph(ColumnDescriptor column, string text)
    {
        column.Item()
            .Text(text)
            .TextColor(Color.Slate800)
            .AlignJustify();
    }
}
