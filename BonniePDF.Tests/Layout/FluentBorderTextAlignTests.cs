using BonniePDF.Fluent;
using Xunit;

namespace BonniePDF.Tests.Layout;

public class FluentBorderTextAlignTests
{
    // --- BorderHorizontal / BorderVertical (parity with TS migration of BorderExtensions) ---

    [Fact]
    public void BorderHorizontal_SetsLeftAndRightBorders_MatchingPaddingHorizontalConvention()
    {
        var c = (Container)new Container().BorderHorizontal(2, Color.Red);

        Assert.Equal(2, c.Border.Left);
        Assert.Equal(2, c.Border.Right);
        Assert.Equal(0, c.Border.Top);
        Assert.Equal(0, c.Border.Bottom);
        Assert.Equal(Color.Red, c.BorderColors.Left);
    }

    [Fact]
    public void BorderVertical_SetsTopAndBottomBorders_DefaultsToBlack()
    {
        var c = (Container)new Container().BorderVertical(3);

        Assert.Equal(3, c.Border.Top);
        Assert.Equal(3, c.Border.Bottom);
        Assert.Equal(0, c.Border.Left);
        Assert.Equal(0, c.Border.Right);
        Assert.Equal(Color.Black, c.BorderColors.Top);
    }

    // --- Per-side Margin + Size (parity with TS migration of MarginExtensions / SizeExtensions) ---

    [Fact]
    public void PerSideMarginHelpers_SetOnlyTheirOwnEdge()
    {
        Assert.Equal(5, ((Container)new Container().MarginTop(5)).Margin.Top);
        Assert.Equal(5, ((Container)new Container().MarginBottom(5)).Margin.Bottom);
        Assert.Equal(5, ((Container)new Container().MarginLeft(5)).Margin.Left);
        Assert.Equal(5, ((Container)new Container().MarginRight(5)).Margin.Right);
    }

    [Fact]
    public void MarginHorizontalAndVertical_SetOpposingPairs()
    {
        var h = (Container)new Container().MarginHorizontal(7);
        Assert.Equal(7, h.Margin.Left);
        Assert.Equal(7, h.Margin.Right);
        Assert.Equal(0, h.Margin.Top);
        Assert.Equal(0, h.Margin.Bottom);

        var v = (Container)new Container().MarginVertical(9);
        Assert.Equal(9, v.Margin.Top);
        Assert.Equal(9, v.Margin.Bottom);
        Assert.Equal(0, v.Margin.Left);
        Assert.Equal(0, v.Margin.Right);
    }

    [Fact]
    public void Size_SetsBothWidthAndHeight()
    {
        var c = (Container)new Container().Size(120, 80);

        Assert.Equal(120, c.Width);
        Assert.Equal(80, c.Height);
    }

    // --- TextDescriptor paragraph alignment (parity with TS migration of TextBlockExtensions) ---

    [Fact]
    public void TextDescriptor_DefaultsToLeftAlignment()
    {
        Assert.Equal(TextAlignment.Left, new TextDescriptor().RichText.Alignment);
    }

    [Fact]
    public void TextDescriptor_AlignAndConvenienceMethods_SetParagraphAlignmentAndChain()
    {
        var td = new TextDescriptor();

        Assert.Same(td, td.AlignCenter());
        Assert.Equal(TextAlignment.Center, td.RichText.Alignment);

        td.AlignRight();
        Assert.Equal(TextAlignment.Right, td.RichText.Alignment);

        td.Justify();
        Assert.Equal(TextAlignment.Justify, td.RichText.Alignment);

        td.Align(TextAlignment.Left);
        Assert.Equal(TextAlignment.Left, td.RichText.Alignment);
    }
}
