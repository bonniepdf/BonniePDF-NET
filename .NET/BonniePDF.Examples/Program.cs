using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BonniePDF.Examples.Examples._01_Basics;
using BonniePDF.Examples.Examples._02_Layout;
using BonniePDF.Examples.Examples._03_Typography;
using BonniePDF.Examples.Examples._04_Elements;
using BonniePDF.Examples.Examples._05_Charting;
using BonniePDF.Examples.Examples._06_Security;
using BonniePDF.Examples.Examples._07_Templates;

namespace BonniePDF.Examples;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           BonniePDF - PDF Generation Examples                ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Create output directory
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        // Define all examples to generate
        List<PdfExample> examples = new()
        {
            new PdfExample("Invoice/Quotation", "invoice.pdf", path => InvoiceExample.Generate(path)),
            new PdfExample("Users Directory (2-page PDF)", "users_list.pdf", path => UsersTableExample.Generate(path)),
            new PdfExample("MoviesData Collection", "movies_list.pdf", path => MoviesTableExample.Generate(path)),
            new PdfExample("Book Page (formatted text)", "book_page.pdf", path => BookPageExample.Generate(path)),
            new PdfExample("Simple Document (new Document API)", "simple_document.pdf",
                path => SimpleDocumentExample.Generate(path)),
            new PdfExample("All Page Sizes", "all_page_sizes.pdf", path => AllPagesExample.GenerateAllPageSizes(path)),
            new PdfExample("Margin Support", "margin_example.pdf", path => MarginExample.Generate(path)),
            new PdfExample("Hyperlink Support", "hyperlink_example.pdf", path => HyperlinkExample.Generate(path)),
            new PdfExample("Image Support (JPEG/PNG)", "image_example.pdf", path => ImageExample.Generate(path)),
            new PdfExample("Encryption (Password Protected)", "encrypted_password.pdf",
                path => EncryptionExample.GeneratePasswordProtected(path),
                ["   Password: demo123"]),
            new PdfExample("Encryption (With Permissions)", "encrypted_permissions.pdf",
                path => EncryptionExample.GenerateWithPermissions(path),
                ["   User password: user", "   Owner password: owner"]),
            new PdfExample("Encryption (Owner Only)", "encrypted_owner_only.pdf",
                path => EncryptionExample.GenerateOwnerPasswordOnly(path),
                ["   Opens without password, restricted actions", "   Owner password: secret"]),
            new PdfExample("Lists (Ordered & Unordered)", "list_example.pdf", path => ListExample.Generate(path)),
            new PdfExample("Horizontal Stack (Side-by-Side Layout)", "horizontal_stack_example.pdf",
                path => HorizontalStackExample.Generate(path)),
            new PdfExample("Charting (All Charts)", "charting_example.pdf", path => ChartingExample.Generate(path)),
            new PdfExample("TextBlock Overflow (Multi-page Flow)", "textblock_overflow.pdf",
                path => FlowArchitectureExample.GenerateTextBlockOverflow(path)),
            new PdfExample("VerticalStack Overflow (Multi-page Flow)", "verticalstack_overflow.pdf",
                path => FlowArchitectureExample.GenerateVerticalStackOverflow(path)),
            new PdfExample("Headers & Footers (Page Templates)", "headers_footers.pdf",
                path => FlowArchitectureExample.GenerateWithHeadersAndFooters(path)),
            new PdfExample("Flow Demo (Table + Text)", "flow_demo.pdf",
                path => FlowArchitectureExample.GenerateComprehensiveFlowDemo(path)),
            new PdfExample("Justified Text Alignment", "justified_text.pdf",
                path => JustifiedTextExample.Generate(path)),
            new PdfExample("Text Decorations (Underline/Strikethrough)", "text_decorations.pdf",
                path => TextDecorationExample.Generate(path)),
            new PdfExample("Watermark (DRAFT)", "watermark_draft.pdf",
                path => WatermarkExample.GenerateDraft(path)),
            new PdfExample("Watermark (CONFIDENTIAL)", "watermark_confidential.pdf",
                path => WatermarkExample.GenerateConfidential(path)),
            new PdfExample("Watermark (Fluent API)", "watermark_fluent.pdf",
                path => WatermarkExample.GenerateFluent(path)),
            new PdfExample("Rounded Rectangles", "rounded_rectangles.pdf",
                path => RoundedRectangleExample.Generate(path)),
            new PdfExample("Rich Text (Mixed Styling)", "rich_text.pdf",
                path => RichTextExample.Generate(path)),
            new PdfExample("Emoji & Supplementary Unicode", "emoji_example.pdf",
                path => EmojiExample.Generate(path))
        };

        //// Define conditional examples (may be skipped)
        //var conditionalExamples = new List<ConditionalPdfExample>
        //{
        //    new("Embedded Font (Unicode support)", "embedded_font.pdf", path => EmbeddedFontExample.Generate(path),
        //          "No TTF font available"),
        //    new("JPEG Image Support", "jpeg_images.pdf", path => JpegImageExample.Generate(path),
        //          "Image file not found"),
        //    new("PNG Image Support", "png_images.pdf", path => PngImageExample.Generate(path),
        //          "PNG generation failed"),
        //};

        //// Define async examples
        //var asyncExamples = new List<AsyncPdfExample>
        //{
        //    new("Async PDF Generation", "async_document.pdf", path => AsyncExample.GenerateAsync(path)),
        //    new("Stream-based PDF (in-memory)", "stream_document.pdf",
        //        async path => { File.WriteAllBytes(path, await AsyncExample.GenerateToMemoryAsync()); }),
        //    new("Web API PDF", "webapi_document.pdf",
        //        async path => { File.WriteAllBytes(path, await WebApiExample.GeneratePdfForWebApiAsync()); }),
        //};

        // Track generated files for opening
        List<string> generatedFiles = new();

        // Run standard examples
        int exampleNumber = 1;
        foreach (PdfExample example in examples)
        {
            string filePath = RunExample(example, outputDir, exampleNumber++);
            if (filePath != null)
                generatedFiles.Add(filePath);
        }

        // Run conditional examples
        //foreach (var example in conditionalExamples)
        //{
        //    var filePath = RunConditionalExample(example, outputDir, exampleNumber++);
        //    if (filePath != null)
        //        generatedFiles.Add(filePath);
        //}

        //// Run async examples
        //foreach (var example in asyncExamples)
        //{
        //    var filePath = RunAsyncExample(example, outputDir, exampleNumber++).GetAwaiter().GetResult();
        //    if (filePath != null)
        //        generatedFiles.Add(filePath);
        //}

        Console.WriteLine();
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    All examples generated!                   ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Output directory: {outputDir}");
        Console.WriteLine();

        // Open all generated PDFs
        OpenPdfs(generatedFiles);

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    private static string? RunExample(PdfExample example, string outputDir, int number)
    {
        Console.WriteLine($"Generating Example {number}: {example.Description}...");
        string filePath = Path.Combine(outputDir, example.FileName);
        example.Generate(filePath);
        Console.WriteLine($"   ✓ Created: {filePath}");

        if (example.AdditionalInfo != null)
            foreach (string info in example.AdditionalInfo)
                Console.WriteLine($"   {info}");

        return filePath;
    }

    private static string? RunConditionalExample(ConditionalPdfExample example, string outputDir, int number)
    {
        Console.WriteLine($"Generating Example {number}: {example.Description}...");
        string filePath = Path.Combine(outputDir, example.FileName);
        object result = example.Generate(filePath);

        if (result != null)
        {
            Console.WriteLine($"   ✓ Created: {filePath}");
            return filePath;
        }

        Console.WriteLine($"   ⚠ Skipped: {example.SkipMessage}");
        return null;
    }

    private static async Task<string?> RunAsyncExample(AsyncPdfExample example, string outputDir, int number)
    {
        Console.WriteLine($"Generating Example {number}: {example.Description}...");
        string filePath = Path.Combine(outputDir, example.FileName);
        await example.GenerateAsync(filePath);
        Console.WriteLine($"   ✓ Created: {filePath}");
        return filePath;
    }

    private static void OpenPdfs(IEnumerable<string> filePaths)
    {
        foreach (string filePath in filePaths) OpenPdf(filePath);
    }

    private static void OpenPdf(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"   ✗ File not found: {filePath}");
                return;
            }

            ProcessStartInfo psi = new()
            {
                FileName = filePath,
                UseShellExecute = true
            };

            Process.Start(psi);
            Console.WriteLine($"   ✓ Opening: {Path.GetFileName(filePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ✗ Error opening {Path.GetFileName(filePath)}: {ex.Message}");
        }
    }

    // Helper classes for organizing examples
    private sealed record PdfExample(
        string Description,
        string FileName,
        Action<string> Generate,
        string[]? AdditionalInfo = null
    );

    private sealed record ConditionalPdfExample(
        string Description,
        string FileName,
        Func<string, object?> Generate,
        string SkipMessage
    );

    private sealed record AsyncPdfExample(
        string Description,
        string FileName,
        Func<string, Task> GenerateAsync
    );
}