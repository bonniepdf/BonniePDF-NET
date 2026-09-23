using BonniePDF.Rendering.Pdf;
using Xunit;

namespace BonniePDF.Tests.Rendering;

/// <summary>
/// The double overload of FormatDecimal must print what the TypeScript
/// formatter prints - toFixed(3) with trailing zeros, a bare point and a
/// negative zero trimmed - for the same value. The expected strings below are
/// that formatter's output.
/// </summary>
public class PdfSyntaxFormatDecimalTests
{
    [Theory]
    // A custom format such as "0.###" rounds to 15 significant digits first,
    // turning these into ties and printing 779.693 / 0.001.
    [InlineData(779.6924999999999, "779.692")]
    [InlineData(779.69249999999994, "779.692")]
    [InlineData(0.0004999999999999999, "0")]
    [InlineData(-0.0001, "0")]
    [InlineData(11d, "11")]
    [InlineData(12.5, "12.5")]
    [InlineData(-3.25, "-3.25")]
    // Exact ties (odd sixteenths): "F3" rounds these to even, toFixed away from zero.
    [InlineData(0.0625, "0.063")]
    [InlineData(-0.0625, "-0.063")]
    [InlineData(2.3125, "2.313")]
    [InlineData(0.1875, "0.188")]
    [InlineData(1e-9, "0")]
    [InlineData(123456.7895, "123456.789")]
    public void RoundsTheExactValueAsTypeScriptDoes(double value, string expected) =>
        Assert.Equal(expected, PdfSyntax.FormatDecimal(value));
}
