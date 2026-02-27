using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._03_Typography;

/// <summary>
/// Generates a formatted textual page that reads like a page from a book
/// </summary>
public class BookPageExample
{
    public static string Generate(string outputPath = "book_page.pdf")
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 60
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "The Architecture of Memory";
        doc.Metadata.Author = "Professor Halloway";

        // Book Header
        VerticalStack headerStack = new();

        // Chapter Title
        headerStack.AddChild(new TextBlock
        {
            Text = "CHAPTER III",
            FontName = StandardFonts.TimesBold,
            FontSize = 11,
            TextColor = Color.Slate700
        }.AlignCenter());

        // Main Title
        headerStack.AddChild(new TextBlock
        {
            Text = "The Architecture of Memory",
            FontName = StandardFonts.TimesBold,
            FontSize = 18,
            TextColor = Color.Slate900
        }.AlignCenter());

        // Decorative Line
        headerStack.AddChild(new TextBlock
        {
            Text = "--------------------------------------------------------------------",
            FontName = StandardFonts.Courier,
            FontSize = 8,
            TextColor = Color.Slate400
        }.AlignCenter());

        // Main Content - Book Style TextBlock
        VerticalStack contentStack = new();

        // Paragraph 1
        contentStack.AddChild(CreateParagraph(
            "The morning light filtered through the high windows of the library, casting long shadows across the ancient oak desks. Professor Halloway stood before the chalkboard, his chalk hovering uncertainly over the surface. He had taught this course for thirty years, yet today, something felt fundamentally different. The students watched him with expectant faces, their pens poised over notebooks, waiting for wisdom that he had dispensed a thousand times before."
        ));

        // Paragraph 2 with italic emphasis
        VerticalStack para2Stack = new();
        para2Stack.AddChild(new TextBlock
        {
            Text =
                "\"Memory,\" he began, his voice gravelly with age, \"is not a repository of facts, neatly cataloged and retrieved at will. It is something far more complex—a living, breathing architecture that shapes and is shaped by who we are.\"",
            FontName = StandardFonts.TimesItalic,
            FontSize = 11,
            TextColor = Color.Slate700
        });
        contentStack.AddChild(para2Stack);

        // Paragraph 3
        contentStack.AddChild(CreateParagraph(
            "A hand rose in the third row. Sarah Chen, a doctoral student whose dissertation on neural networks had already garnered attention from several tech companies, regarded him with thoughtful eyes. \"But Professor,\" she asked, \"if memory is architectural, does that imply there's a blueprint? A fundamental structure that underlies all human recollection?\""
        ));

        // Paragraph 4
        contentStack.AddChild(CreateParagraph(
            "Halloway smiled, and for a moment, the years seemed to fall away. \"An excellent question, Ms. Chen. The answer is both yes and no. Consider a cathedral. From a distance, it appears to follow a precise architectural plan—spires, nave, transept, all arranged according to centuries-old design principles. Yet upon closer inspection, you find modifications made by generations of builders, each leaving their mark on the structure. Memory is much the same.\""
        ));

        // Paragraph 5
        contentStack.AddChild(CreateParagraph(
            "He turned to the board and drew a simple diagram—a series of interconnected nodes that resembled both a neural network and a medieval cathedral's flying buttresses. \"We have what I call the foundational framework: sensory encoding, short-term consolidation, and long-term storage. These are the nave and transept of our memory cathedral, present in every human mind, shaped by millions of years of evolution.\""
        ));

        // Paragraph 6 with emphasis
        VerticalStack para6Stack = new();
        para6Stack.AddChild(new TextBlock
        {
            Text = "But then—",
            FontName = StandardFonts.TimesBold,
            FontSize = 11,
            TextColor = Color.Slate800
        });
        para6Stack.AddChild(new TextBlock
        {
            Text = "he continued, his voice dropping to a conspiratorial whisper—",
            FontName = StandardFonts.TimesItalic,
            FontSize = 11,
            TextColor = Color.Slate700
        });
        contentStack.AddChild(para6Stack);

        // Paragraph 7
        contentStack.AddChild(CreateParagraph(
            "\"there are the personal modifications. Each experience, each trauma, each moment of profound joy adds a new room to our internal cathedral, alters an existing corridor, or sometimes—rarely but catastrophically—brings down an entire wing.\""
        ));

        // Paragraph 8
        contentStack.AddChild(CreateParagraph(
            "The classroom was silent now. Even the typically restless students in the back row had stopped checking their phones. Halloway could feel the weight of what he was about to share, the culmination of a lifetime's work that had cost him friendships, promotions, and ultimately, his reputation in mainstream academia."
        ));

        // Paragraph 9 - Quotation block
        VerticalStack quoteStack = new() { Margin = new Thickness(5) };
        quoteStack.AddChild(new TextBlock
        {
            Text =
                "We have discovered, through decades of research across multiple disciplines, that the architectural blueprint of memory is not unique to humans. It exists in varying forms across all mammalian species, suggesting an ancient origin predating consciousness itself. More remarkably—and this is where conventional science begins to grow uncomfortable—we have found evidence that this architecture can be influenced, modified, and in some cases, completely redesigned.",
            FontName = StandardFonts.TimesItalic,
            FontSize = 10,
            TextColor = Color.Slate600
        });

        Container quoteBlock = new()
        {
            Child = quoteStack,
            BackgroundColor = Color.Slate50,
            Border = new Border(1),
            BorderColors = new BorderColors(Color.Slate300),
            Padding = new Thickness(20, 8, 20, 8)
        };
        contentStack.AddChild(quoteBlock);

        // Paragraph 10
        contentStack.AddChild(CreateParagraph(
            "He paused, letting the implications settle over the room like dust in an abandoned house. Sarah Chen's hand had gone to her mouth. Another student, Marcus Webb, who had spent four years in the Army before returning to academia, was staring at the diagram on the board as if seeing it for the first time."
        ));

        // Paragraph 11 - Final paragraph
        contentStack.AddChild(CreateParagraph(
            "\"I know what you're thinking,\" Halloway said softly. \"You're thinking about Alzheimer's. About PTSD. About the millions of people whose internal cathedrals are crumbling, whose memories have become labyrinths with no exit. You're wondering why this isn't headline news. Why pharmaceutical companies aren't beating down my door.\" He looked around the room, making eye contact with each student in turn. \"The answer, unfortunately, is the same reason I've been forced to hold this lecture in a rented classroom at nine o'clock at night. Some discoveries are too dangerous. Some architectures, once revealed, cannot be unseen.\""
        ));

        // Page Footer
        VerticalStack footerStack = new();
        footerStack.AddChild(new TextBlock
        {
            Text = "--------------------------------------------------------------------",
            FontName = StandardFonts.Courier,
            FontSize = 8,
            TextColor = Color.Slate400
        }.AlignCenter());

        TextBlock pageNumbers = new TextBlock
        {
            Text = "42",
            FontName = StandardFonts.TimesBold,
            FontSize = 10,
            TextColor = Color.Slate500
        }.AlignCenter();

        footerStack.AddChild(pageNumbers);
        footerStack.AddChild(new TextBlock
        {
            Text = "THE ARCHITECTURE OF CONSCIOUSNESS | Cambridge University Press, 2024",
            FontName = StandardFonts.TimesItalic,
            FontSize = 8,
            TextColor = Color.Slate400
        }.AlignCenter());

        // Main Layout
        VerticalStack mainStack = new();
        mainStack.AddChild(headerStack);
        mainStack.AddChild(contentStack);
        mainStack.AddChild(footerStack);

        // Add to document
        doc.Add(mainStack);

        // Save PDF
        doc.Save(outputPath);

        return outputPath;
    }

    private static TextBlock CreateParagraph(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontName = StandardFonts.TimesRoman,
            FontSize = 11,
            TextColor = Color.Slate800,
        };
    }
}