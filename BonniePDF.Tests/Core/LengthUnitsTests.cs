using System;
using BonniePDF.Fluent;
using Xunit;

namespace BonniePDF.Tests.Core;

/// <summary>
/// The length grammar - shared conformance fixture layout/length-grammar. Any
/// measurement may be written in points, inches, centimetres, or millimetres.
///
/// C# spells this two ways, both resolving through Units.ToPoints: numeric
/// extension methods for object initializers (1.Inches()), and a second Unit
/// argument on the fluent verbs (.Height(1, Unit.Inch)). This differs
/// from TypeScript's widened input type - the capability is the same, the spelling is idiomatic.
///
/// Mirrors core/tests/normalize/to-length.test.ts and
/// core/tests/layout/length-units-end-to-end.test.ts.
/// </summary>
public class LengthUnitsTests
{
    [Fact]
    public void ConvertsEachUnitToPoints()
    {
        Assert.Equal(72f, Units.ToPoints(72f, Unit.Point), 4);
        Assert.Equal(72f, Units.ToPoints(1f, Unit.Inch), 4);
        Assert.Equal(72f, Units.ToPoints(2.54f, Unit.Centimeter), 3);
        Assert.Equal(72f, Units.ToPoints(25.4f, Unit.Millimeter), 3);
    }

    [Fact]
    public void NumericExtensionsMatchTheConversionTable()
    {
        Assert.Equal(Units.ToPoints(1f, Unit.Inch), 1.Inches(), 4);
        Assert.Equal(Units.ToPoints(2.5f, Unit.Centimeter), 2.5.Cm(), 4);
        Assert.Equal(Units.ToPoints(10f, Unit.Millimeter), 10.Mm(), 4);
        Assert.Equal(72f, 72.Points(), 4);
        Assert.Equal(72f, 1f.In(Unit.Inch), 4);
    }

    [Fact]
    public void ParsesEveryUnitToken()
    {
        Assert.Equal(72f, Units.Parse("72pt"), 4);
        Assert.Equal(72f, Units.Parse("1in"), 4);
        Assert.Equal(72f, Units.Parse("2.54cm"), 3);
        Assert.Equal(72f, Units.Parse("25.4mm"), 3);
    }

    [Fact]
    public void ParseIsCaseInsensitiveAndToleratesSpace()
    {
        Assert.Equal(72f, Units.Parse("1IN"), 4);
        Assert.Equal(72f, Units.Parse("1 in"), 4);
        Assert.Equal(72f, Units.Parse("  1 In  "), 4);
    }

    [Fact]
    public void ParseAcceptsSignsBareDecimalsAndExponents()
    {
        Assert.Equal(-72f, Units.Parse("-1in"), 4);
        Assert.Equal(72f, Units.Parse("+1in"), 4);
        Assert.Equal(36f, Units.Parse(".5in"), 4);
        Assert.Equal(Units.Parse("10mm"), Units.Parse("1e1mm"), 4);
    }

    [Fact]
    public void ParseThrowsOnAnUnknownUnitRatherThanReadingZero()
    {
        Assert.Throws<FormatException>(() => Units.Parse("10km"));
        Assert.Throws<FormatException>(() => Units.Parse("inch"));
        Assert.Throws<FormatException>(() => Units.Parse("12"));
        Assert.Throws<FormatException>(() => Units.Parse("1in please"));
    }

    [Fact]
    public void TryParseReportsFailureWithoutThrowing()
    {
        Assert.False(Units.TryParse("10km", out float bad));
        Assert.Equal(0f, bad);
        Assert.True(Units.TryParse("1in", out float good));
        Assert.Equal(72f, good, 4);
    }

    [Fact]
    public void ObjectInitializersTakeUnitQualifiedMeasurements()
    {
        var container = new Container
        {
            Height = 1.Inches(),
            Width = 2.54.Cm(),
            CornerRadius = 2.Mm(),
            Padding = new Thickness(10.Mm()),
        };

        Assert.Equal(72f, container.Height!.Value, 3);
        Assert.Equal(72f, container.Width!.Value, 3);
        Assert.Equal(Units.ToPoints(2f, Unit.Millimeter), container.CornerRadius, 4);
        Assert.Equal(Units.ToPoints(10f, Unit.Millimeter), container.Padding.Top, 4);
    }

    [Fact]
    public void FluentVerbsTakeAUnitAsASecondArgument()
    {
        // The unit rides along with the value as a second argument, and the
        // result must match the points-taking verb exactly.
        Container inches = FluentBox(box => box
            .Height(1, Unit.Inch)
            .Width(2.54f, Unit.Centimeter)
            .Padding(10, Unit.Millimeter)
            .CornerRadius(2, Unit.Millimeter));

        Container points = FluentBox(box => box
            .Height(72f)
            .Width(72f)
            .Padding(Units.ToPoints(10f, Unit.Millimeter))
            .CornerRadius(Units.ToPoints(2f, Unit.Millimeter)));

        Assert.Equal(points.Height!.Value, inches.Height!.Value, 3);
        Assert.Equal(points.Width!.Value, inches.Width!.Value, 3);
        Assert.Equal(points.Padding.Top, inches.Padding.Top, 4);
        Assert.Equal(points.CornerRadius, inches.CornerRadius, 4);
    }

    [Fact]
    public void ADocumentAuthoredInInchesMatchesTheSameDocumentInPoints()
    {
        Assert.Equal(
            WithoutDocumentId(Build(inches: false).GenerateBytes()),
            WithoutDocumentId(Build(inches: true).GenerateBytes()));
    }

    private static BonniePDF.Document Build(bool inches)
    {
        var doc = new BonniePDF.Document();
        doc.Metadata.CreationDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        doc.Metadata.ModifiedDate = doc.Metadata.CreationDate;
        doc.Settings.PageSize = PdfSize.A4;
        doc.Settings.Margin = inches ? 0.5f.Inches() : 36f;

        var stack = new VerticalStack { Spacing = inches ? 0.25f.Inches() : 18f };
        stack.Add(new Container
        {
            Height = inches ? 1.Inches() : 72f,
            Padding = new Thickness(inches ? 0.125f.Inches() : 9f),
            CornerRadius = inches ? 0.0625f.Inches() : 4.5f,
            BackgroundColor = ColorPalette.Slate[100],
            Child = new TextBlock { Text = "units", FontSize = 14f },
        });
        stack.Add(new Spacer { Height = inches ? 0.5f.Inches() : 36f });
        doc.Add(stack);
        return doc;
    }

    /// <summary>Runs a fluent chain over a page's content slot and returns the container it built.</summary>
    private static Container FluentBox(Action<IContentContainer> configure)
    {
        Container? built = null;
        Pdf.Create(b => b.Page(p =>
        {
            IContentContainer slot = p.Content();
            configure(slot);
            built = slot.Child as Container ?? slot as Container;
        }));

        Assert.NotNull(built);
        return built;
    }

    /// <summary>
    /// PDF bytes as text, with the time-derived document /ID masked out: it
    /// differs between any two runs of the same document.
    /// </summary>
    private static string WithoutDocumentId(byte[] bytes)
    {
        string text = System.Text.Encoding.Latin1.GetString(bytes);
        int start = text.IndexOf("/ID [", StringComparison.Ordinal);
        if (start < 0) return text;
        int end = text.IndexOf(']', start);
        if (end < 0) return text;
        return string.Concat(text.AsSpan(0, start), "/ID [MASKED]", text.AsSpan(end + 1));
    }
}
