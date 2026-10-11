using Xunit;

namespace BonniePDF.Tests.Content;

[Collection("Serial")]
public class TextStorageTests
{
    [Fact]
    public void FeatureOverridesRemainOwnedValidatedAndIndependentAfterReset()
    {
        var first = new TextBlock("ffi");
        var second = new TextBlock("ffi");
        Assert.Null(first.FontFeatures);
        first.FontFeatures = null;
        var input = new Dictionary<string, int> { ["liga"] = 0 };
        first.FontFeatures = input;
        input["liga"] = 1;
        Assert.Equal(0, first.FontFeatures!["liga"]);
        Assert.Null(second.FontFeatures);
        Assert.Throws<ArgumentException>(() => first.FontFeatures = new Dictionary<string, int> { ["bad"] = 1 });
        Assert.Equal(0, first.FontFeatures!["liga"]);
        first.FontFeatures = null;
        Assert.Null(first.FontFeatures);
        Assert.Throws<ArgumentOutOfRangeException>(() => first.FontFeatures = new Dictionary<string, int> { ["liga"] = -1 });
        Assert.Null(first.FontFeatures);
        first.FontFeatures = input;
        Assert.Equal(1, first.FontFeatures!["liga"]);
    }

    [Fact]
    public void AmbientFeaturesRemainSnapshotsOfTheirDefaultStyle()
    {
        var previous = TextElementBase.AmbientDefaultStyle.Value;
        try
        {
            var style = new TextStyle { FontFeatures = new Dictionary<string, int> { ["liga"] = 0 } };
            TextElementBase.AmbientDefaultStyle.Value = style;
            var first = new TextBlock("ffi");
            var second = new TextBlock("ffi");
            style.FontFeatures = new Dictionary<string, int> { ["liga"] = 1 };
            first.FontFeatures = null;
            Assert.Equal(0, second.FontFeatures!["liga"]);
            Assert.Null(first.FontFeatures);
            Assert.Equal(1, new TextBlock("ffi").FontFeatures!["liga"]);
        }
        finally
        {
            TextElementBase.AmbientDefaultStyle.Value = previous;
        }
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 0)]
    [InlineData(0, 9)]
    [InlineData(15, 9)]
    public void CachedClampedLayoutSurvivesWidthChangesAndParagraphReset(float indent, float spacing)
    {
        const string text = "A deliberately long paragraph wraps across several lines.\nAnother paragraph also wraps across several lines.";
        var block = new TextBlock(text) { FontSize = 12, MaxLines = 2, FirstLineIndent = indent, ParagraphSpacing = spacing };
        foreach (float width in new[] { 100f, 400f, 70f })
        {
            var fresh = new TextBlock(text) { FontSize = 12, MaxLines = 2, FirstLineIndent = indent, ParagraphSpacing = spacing };
            float expected = fresh.MeasureHeight(width);
            Assert.Equal(expected, block.MeasureHeight(width));
            Assert.Equal(expected, block.MeasureHeight(width));
        }
        block.FirstLineIndent = 0;
        block.ParagraphSpacing = 0;
        block.ResetState();
        Assert.Equal(new TextBlock(text) { FontSize = 12, MaxLines = 2 }.MeasureHeight(400), block.MeasureHeight(400));
        block.FirstLineIndent = 15;
        block.ParagraphSpacing = 9;
        block.ResetState();
        Assert.Equal(new TextBlock(text) { FontSize = 12, MaxLines = 2, FirstLineIndent = 15, ParagraphSpacing = 9 }.MeasureHeight(400), block.MeasureHeight(400));
    }
}
