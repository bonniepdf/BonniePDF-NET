using System.Globalization;
using BonniePDF;

namespace BonniePDF.Examples.GettingStarted.SimpleDocument;

/// <summary>A one-page invoice in a few lines: document title and language, a heading, a two-column table and a total.</summary>
[System.ComponentModel.DisplayName("Your First PDF")]
[System.ComponentModel.Category("Getting Started")]
[System.ComponentModel.Description("Create a short invoice with a title and language, a heading, a table and a total, and export it as PDF.")]
public static class FirstPdfExample
{
    /// <summary>Builds the document. Export it with GenerateBytes() or GenerateBytesAsync().</summary>
    public static Document Generate() => Generate(now: null);

    public static Document Generate(DateTimeOffset? now, CultureInfo? culture = null, TimeZoneInfo? timeZone = null)
    {
        DateTime day = TimeZoneInfo.ConvertTime(now ?? DateTimeOffset.UtcNow, timeZone ?? TimeZoneInfo.Local).Date;
        culture ??= CultureInfo.GetCultureInfo("en-GB");
        string DateText(DateTime value) => value.ToString(string.Equals(culture.Name, "en-US", StringComparison.Ordinal) ? "MM/dd/yyyy" : "d", culture);
        string title = $"Invoice #{day.Year}-014";
        NumberFormatInfo currencyFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        currencyFormat.CurrencySymbol = "£";
        string Money(decimal amount) => amount.ToString("C2", currencyFormat).Replace(' ', '\u00A0');
        Document doc = new() { Metadata = new DocumentMetadata { Title = title } };
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(ColorPalette.Slate[700]);

        doc.Add(new Heading(title)
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 26,
            Color = ColorPalette.Slate[900]
        });
        doc.Add(new TextBlock
        {
            Text = "Northlight Studio · 14 Harbour Square, Bristol BS1 4QA",
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 0, 0, 24)
        });
        doc.Add(new TextBlock
        {
            Text = $"Billed to Sample Customer Ltd · issued {DateText(day)} · due {DateText(day.AddDays(14))}",
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Cells take plain strings; a TextBlock gives a cell its own alignment.
        var items = new Table(4f, 1f) { HeaderBackgroundColor = ColorPalette.Slate[800], HeaderTextColor = Color.White };
        items.AddHeader("Description", new TextBlock { Text = "Amount", Alignment = TextAlignment.Right });
        items.AddRow("Brand workshop, two days", new TextBlock { Text = Money(1800), Alignment = TextAlignment.Right });
        items.AddRow("Logo and colour palette", new TextBlock { Text = Money(950), Alignment = TextAlignment.Right });
        doc.Add(items);

        doc.Add(new Line { Thickness = 1, Color = ColorPalette.Slate[300], Margin = new Thickness(0, 12, 0, 8) });
        doc.Add(new TextBlock
        {
            Text = $"Total due  {Money(2750)}",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            Alignment = TextAlignment.Right
        });
        doc.Add(new TextBlock
        {
            Text = $"Pay by bank transfer to Northlight Studio, sort code 00-00-00, account 12345678, quoting INV-{day.Year}-014.",
            FontSize = 9,
            Color = ColorPalette.Slate[500],
            Margin = new Thickness(0, 28, 0, 0)
        });
        return doc;
    }
}
