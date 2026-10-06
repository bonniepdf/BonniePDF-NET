using System;
using System.Globalization;
using BonniePDF.Metadata;
using BonniePDF.Rendering.Pdf;
using Xunit;

namespace BonniePDF.Tests.Metadata;

/// <summary>
/// Dates in the document information dictionary and the XMP packet are file
/// syntax, not text for a reader, so they must not follow the culture of the
/// machine that generates the document.
/// </summary>
public class CultureInvariantDateTests
{
    private static readonly DateTimeOffset When = new(2026, 9, 23, 14, 5, 9, TimeSpan.Zero);

    /// <summary>
    /// Runs <paramref name="action"/> under a culture whose default calendar is
    /// not Gregorian (th-TH counts years in the Buddhist era, 543 ahead) and whose
    /// time separator is not a colon (fi-FI uses a full stop).
    /// </summary>
    private static T Under<T>(string culture, Func<T> action)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("fi-FI")]
    public void FormatsPdfDatesInTheInvariantCulture(string culture)
    {
        Assert.Equal("D:20260923140509Z", Under(culture, () => PdfSyntax.FormatDate(When)));
    }

    [Theory]
    [InlineData("th-TH")]
    [InlineData("fi-FI")]
    public void FormatsXmpDatesInTheInvariantCulture(string culture)
    {
        var metadata = new DocumentMetadata { CreationDate = When };
        string xml = Under(culture, () => XmpMetadataWriter.GenerateXmpXml(metadata));
        Assert.Contains("2026-09-23T14:05:09+00:00", xml, StringComparison.Ordinal);
    }
}
