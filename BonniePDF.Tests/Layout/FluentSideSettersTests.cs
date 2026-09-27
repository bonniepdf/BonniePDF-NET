using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using Xunit;

namespace BonniePDF.Tests.Layout;

/// <summary>
/// The one-sided fluent setters (PaddingTop, MarginLeft, BorderBottom, ...) change only their own side
/// or sides, so calls chain and accumulate. Each used to assign a whole value and zero the others, so
/// only the last call in a chain survived.
/// </summary>
public class FluentSideSettersTests
{
    // The Container the fluent chain builds, captured while the page handler runs.
    private static Container Chain(Func<IContentContainer, IContentContainer> chain)
    {
        Container? built = null;
        Pdf.Create(pdf => pdf.Page(page => built = (Container)chain(page.Content())));
        return built!;
    }

    [Fact]
    public void PaddingVerticalThenHorizontal_KeepsBoth()
    {
        Container c = Chain(x => x.PaddingVertical(7).PaddingHorizontal(3));

        Assert.Equal(new Thickness(3, 7, 3, 7), c.Padding);
    }

    [Fact]
    public void PaddingTop_LeftThenBottom_KeepsAllThree()
    {
        Container c = Chain(x => x.PaddingLeft(1).PaddingTop(2).PaddingBottom(3));

        Assert.Equal(new Thickness(1, 2, 0, 3), c.Padding);
    }

    [Fact]
    public void PaddingWithUnits_Accumulates()
    {
        Container c = Chain(x => x.PaddingLeft(1, Unit.Centimeter).PaddingTop(1, Unit.Inch));

        Assert.Equal(72f / 2.54f, c.Padding.Left, 3); // one centimetre in points
        Assert.Equal(72f, c.Padding.Top, 3);
    }

    [Fact]
    public void ALaterUniformPadding_StillReplacesEverything()
    {
        Container c = Chain(x => x.PaddingLeft(1).Padding(5));

        Assert.Equal(new Thickness(5), c.Padding);
    }

    [Fact]
    public void MarginSides_Accumulate()
    {
        Container c = Chain(x => x.MarginVertical(4).MarginHorizontal(6));

        Assert.Equal(new Thickness(6, 4, 6, 4), c.Margin);
    }

    [Fact]
    public void BorderSides_Accumulate_WithTheirOwnColours()
    {
        Color red = Color.Red;
        Color blue = Color.Blue;
        Container c = Chain(x => x.BorderBottom(2, red).BorderTop(1, blue));

        Assert.Equal(2, c.Border.Bottom);
        Assert.Equal(1, c.Border.Top);
        Assert.Equal(0, c.Border.Left);
        Assert.Equal(red, c.BorderColors.Bottom);
        Assert.Equal(blue, c.BorderColors.Top);
    }

    [Fact]
    public void ALaterUniformBorder_StillReplacesEverything()
    {
        Container c = Chain(x => x.BorderBottom(2).Border(1));

        Assert.Equal(new BonniePDF.Core.Border(1), c.Border);
    }
}
