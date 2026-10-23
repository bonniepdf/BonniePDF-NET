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
    public static TheoryData<string, string, string> ByFormat()
    {
        TheoryData<string, string, string> data = new();
        data.Add("G", "0", "0");
        data.Add("G", "1", "1");
        data.Add("G", "12.5", "12.5");
        data.Add("G", "2.5", "2.5");
        data.Add("G", "3.5", "3.5");
        data.Add("G", "-3.5", "-3.5");
        data.Add("G", "0.5", "0.5");
        data.Add("G", "-0.5", "-0.5");
        data.Add("G", "100", "100");
        data.Add("G", "0.1", "0.1");
        data.Add("G", "0.3", "0.3");
        data.Add("G", "1234.5", "1234.5");
        data.Add("G", "10000", "10000");
        data.Add("G", "999.995", "999.995");
        data.Add("G", "1.005", "1.005");
        data.Add("G", "-0.001", "-0.001");
        data.Add("G", "1234567.891", "1234567.891");
        data.Add("G", "-1234567.5", "-1234567.5");
        data.Add("G", "0.000001", "0.000001");
        data.Add("G", "0.0000001", "0.0000001");
        data.Add("G", "1000000000000000000000", "1000000000000000000000");
        data.Add("F0", "0", "0");
        data.Add("F0", "1", "1");
        data.Add("F0", "12.5", "13");
        data.Add("F0", "2.5", "3");
        data.Add("F0", "3.5", "4");
        data.Add("F0", "-3.5", "-4");
        data.Add("F0", "0.5", "1");
        data.Add("F0", "-0.5", "-1");
        data.Add("F0", "100", "100");
        data.Add("F0", "0.1", "0");
        data.Add("F0", "0.3", "0");
        data.Add("F0", "1234.5", "1235");
        data.Add("F0", "10000", "10000");
        data.Add("F0", "999.995", "1000");
        data.Add("F0", "1.005", "1");
        data.Add("F0", "-0.001", "0");
        data.Add("F0", "1234567.891", "1234568");
        data.Add("F0", "-1234567.5", "-1234568");
        data.Add("F0", "0.000001", "0");
        data.Add("F0", "0.0000001", "0");
        data.Add("F0", "1000000000000000000000", "1000000000000000000000");
        data.Add("F2", "0", "0.00");
        data.Add("F2", "1", "1.00");
        data.Add("F2", "12.5", "12.50");
        data.Add("F2", "2.5", "2.50");
        data.Add("F2", "3.5", "3.50");
        data.Add("F2", "-3.5", "-3.50");
        data.Add("F2", "0.5", "0.50");
        data.Add("F2", "-0.5", "-0.50");
        data.Add("F2", "100", "100.00");
        data.Add("F2", "0.1", "0.10");
        data.Add("F2", "0.3", "0.30");
        data.Add("F2", "1234.5", "1234.50");
        data.Add("F2", "10000", "10000.00");
        data.Add("F2", "999.995", "1000.00");
        data.Add("F2", "1.005", "1.01");
        data.Add("F2", "-0.001", "0.00");
        data.Add("F2", "1234567.891", "1234567.89");
        data.Add("F2", "-1234567.5", "-1234567.50");
        data.Add("F2", "0.000001", "0.00");
        data.Add("F2", "0.0000001", "0.00");
        data.Add("F2", "1000000000000000000000", "1000000000000000000000.00");
        data.Add("N0", "0", "0");
        data.Add("N0", "1", "1");
        data.Add("N0", "12.5", "13");
        data.Add("N0", "2.5", "3");
        data.Add("N0", "3.5", "4");
        data.Add("N0", "-3.5", "-4");
        data.Add("N0", "0.5", "1");
        data.Add("N0", "-0.5", "-1");
        data.Add("N0", "100", "100");
        data.Add("N0", "0.1", "0");
        data.Add("N0", "0.3", "0");
        data.Add("N0", "1234.5", "1,235");
        data.Add("N0", "10000", "10,000");
        data.Add("N0", "999.995", "1,000");
        data.Add("N0", "1.005", "1");
        data.Add("N0", "-0.001", "0");
        data.Add("N0", "1234567.891", "1,234,568");
        data.Add("N0", "-1234567.5", "-1,234,568");
        data.Add("N0", "0.000001", "0");
        data.Add("N0", "0.0000001", "0");
        data.Add("N0", "1000000000000000000000", "1,000,000,000,000,000,000,000");
        data.Add("N2", "0", "0.00");
        data.Add("N2", "1", "1.00");
        data.Add("N2", "12.5", "12.50");
        data.Add("N2", "2.5", "2.50");
        data.Add("N2", "3.5", "3.50");
        data.Add("N2", "-3.5", "-3.50");
        data.Add("N2", "0.5", "0.50");
        data.Add("N2", "-0.5", "-0.50");
        data.Add("N2", "100", "100.00");
        data.Add("N2", "0.1", "0.10");
        data.Add("N2", "0.3", "0.30");
        data.Add("N2", "1234.5", "1,234.50");
        data.Add("N2", "10000", "10,000.00");
        data.Add("N2", "999.995", "1,000.00");
        data.Add("N2", "1.005", "1.01");
        data.Add("N2", "-0.001", "0.00");
        data.Add("N2", "1234567.891", "1,234,567.89");
        data.Add("N2", "-1234567.5", "-1,234,567.50");
        data.Add("N2", "0.000001", "0.00");
        data.Add("N2", "0.0000001", "0.00");
        data.Add("N2", "1000000000000000000000", "1,000,000,000,000,000,000,000.00");
        data.Add("P0", "0", "000%");
        data.Add("P0", "1", "100%");
        data.Add("P0", "12.5", "1,250%");
        data.Add("P0", "2.5", "250%");
        data.Add("P0", "3.5", "350%");
        data.Add("P0", "-3.5", "-350%");
        data.Add("P0", "0.5", "50%");
        data.Add("P0", "-0.5", "-50%");
        data.Add("P0", "100", "10,000%");
        data.Add("P0", "0.1", "10%");
        data.Add("P0", "0.3", "30%");
        data.Add("P0", "1234.5", "123,450%");
        data.Add("P0", "10000", "1,000,000%");
        data.Add("P0", "999.995", "100,000%");
        data.Add("P0", "1.005", "101%");
        data.Add("P0", "-0.001", "0%");
        data.Add("P0", "1234567.891", "123,456,789%");
        data.Add("P0", "-1234567.5", "-123,456,750%");
        data.Add("P0", "0.000001", "0%");
        data.Add("P0", "0.0000001", "0%");
        data.Add("P0", "1000000000000000000000", "100,000,000,000,000,000,000,000%");
        data.Add("P1", "0", "000.0%");
        data.Add("P1", "1", "100.0%");
        data.Add("P1", "12.5", "1,250.0%");
        data.Add("P1", "2.5", "250.0%");
        data.Add("P1", "3.5", "350.0%");
        data.Add("P1", "-3.5", "-350.0%");
        data.Add("P1", "0.5", "50.0%");
        data.Add("P1", "-0.5", "-50.0%");
        data.Add("P1", "100", "10,000.0%");
        data.Add("P1", "0.1", "10.0%");
        data.Add("P1", "0.3", "30.0%");
        data.Add("P1", "1234.5", "123,450.0%");
        data.Add("P1", "10000", "1,000,000.0%");
        data.Add("P1", "999.995", "99,999.5%");
        data.Add("P1", "1.005", "100.5%");
        data.Add("P1", "-0.001", "-0.1%");
        data.Add("P1", "1234567.891", "123,456,789.1%");
        data.Add("P1", "-1234567.5", "-123,456,750.0%");
        data.Add("P1", "0.000001", "0.0%");
        data.Add("P1", "0.0000001", "0.0%");
        data.Add("P1", "1000000000000000000000", "100,000,000,000,000,000,000,000.0%");
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
