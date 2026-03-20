using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.TextBlockOverflow;

/// <summary>
/// Generates a document with a long TextBlock that flows across multiple pages.
/// </summary>
[System.ComponentModel.DisplayName("TextBlock Overflow")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("TextBlock overflow flow architecture.")]
public static class TextBlockOverflowExample
{
    
    public static string Generate(string outputPath)
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.FooterTemplate = pageNumber => new TextBlock
        {
            Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Slate500,
            Alignment = TextAlignment.Right
        };

        doc.Metadata.Title = "TextBlock Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates text flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Long Text Document",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Generate a long text that will overflow
        string[] paragraphs = new[]
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.",
            "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.",
            "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur?",
            "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga."
        };

        // Repeat paragraphs to create enough content for multiple pages
        string longText = string.Join("\n\n", Enumerable.Range(1, 20).Select(i =>
            $"Section {i}\n\n{paragraphs[i % paragraphs.Length]}\n\n{paragraphs[(i + 1) % paragraphs.Length]}"));

        doc.Add(new TextBlock
        {
            Text = longText,
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate700
        });

        doc.Save(outputPath);
        return outputPath;
    }
}
