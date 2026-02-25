using System;
using System.IO;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._08_Integration;

/// <summary>
/// Demonstrates how to use BonniePDF in ASP.NET Core Web APIs
/// This shows the real-world scenario that was previously impossible
/// </summary>
public class WebApiExample
{
    /// <summary>
    /// Example ASP.NET Core controller action that returns a PDF
    /// This is the pattern used in real web applications
    /// </summary>
    public static async Task<byte[]> GeneratePdfForWebApiAsync()
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        // Set metadata
        doc.Metadata.Title = "Web API PDF";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Creator = "BonniePDF Web API";

        // Add content
        doc.Add(new TextBlock
        {
            Text = "PDF from Web API",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = "This PDF was generated in-memory and returned directly to the client.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 8)
        });

        VerticalStack features = new();
        features.AddChild(new TextBlock
        {
            Text = "Benefits of this approach:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 6)
        });
        features.AddChild(new TextBlock
        {
            Text = "• No temporary files on disk",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better security (sensitive data never hits disk)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Non-blocking async I/O",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Better scalability under load",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });
        features.AddChild(new TextBlock
        {
            Text = "• Cleaner code (no temp file cleanup)",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600
        });

        doc.Add(features);

        using MemoryStream ms = new();
        await doc.SaveAsync(ms);

        return ms.ToArray();
    }

    /// <summary>
    /// Example: Invoice generation endpoint
    /// </summary>
    public static async Task<byte[]> GenerateInvoiceAsync(string invoiceNumber, string customerName)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = $"Invoice {invoiceNumber}";
        doc.Metadata.Author = "MyCompany";

        // Header
        doc.Add(new TextBlock
        {
            Text = $"INVOICE #{invoiceNumber}",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Customer info
        doc.Add(new TextBlock
        {
            Text = $"Customer: {customerName}",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600
        });

        doc.Add(new TextBlock
        {
            Text = $"Date: {DateTime.Now:yyyy-MM-dd}",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 16)
        });

        // Sample table
        Table table = new(3, 1, 2, 1); // column weights
        table.AddHeader(
            new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Qty", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Price", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Total", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Web Development Services", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "1", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Hosting Setup", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "1", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$500.00", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$500.00", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Domain Registration", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "1", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$50.00", FontName = StandardFonts.Helvetica, FontSize = 10 },
            new TextBlock { Text = "$50.00", FontName = StandardFonts.Helvetica, FontSize = 10 }
        );

        doc.Add(table);

        // Generate to memory
        using MemoryStream ms = new();
        await doc.SaveAsync(ms);

        return ms.ToArray();
    }

    /// <summary>
    /// Example: Report generation with leaveOpen=true for streaming
    /// </summary>
    public static async Task GenerateReportToStreamAsync(Stream outputStream)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Monthly Report";
        doc.Metadata.Author = "Report Generator";

        doc.Add(new TextBlock
        {
            Text = "Monthly Sales Report",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        doc.Add(new TextBlock
        {
            Text = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Slate500
        });

        // Save to the provided stream without closing it
        // This allows streaming directly to HTTP response
        await doc.SaveAsync(outputStream, true);
    }
}

/// <summary>
/// Example ASP.NET Core Controller
/// This shows how the above methods would be used in a real API
/// </summary>
/// <code>
/// [ApiController]
/// [Route("api/[controller]")]
/// public class PdfController : ControllerBase
/// {
/// [HttpGet("document")]
/// public async Task<IActionResult> GetDocument()
/// {
/// var pdfBytes = await WebApiExample.GeneratePdfForWebApiAsync();
/// return File(pdfBytes, "application/pdf", "document.pdf");
/// }
/// 
/// [HttpGet("invoice/{invoiceNumber}")]
/// public async Task<IActionResult> GetInvoice(string invoiceNumber, [FromQuery] string customer)
/// {
/// var pdfBytes = await WebApiExample.GenerateInvoiceAsync(invoiceNumber, customer);
/// return File(pdfBytes, "application/pdf", $"invoice-{invoiceNumber}.pdf");
/// }
/// 
/// [HttpGet("report")]
/// public async Task<IActionResult> GetReport()
/// {
/// Response.ContentType = "application/pdf";
/// Response.Headers.Append("Content-Disposition", "inline; filename=report.pdf");
/// 
/// await WebApiExample.GenerateReportToStreamAsync(Response.Body);
/// return new EmptyResult();
/// }
/// }
/// </code>