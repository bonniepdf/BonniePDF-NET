using System;
using System.Collections.Generic;
using BonniePDF.Examples.Examples._01_Basics;
using BonniePDF.Examples.Examples._02_Layout;
using BonniePDF.Examples.Examples._03_Typography;
using BonniePDF.Examples.Examples._04_Elements;
using BonniePDF.Examples.Examples._05_Charting;
using BonniePDF.Examples.Examples._05_Charting.Components;
using BonniePDF.Examples.Examples._06_Security;
using BonniePDF.Examples.Examples._07_Templates;
using BonniePDF.Examples.Examples._08_Integration;
using BonniePDF.Examples.Models;

namespace BonniePDF.Examples;

/// <summary>
/// Provides a centralized, robust registry for executing BonniePDF examples by ID.
/// </summary>
public static class ExampleRunner
{
    public static readonly List<ExampleCategory> Categories = new()
    {
        new ExampleCategory { Id = "01", Name = "01_Basics", Examples = new() {
            new ExampleInfo { Id = "MarginsAndUnits", Title = "Margins & Units", Description = "Shows how to configure page margins and use different units.", FileName = "margin_example.pdf", GenerateAction = path => MarginExample.Generate(path) },
            new ExampleInfo { Id = "PageSettings", Title = "Page Settings", Description = "Demonstrates orientation, sizes, and backgrounds.", FileName = "all_page_sizes.pdf", GenerateAction = path => AllPagesExample.GenerateAllPageSizes(path) },
            new ExampleInfo { Id = "SimpleDocument", Title = "Simple Document", Description = "A basic text-only PDF generation example.", FileName = "simple_document.pdf", GenerateAction = path => SimpleDocumentExample.Generate(path) }
        }},
        new ExampleCategory { Id = "02", Name = "02_Layout", Examples = new() {
            new ExampleInfo { Id = "HorizontalStacks", Title = "Horizontal Stacks", Description = "Demonstrates flex-like horizontal layout blocks.", FileName = "horizontal_stack_example.pdf", GenerateAction = path => HorizontalStackExample.Generate(path) },
            new ExampleInfo { Id = "MultiPageFlow", Title = "Multi-Page Flow", Description = "Shows how elements seamlessly flow across multiple pages.", FileName = "flow_demo.pdf", GenerateAction = path => FlowArchitectureExample.GenerateComprehensiveFlowDemo(path) },
            new ExampleInfo { Id = "TextBlockOverflow", Title = "TextBlock Overflow", Description = "TextBlock overflow flow architecture.", FileName = "textblock_overflow.pdf", GenerateAction = path => FlowArchitectureExample.GenerateTextBlockOverflow(path) },
            new ExampleInfo { Id = "VerticalStackOverflow", Title = "VerticalStack Overflow", Description = "VerticalStack overflow flow architecture.", FileName = "verticalstack_overflow.pdf", GenerateAction = path => FlowArchitectureExample.GenerateVerticalStackOverflow(path) },
            new ExampleInfo { Id = "HeadersFooters", Title = "Headers & Footers", Description = "Page template headers and footers.", FileName = "headers_footers.pdf", GenerateAction = path => FlowArchitectureExample.GenerateWithHeadersAndFooters(path) }
        }},
        new ExampleCategory { Id = "03", Name = "03_Typography", Examples = new() {
            new ExampleInfo { Id = "BookPageLayout", Title = "Book Page Layout", Description = "Advanced typography layout mimicking a book.", FileName = "book_page.pdf", GenerateAction = path => BookPageExample.Generate(path) },
            new ExampleInfo { Id = "CustomFonts", Title = "Custom Fonts", Description = "Embedding and using custom TTF fonts.", FileName = "embedded_font.pdf", GenerateAction = path => EmbeddedFontExample.Generate(path) },
            new ExampleInfo { Id = "EmojisAndUnicode", Title = "Emojis & Unicode", Description = "Rendering international text and emojis.", FileName = "emoji_example.pdf", GenerateAction = path => EmojiExample.Generate(path) },
            new ExampleInfo { Id = "RichTextStyles", Title = "Rich Text Styles", Description = "Mixed formatting within paragraphs.", FileName = "rich_text.pdf", GenerateAction = path => RichTextExample.Generate(path) },
            new ExampleInfo { Id = "TextAlignment", Title = "Text Alignment", Description = "Left, center, right, and justified text alignment.", FileName = "justified_text.pdf", GenerateAction = path => JustifiedTextExample.Generate(path) },
            new ExampleInfo { Id = "TextDecorations", Title = "Text Decorations", Description = "Underlines, strikethroughs, and text backgrounds.", FileName = "text_decorations.pdf", GenerateAction = path => TextDecorationExample.Generate(path) }
        }},
        new ExampleCategory { Id = "04", Name = "04_Elements", Examples = new() {
            new ExampleInfo { Id = "Hyperlinks", Title = "Hyperlinks", Description = "Internal and external clickable links.", FileName = "hyperlink_example.pdf", GenerateAction = path => HyperlinkExample.Generate(path) },
            new ExampleInfo { Id = "Images", Title = "Images", Description = "Embedding and sizing raster and SVG images.", FileName = "image_example.pdf", GenerateAction = path => ImageExample.Generate(path) },
            new ExampleInfo { Id = "Lists", Title = "Lists", Description = "Ordered and unordered bullet point lists.", FileName = "list_example.pdf", GenerateAction = path => ListExample.Generate(path) },
            new ExampleInfo { Id = "ShapesAndBorders", Title = "Shapes & Borders", Description = "Drawing basic shapes and styling borders.", FileName = "rounded_rectangles.pdf", GenerateAction = path => RoundedRectangleExample.Generate(path) }
        }},
        new ExampleCategory { Id = "05", Name = "05_Charting", Examples = new() {
            new ExampleInfo { Id = "ChartShowcase", Title = "Chart Showcase", Description = "A comprehensive document with various charts.", FileName = "charting_example.pdf", GenerateAction = path => ChartingExample.Generate(path) },
            new ExampleInfo { Id = "AreaChartExample", Title = "Area Chart", Description = "Render an area chart.", FileName = "area_chart.pdf", GenerateAction = path => AreaChartExample.Generate(path) },
            new ExampleInfo { Id = "BarChartExample", Title = "Bar Chart", Description = "Render a bar chart.", FileName = "bar_chart.pdf", GenerateAction = path => BarChartExample.Generate(path) },
            new ExampleInfo { Id = "DotPlotChartExample", Title = "Dot Plot Chart", Description = "Render a dot plot chart.", FileName = "dot_plot.pdf", GenerateAction = path => DotPlotChartExample.Generate(path) },
            new ExampleInfo { Id = "GroupedBarChartExample", Title = "Grouped Bar Chart", Description = "Render a grouped bar chart.", FileName = "grouped_bar_chart.pdf", GenerateAction = path => GroupedBarChartExample.Generate(path) },
            new ExampleInfo { Id = "LineChartExample", Title = "Line Chart", Description = "Render a line chart.", FileName = "line_chart.pdf", GenerateAction = path => LineChartExample.Generate(path) },
            new ExampleInfo { Id = "LollipopChartExample", Title = "Lollipop Chart", Description = "Render a lollipop chart.", FileName = "lollipop_chart.pdf", GenerateAction = path => LollipopChartExample.Generate(path) },
            new ExampleInfo { Id = "PieChartExample", Title = "Pie Chart", Description = "Render a pie chart.", FileName = "pie_chart.pdf", GenerateAction = path => PieChartExample.Generate(path) }
        }},
        new ExampleCategory { Id = "06", Name = "06_Security", Examples = new() {
            new ExampleInfo { Id = "Encryption", Title = "Encryption", Description = "Password protecting and encrypting PDFs.", FileName = "encrypted_password.pdf", GenerateAction = path => EncryptionExample.GeneratePasswordProtected(path), AdditionalInfo = new[] { "   Password: demo123" }, Password = "demo123" },
            new ExampleInfo { Id = "EncryptionPerms", Title = "Encryption Permissions", Description = "Permissions.", FileName = "encrypted_permissions.pdf", GenerateAction = path => EncryptionExample.GenerateWithPermissions(path), AdditionalInfo = new[] { "   User password: user", "   Owner password: owner" }, Password = "user" },
            new ExampleInfo { Id = "EncryptionOwner", Title = "Encryption Owner Only", Description = "Owner only.", FileName = "encrypted_owner_only.pdf", GenerateAction = path => EncryptionExample.GenerateOwnerPasswordOnly(path), AdditionalInfo = new[] { "   Opens without password, restricted actions", "   Owner password: secret" } },
            new ExampleInfo { Id = "WatermarkDraft", Title = "Watermarking DRAFT", Description = "DRAFT watermark.", FileName = "watermark_draft.pdf", GenerateAction = path => WatermarkExample.GenerateDraft(path) },
            new ExampleInfo { Id = "WatermarkConfidential", Title = "Watermarking CONFIDENTIAL", Description = "CONFIDENTIAL watermark.", FileName = "watermark_confidential.pdf", GenerateAction = path => WatermarkExample.GenerateConfidential(path) },
            new ExampleInfo { Id = "WatermarkFluent", Title = "Watermarking Fluent API", Description = "Fluent API watermark.", FileName = "watermark_fluent.pdf", GenerateAction = path => WatermarkExample.GenerateFluent(path) }
        }},
        new ExampleCategory { Id = "07", Name = "07_Templates", Examples = new() {
            new ExampleInfo { Id = "BusinessInvoice", Title = "Business Invoice", Description = "A real-world invoice template generation.", FileName = "invoice.pdf", GenerateAction = path => InvoiceExample.Generate(path) },
            new ExampleInfo { Id = "MovieCatalog", Title = "Movie Catalog", Description = "A visually rich catalog template.", FileName = "movies_list.pdf", GenerateAction = path => MoviesTableExample.Generate(path) },
            new ExampleInfo { Id = "UserDirectory", Title = "User Directory", Description = "A tabular data presentation.", FileName = "users_list.pdf", GenerateAction = path => UsersTableExample.Generate(path) }
        }},
        new ExampleCategory { Id = "08", Name = "08_Integration", Examples = new() {
            new ExampleInfo { Id = "AsyncGeneration", Title = "Async Generation", Description = "Generating PDFs asynchronously.", FileName = "async_document.pdf", GenerateAction = path => AsyncExample.GenerateAsync(path).GetAwaiter().GetResult() },
            new ExampleInfo { Id = "WebApiStream", Title = "Web API Stream", Description = "Streaming a PDF output.", FileName = "webapi_document.pdf", GenerateAction = path => { System.IO.File.WriteAllBytes(path, WebApiExample.GeneratePdfForWebApiAsync().GetAwaiter().GetResult()); } }
        }}
    };

    /// <summary>
    /// Executes the specified example and outputs the PDF to the given path.
    /// </summary>
    public static void GenerateExample(string exampleId, string outputPath)
    {
        ExampleInfo? target = null;
        foreach (var category in Categories)
        {
            var match = category.Examples.Find(e => string.Equals(e.Id, exampleId, StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                continue;
            }
            target = match;
            break;
        }

        if (target is { GenerateAction: not null })
        {
            target.GenerateAction(outputPath);
        }
        else
        {
            throw new ArgumentException($"Example with ID '{exampleId}' was not found in the registry.");
        }
    }

    /// <summary>
    /// Executes the specified example and returns the PDF as a byte array.
    /// </summary>
    public static byte[] GenerateExampleToBytes(string exampleId)
    {
        var tempFile = System.IO.Path.GetTempFileName() + ".pdf";
        try
        {
            GenerateExample(exampleId, tempFile);
            return System.IO.File.ReadAllBytes(tempFile);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }
}
