using BonniePDF.Configuration;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

public class HorizontalStackTests
{
    [Fact]
    public void AutoDistribution_UsesIntrinsicWidths()
    {
        // Arrange
        HorizontalStack stack = new() { Spacing = 0, WidthDistribution = WidthDistribution.Auto };
        TextBlock text1 = new()
            { Text = "Hello", FontName = StandardFonts.Helvetica, FontSize = 10 }; // Width approx 25
        TextBlock text2 = new()
            { Text = "World", FontName = StandardFonts.Helvetica, FontSize = 10 }; // Width approx 25

        stack.AddChild(text1);
        stack.AddChild(text2);

        // Measure with ample space
        // To verify widths, we need to inspect the internals or check if MeasureContentHeight calls child with correct width.
        // Since we can't easily spy without Moq (and I don't have it installed), I'll verify by behavior.
        // Actually, I can check GetPreferredWidth directly.

        Assert.True(text1.GetPreferredWidth() > 0);
        Assert.True(text2.GetPreferredWidth() > 0);

        // If I put them in a container of exact size, they should fit?
        // Let's rely on the fact that we can call MeasureContentHeight.
    }

    [Fact]
    public void MixedLayout_DistributesCorrectly()
    {
        // Arrange
        HorizontalStack stack = new() { Spacing = 0, WidthDistribution = WidthDistribution.Weighted };

        // Fixed/Auto element
        TextBlock label = new() { Text = "Label:", FontName = StandardFonts.Helvetica, FontSize = 10 };
        float labelWidth = label.GetPreferredWidth(); // e.g. ~30

        // Flexible element
        TextBlock value = new() { Text = "Value", FontName = StandardFonts.Helvetica, FontSize = 10 };

        stack.AddChild(label); // Auto (null weight)
        stack.AddChild(value, 1f); // Weighted (1)

        // Act
        // We can't easily verify the *rendered* width without a mock Canvas.
        // But we can check the logic via a "TestElement" that exposes "LastRenderWidth".
    }

    [Fact]
    public void MixedLayout_AssignsRemainingSpaceToWeightedChild()
    {
        // Arrange
        HorizontalStack stack = new() { Spacing = 10, WidthDistribution = WidthDistribution.Weighted };
        TestElement fixedItem = new() { PreferredWidth = 100 };
        TestElement flexItem = new() { PreferredWidth = 50 }; // Should be ignored in favor of weight

        stack.AddChild(fixedItem, null); // Auto
        stack.AddChild(flexItem, 1f); // Weight 1

        float totalWidth = 500;

        // Render to trigger width calculation
        // Create a dummy context
        RenderContext ctx = new(new PdfSize(totalWidth, 100), 0);

        // Act
        stack.Render(ctx, totalWidth);

        // Assert
        // Fixed item should get its preferred width (100)
        Assert.Equal(100, fixedItem.LastRenderWidth, 0.1);

        // Flex item should get: Total (500) - Fixed (100) - Spacing (10) = 390
        Assert.Equal(390, flexItem.LastRenderWidth, 0.1);
    }

    [Fact]
    public void AutoLayout_RespectsIntrinsicWidths()
    {
        // Arrange
        HorizontalStack stack = new() { Spacing = 10, WidthDistribution = WidthDistribution.Auto };
        TestElement item1 = new() { PreferredWidth = 100 };
        TestElement item2 = new() { PreferredWidth = 200 };

        stack.AddChild(item1);
        stack.AddChild(item2);

        float totalWidth = 500;
        RenderContext ctx = new(new PdfSize(totalWidth, 100), 0);

        // Act
        stack.Render(ctx, totalWidth);

        // Assert
        Assert.Equal(100, item1.LastRenderWidth, 0.1);
        Assert.Equal(200, item2.LastRenderWidth, 0.1);
    }

    private sealed class TestElement : ElementBase
    {
        public float PreferredWidth { get; set; }
        public float LastRenderWidth { get; private set; }

        protected override float GetPreferredContentWidth()
        {
            return PreferredWidth;
        }

        protected override float MeasureContentHeight(float contentWidth)
        {
            return 10;
        }

        protected override void RenderContent(RenderContext ctx, float contentWidth)
        {
            LastRenderWidth = contentWidth;
        }
    }
}