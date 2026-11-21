using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.ComplianceArchival.PdfCompliance;

/// <summary>
/// Minimal PDF/A-4f with PDF/UA-2 fixture consumed by the independent veraPDF release gate.
/// Keep this logically equivalent to the TypeScript gate fixture.
/// </summary>
public static class PdfA4fValidationExample
{
    /// <summary>Builds the document. Export the result with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "BonniePDF PDF/A-4f validation sample",
                Author = "BonniePDF validation harness",
                Producer = "BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance = PdfAConformance.PdfA4f,
                Accessibility = AccessibilityMode.PdfUa2,
                Language = "en-GB",
            },
        };

        doc.Add(new Heading("PDF/A-4f validation sample") { Level = 1 });
        doc.Add(new TextBlock
        {
            Text = "An archival document that carries its source data: an XML file and a CSV file with no stated type.",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
        });

        doc.Attach("readings.xml", Encoding.UTF8.GetBytes("<readings><reading value=\"42\"/></readings>"),
            AttachmentRelationship.Source, "text/xml");
        doc.Attach("readings.csv", Encoding.UTF8.GetBytes("value\n42\n"), AttachmentRelationship.Data);

        return doc;
    }
}
