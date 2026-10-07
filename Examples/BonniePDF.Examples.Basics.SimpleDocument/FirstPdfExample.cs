using BonniePDF;

namespace BonniePDF.Examples.Basics.SimpleDocument;

/// <summary>A first document with a title and language for readers and PDF viewers.</summary>
[System.ComponentModel.DisplayName("Your First PDF")]
[System.ComponentModel.Category("Basics")]
[System.ComponentModel.Description("Create a document, add a text block, and export it as PDF.")]
public static class FirstPdfExample
{
    /// <summary>Builds the document. Export it with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new() { Metadata = new DocumentMetadata { Title = "Invoice #2026-014" } };
        doc.Settings.Language = "en-GB";
        doc.Add(new TextBlock("Invoice #2026-014"));
        return doc;
    }
}
