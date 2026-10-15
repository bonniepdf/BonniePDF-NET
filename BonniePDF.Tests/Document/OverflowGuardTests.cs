using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Document;

/// <summary>
/// The top-level pagination loop: an element may span any number of pages while it makes
/// progress, and is abandoned after two fresh pages on which it still cannot fit.
/// Mirrored by the TypeScript suite's overflow-guard tests.
/// </summary>
public partial class OverflowGuardTests
{
    [Fact]
    public void OneElementCanSpanMoreThan500Pages()
    {
        var element = new ScriptedElement(i => i < 799 ? RenderStatus.Overflow : RenderStatus.Complete);
        var after = new ScriptedElement(_ => RenderStatus.Complete);
        var doc = new BonniePDF.Document();
        doc.Add(element);
        doc.Add(after);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(800, element.Calls);
        Assert.Equal(800, PageCount(bytes));
        Assert.Equal(799, after.LastPageIndex);
    }

    [Fact]
    public void AnElementThatCannotFitIsAbandonedAfterTwoFreshPages()
    {
        var element = new ScriptedElement(_ => RenderStatus.WrapRequired);
        var after = new ScriptedElement(_ => RenderStatus.Complete);
        var doc = new BonniePDF.Document();
        doc.Add(element);
        doc.Add(after);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(3, element.Calls);
        Assert.Equal(3, PageCount(bytes));
        Assert.Equal(2, after.LastPageIndex);
    }

    [Fact]
    public void ProgressResetsTheRetryCount()
    {
        RenderStatus[] pattern = [RenderStatus.WrapRequired, RenderStatus.WrapRequired, RenderStatus.Overflow];
        var element = new ScriptedElement(i => i < 9 ? pattern[i % 3] : RenderStatus.Complete);
        var doc = new BonniePDF.Document();
        doc.Add(element);

        byte[] bytes = doc.GenerateBytes();

        Assert.Equal(10, element.Calls);
        Assert.Equal(10, PageCount(bytes));
    }

    private static int PageCount(byte[] pdf) => PageObject().Count(Encoding.Latin1.GetString(pdf));

    [GeneratedRegex(@"/Type\s*/Page(?![a-zA-Z])")]
    private static partial Regex PageObject();

    /// <summary>Returns the scripted status for each RenderStateful call.</summary>
    private sealed class ScriptedElement(Func<int, RenderStatus> results) : IStatefulElement
    {
        public int Calls { get; private set; }
        public int LastPageIndex { get; private set; } = -1;

        public float MeasureHeight(float availableWidth) => 10;
        public float GetPreferredWidth() => 0;
        public void Render(RenderContext ctx, float width) { }

        public RenderStatus RenderStateful(RenderContext ctx, float width)
        {
            ctx.CurrentY += 10;
            LastPageIndex = ctx.CurrentPageIndex;
            return results(Calls++);
        }

        public void ResetState() { }
    }
}
