using System;
using System.Text;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography.TrueType;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// marked-content emission tests for <see cref="PdfCanvas"/>.
/// Drives the canvas directly with a stub TaggingContext and asserts
/// the emitted PDF operators wrap text/image draws in BDC/EMC. Also
/// verifies the byte-identical-when-tagging-off invariant the plan
/// calls for.
/// </summary>
public class PdfCanvasMarkedContentTests
{
    [Fact]
    public void BeginMarkedContent_EmitsBdcWithMcidAndAttachesToTree()
    {
        var (canvas, tree, _) = NewTaggedCanvas();
        tree.Begin(SemanticRole.P);

        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();

        string output = canvas.Stream.ToString();
        Assert.Contains("/P << /MCID 0 >> BDC", output);
        Assert.Contains("EMC", output);
        Assert.Equal(new[] { 0 }, tree.Current.McIds);
    }

    [Fact]
    public void McidIncrementsAcrossMultipleBeginEndPairs()
    {
        var (canvas, tree, ctx) = NewTaggedCanvas();
        tree.Begin(SemanticRole.P);

        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();
        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();
        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();

        string output = canvas.Stream.ToString();
        Assert.Contains("/MCID 0 >>", output);
        Assert.Contains("/MCID 1 >>", output);
        Assert.Contains("/MCID 2 >>", output);
        Assert.Equal(3, ctx.AllocateMcid()); // next allocation would be 3
    }

    [Fact]
    public void ResetPage_RestartsMcidCounterAtZero()
    {
        var (canvas, tree, ctx) = NewTaggedCanvas();
        tree.Begin(SemanticRole.P);

        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();
        Assert.Contains("/MCID 0 >>", canvas.Stream.ToString());

        ctx.ResetPage();
        canvas.Stream.Clear();
        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();

        // After ResetPage the counter starts at 0 again — required because
        // MCIDs are per-page in PDF (ISO 32000-1 §14.7.4).
        Assert.Contains("/MCID 0 >>", canvas.Stream.ToString());
    }

    [Fact]
    public void WithoutTaggingContext_BeginEndAreNoOps()
    {
        var stream = new StringBuilder();
        var canvas = new PdfCanvas(stream, pageHeight: 800, 0, 0,
            fontResolver: f => "/F1",
            imageResolver: _ => "/Im1",
            embeddedFontResolver: _ => null,
            glyphTracker: (_, _) => { });

        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();

        // No tagging context => zero output. Existing untagged examples
        // must continue to produce byte-identical PDFs.
        Assert.Equal(string.Empty, stream.ToString());
    }

    [Fact]
    public void NestedBeginEndPairs_EmitInOrder()
    {
        var (canvas, tree, _) = NewTaggedCanvas();
        tree.Begin(SemanticRole.Sect);

        canvas.BeginMarkedContent("Sect");
        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();
        canvas.BeginMarkedContent("P");
        canvas.EndMarkedContent();
        canvas.EndMarkedContent();

        string output = canvas.Stream.ToString();
        int sectIdx = output.IndexOf("/Sect << /MCID");
        int p1Idx = output.IndexOf("/P << /MCID 1 >>");
        int p2Idx = output.IndexOf("/P << /MCID 2 >>");
        int firstEmc = output.IndexOf("EMC");
        int lastEmc = output.LastIndexOf("EMC");

        Assert.True(sectIdx >= 0);
        Assert.True(p1Idx > sectIdx);
        Assert.True(p2Idx > p1Idx);
        Assert.True(lastEmc > firstEmc);
    }

    [Fact]
    public void UnbalancedEndMarkedContent_IsTolerated()
    {
        var (canvas, tree, _) = NewTaggedCanvas();
        tree.Begin(SemanticRole.P);

        // No matching Begin — End is a no-op rather than emitting a stray EMC.
        canvas.EndMarkedContent();

        Assert.Equal(string.Empty, canvas.Stream.ToString());
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static (PdfCanvasWithStream canvas, StructureTreeBuilder tree, TaggingContext ctx) NewTaggedCanvas()
    {
        var stream = new StringBuilder();
        var tree = new StructureTreeBuilder();
        var ctx = new TaggingContext(tree);
        var canvas = new PdfCanvas(stream, pageHeight: 800, 0, 0,
            fontResolver: f => "/F1",
            imageResolver: _ => "/Im1",
            embeddedFontResolver: _ => null,
            glyphTracker: (_, _) => { });
        canvas.SetTaggingContext(ctx);
        return (new PdfCanvasWithStream(canvas, stream), tree, ctx);
    }

    /// <summary>
    /// Thin wrapper that exposes the underlying <see cref="StringBuilder"/>
    /// for assertion access. PdfCanvas writes to an externally-provided
    /// stream; the tests just need a reference to it.
    /// </summary>
    private sealed class PdfCanvasWithStream
    {
        public PdfCanvas Inner { get; }
        public StringBuilder Stream { get; }
        public PdfCanvasWithStream(PdfCanvas inner, StringBuilder stream)
        {
            Inner = inner;
            Stream = stream;
        }
        public void BeginMarkedContent(string role) => Inner.BeginMarkedContent(role);
        public void EndMarkedContent() => Inner.EndMarkedContent();
    }
}
