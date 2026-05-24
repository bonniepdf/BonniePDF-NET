using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Integration.WebApi;

/// <summary>
/// Demonstrates invoice generation for Web APIs.
/// This shows a realistic scenario of generating invoices with dynamic data.
/// </summary>
[System.ComponentModel.DisplayName("Web API - Invoice")]
[System.ComponentModel.Category("Integration")]
[System.ComponentModel.Description("Demonstrates invoice generation for Web APIs with customer information, line items table, and dynamic data.")]
public static class WebApiInvoiceExample
{

    public static global::BonniePDF.Document Generate()
    {
        // For demonstration, we'll save to file instead of returning bytes
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(Color.Slate600);

        doc.Metadata.Title = $"Invoice INV-{DateTime.Now.Year}-001";
        doc.Metadata.Author = "MyCompany";

        // Header
        doc.Add(new TextBlock
        {
            Text = $"INVOICE #INV-{DateTime.Now.Year}-001",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Customer info
        doc.Add(new TextBlock { Text = "Customer: John Doe" });
        doc.Add(new TextBlock { Text = $"Date: {DateTime.Now:yyyy-MM-dd}", Margin = new Thickness(0, 0, 0, 16) });

        // Sample table
        Table table = new(3, 1, 2, 1); // column weights
        table.AddHeader(
            new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Qty", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Price", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Total", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Web Development Services", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Hosting Setup", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$500.00", FontSize = 10 },
            new TextBlock { Text = "$500.00", FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Domain Registration", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$50.00", FontSize = 10 },
            new TextBlock { Text = "$50.00", FontSize = 10 }
        );

        doc.Add(table);
        return doc;
    }

    /// <summary>
    /// Example: Invoice generation endpoint
    /// </summary>
    public static async Task<byte[]> GenerateInvoiceAsync(string invoiceNumber, string customerName)
    {
        Document doc = new();

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(Color.Slate600);

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
        doc.Add(new TextBlock { Text = $"Customer: {customerName}" });
        doc.Add(new TextBlock { Text = $"Date: {DateTime.Now:yyyy-MM-dd}", Margin = new Thickness(0, 0, 0, 16) });

        // Sample table
        Table table = new(3, 1, 2, 1); // column weights
        table.AddHeader(
            new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Qty", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Price", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Total", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Web Development Services", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontSize = 10 },
            new TextBlock { Text = "$5,000.00", FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Hosting Setup", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$500.00", FontSize = 10 },
            new TextBlock { Text = "$500.00", FontSize = 10 }
        );
        table.AddRow(
            new TextBlock { Text = "Domain Registration", FontSize = 10 },
            new TextBlock { Text = "1", FontSize = 10 },
            new TextBlock { Text = "$50.00", FontSize = 10 },
            new TextBlock { Text = "$50.00", FontSize = 10 }
        );

        doc.Add(table);

        // Generate to memory
        using MemoryStream ms = new();
        await doc.SaveAsync(ms);

        return ms.ToArray();
    }
}


