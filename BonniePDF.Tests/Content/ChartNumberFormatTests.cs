using System.Globalization;
using Xunit;

namespace BonniePDF.Tests.Content;

/// <summary>
/// Chart number formatting - shared conformance fixture charts/number-format.
/// The same culture-invariant grammar, on the value's decimal digits, in both implementations.
///
/// Mirrors core/tests/content/chart-number-format.test.ts.
/// </summary>
public class ChartNumberFormatTests
{
    // The fixture's input values, and for each format the expected output for each value in order.
    private static readonly string[] FixtureValues =
    [
        "0", "1", "12.5", "2.5", "3.5", "-3.5", "0.5",
        "-0.5", "100", "0.1", "0.3", "1234.5", "10000", "999.995",
        "1.005", "-0.001", "1234567.891", "-1234567.5", "0.000001", "0.0000001", "1000000000000000000000",
    ];

    private static readonly (string Format, string[] Expected)[] FixtureOutputs =
    [
        ("G",
        [
            "0", "1", "12.5", "2.5", "3.5", "-3.5", "0.5",
            "-0.5", "100", "0.1", "0.3", "1234.5", "10000", "999.995",
            "1.005", "-0.001", "1234567.891", "-1234567.5", "0.000001", "0.0000001", "1000000000000000000000",
        ]),
        ("F0",
        [
            "0", "1", "13", "3", "4", "-4", "1",
            "-1", "100", "0", "0", "1235", "10000", "1000",
            "1", "0", "1234568", "-1234568", "0", "0", "1000000000000000000000",
        ]),
        ("F2",
        [
            "0.00", "1.00", "12.50", "2.50", "3.50", "-3.50", "0.50",
            "-0.50", "100.00", "0.10", "0.30", "1234.50", "10000.00", "1000.00",
            "1.01", "0.00", "1234567.89", "-1234567.50", "0.00", "0.00", "1000000000000000000000.00",
        ]),
        ("N0",
        [
            "0", "1", "13", "3", "4", "-4", "1",
            "-1", "100", "0", "0", "1,235", "10,000", "1,000",
            "1", "0", "1,234,568", "-1,234,568", "0", "0", "1,000,000,000,000,000,000,000",
        ]),
        ("N2",
        [
            "0.00", "1.00", "12.50", "2.50", "3.50", "-3.50", "0.50",
            "-0.50", "100.00", "0.10", "0.30", "1,234.50", "10,000.00", "1,000.00",
            "1.01", "0.00", "1,234,567.89", "-1,234,567.50", "0.00", "0.00", "1,000,000,000,000,000,000,000.00",
        ]),
        ("P0",
        [
            "000%", "100%", "1,250%", "250%", "350%", "-350%", "50%",
            "-50%", "10,000%", "10%", "30%", "123,450%", "1,000,000%", "100,000%",
            "101%", "0%", "123,456,789%", "-123,456,750%", "0%", "0%", "100,000,000,000,000,000,000,000%",
        ]),
        ("P1",
        [
            "000.0%", "100.0%", "1,250.0%", "250.0%", "350.0%", "-350.0%", "50.0%",
            "-50.0%", "10,000.0%", "10.0%", "30.0%", "123,450.0%", "1,000,000.0%", "99,999.5%",
            "100.5%", "-0.1%", "123,456,789.1%", "-123,456,750.0%", "0.0%", "0.0%", "100,000,000,000,000,000,000,000.0%",
        ]),
    ];

    public static TheoryData<string, string, string> ByFormat()
    {
        TheoryData<string, string, string> data = new();
        foreach ((string format, string[] expected) in FixtureOutputs)
        {
            for (int i = 0; i < FixtureValues.Length; i++) data.Add(format, FixtureValues[i], expected[i]);
        }
        return data;
    }

    public static TheoryData<string, string, string> Defaults()
    {
        TheoryData<string, string, string> data = new();
        data.Add("", "1234.5", "1234.5");
        data.Add("F", "1234.5", "1234.50");
        data.Add("N", "1234.5", "1,234.50");
        data.Add("P", "0.256", "25.60%");
        data.Add("f1", "1234.567", "1234.6");
        data.Add("n0", "1234.567", "1,235");
        data.Add("F15", "0.1", "0.100000000000000");
        return data;
    }

    public static TheoryData<string[], string, string> Sums()
    {
        TheoryData<string[], string, string> data = new();
        data.Add(new[] { "0.1", "0.2" }, "G", "0.3");
        data.Add(new[] { "0.1", "0.2", "1234.5678" }, "G", "1234.8678");
        data.Add(new[] { "0.1", "0.2", "1234.5678" }, "F2", "1234.87");
        data.Add(new[] { "1234.5", "2500", "-0.5" }, "N0", "3,734");
        data.Add(new[] { "0.7", "0.1" }, "P0", "80%");
        data.Add(new[] { "1e-7", "2e-7" }, "G", "0.0000003");
        return data;
    }

    private static readonly string[] Unsupported = ["C0", "C", "E2", "D", "D4", "X", "G2", "F16", "N99", "0.00", "#,##0", "N-1", "F 2", "currency"];

    [Theory]
    [MemberData(nameof(ByFormat))]
    public void FormatsEveryFixtureValue(string format, string value, string expected)
    {
        Assert.Equal(expected, ChartNumberFormat.Format(decimal.Parse(value, CultureInfo.InvariantCulture), format));
    }

    [Theory]
    [MemberData(nameof(Defaults))]
    public void TakesTheDefaultDecimalsAnEmptyFormatAsGAndAnyLetterCase(string format, string value, string expected)
    {
        Assert.Equal(expected, ChartNumberFormat.Format(decimal.Parse(value, CultureInfo.InvariantCulture), format));
    }

    [Fact]
    public void ANullFormatIsG()
    {
        Assert.Equal("1234.5", ChartNumberFormat.Format(1234.5m, null));
    }

    [Fact]
    public void RejectsEverythingOutsideTheGrammar()
    {
        foreach (string format in Unsupported)
        {
            Assert.False(ChartNumberFormat.IsSupported(format), format);
            FormatException ex = Assert.Throws<FormatException>(() => ChartNumberFormat.Format(1m, format));
            Assert.Contains("Unsupported chart number format", ex.Message, StringComparison.Ordinal);
        }
    }

    [Theory]
    [MemberData(nameof(Sums))]
    public void AddsTheValuesThenFormats(string[] values, string format, string expected)
    {
        decimal total = values.Sum(v => decimal.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture));
        Assert.Equal(expected, ChartNumberFormat.Format(total, format));
    }

    [Fact]
    public void IgnoresTheCurrentCulture()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            foreach (string name in new[] { "de-DE", "fr-FR", "en-US" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(name);
                Assert.Equal("1,234,567.89", ChartNumberFormat.Format(1234567.891m, "N2"));
                Assert.Equal("25.0%", ChartNumberFormat.Format(0.25m, "P1"));
                Assert.Equal("1234.5", ChartNumberFormat.Format(1234.5m, "G"));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void FormattedValueHonoursLabelFormat()
    {
        Assert.Equal("1,235", new ChartDataPoint { Label = "x", Value = 1234.5m, LabelFormat = "N0" }.FormattedValue);
    }

    [Fact]
    public void FormattedValueDefaultsToG()
    {
        Assert.Equal("12.5", new ChartDataPoint { Label = "x", Value = 12.5m }.FormattedValue);
    }

    [Fact]
    public void FormattedValueThrowsForAnUnsupportedFormat()
    {
        var point = new ChartDataPoint { Label = "x", Value = 1m, LabelFormat = "C0" };
        Assert.Throws<FormatException>(() => point.FormattedValue);
    }
}
