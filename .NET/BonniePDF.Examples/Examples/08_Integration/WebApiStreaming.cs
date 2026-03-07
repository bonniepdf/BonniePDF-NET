using System;
using System.IO;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Core;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._08_Integration;

/// <summary>
/// Demonstrates report generation with streaming for Web APIs.
/// This shows how to stream PDFs directly to HTTP responses without buffering in memory.
/// </summary>
public class WebApiStreamingExample : ExampleBase
{
    public override string Id => "webapi-streaming";
    public override string Title => "Web API - Streaming";
    public override string Description => "Demonstrates report generation with streaming, writing PDFs directly to response streams without buffering in memory for better scalability.";
    public override string FileName => "webapi_streaming.pdf";
    public override ExampleCategory Category => ExampleCategory.Integration;

    public override string Generate(string outputPath)
    {
        // For demonstration, we'll save to file instead of streaming
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

        doc.Save(outputPath);
        return outputPath;
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
