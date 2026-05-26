using System.Collections.Generic;
using BonniePDF.Rendering.Tagging;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// this feature acceptance tests for the in-memory structure-tree model.
/// Covers builder invariants, MCID attachment, optional metadata,
/// visitor traversal, and the human-readable tree dump used by later
/// golden tests.
/// </summary>
public class StructureTreeBuilderTests
{
    [Fact]
    public void NewBuilderHasDocumentRoot()
    {
        var builder = new StructureTreeBuilder();
        Assert.Equal(SemanticRole.Document, builder.Root.Role);
        Assert.Same(builder.Root, builder.Current);
        Assert.Empty(builder.Root.Children);
        Assert.Empty(builder.Root.McIds);
    }

    [Fact]
    public void BeginCreatesChildAndMakesItCurrent()
    {
        var builder = new StructureTreeBuilder();
        var h1 = builder.Begin(SemanticRole.H1);

        Assert.Equal(SemanticRole.H1, h1.Role);
        Assert.Same(builder.Root, h1.Parent);
        Assert.Same(h1, builder.Current);
        Assert.Single(builder.Root.Children);
        Assert.Same(h1, builder.Root.Children[0]);
    }

    [Fact]
    public void EndRestoresPreviousCurrent()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        Assert.Equal(SemanticRole.P, builder.Current.Role);
        builder.End();
        Assert.Same(builder.Root, builder.Current);
    }

    [Fact]
    public void NestedBeginEndComposesCorrectly()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.Sect);
        builder.Begin(SemanticRole.H1);
        builder.End();
        builder.Begin(SemanticRole.P);
        builder.End();
        builder.End();

        Assert.Single(builder.Root.Children);
        var sect = builder.Root.Children[0];
        Assert.Equal(SemanticRole.Sect, sect.Role);
        Assert.Equal(2, sect.Children.Count);
        Assert.Equal(SemanticRole.H1, sect.Children[0].Role);
        Assert.Equal(SemanticRole.P, sect.Children[1].Role);
    }

    [Fact]
    public void EndAtDocumentRootThrows()
    {
        var builder = new StructureTreeBuilder();
        Assert.Throws<StructureTreeException>(() => builder.End());
    }

    [Fact]
    public void TooManyEndsThrows()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.End();
        Assert.Throws<StructureTreeException>(() => builder.End());
    }

    [Fact]
    public void AddMcidAttachesToCurrentElement()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0);
        builder.AddMcid(1);
        builder.End();

        var p = builder.Root.Children[0];
        Assert.Equal(new[] { 0, 1 }, p.McIds);
        Assert.Empty(builder.Root.McIds);
    }

    [Fact]
    public void SetAltActualTextLangAndTitleRoundTripOnCurrent()
    {
        var builder = new StructureTreeBuilder();
        var fig = builder.Begin(SemanticRole.Figure);
        builder.SetAlt("Quarterly revenue chart");
        builder.SetActualText("£12.3M");
        builder.SetLang("en-GB");
        builder.SetTitle("Q4 revenue");
        builder.End();

        Assert.Equal("Quarterly revenue chart", fig.Alt);
        Assert.Equal("£12.3M", fig.ActualText);
        Assert.Equal("en-GB", fig.Lang);
        Assert.Equal("Q4 revenue", fig.Title);
    }

    [Fact]
    public void WalkTraversesDepthFirstWithEnterAndLeavePairs()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.Sect);
        builder.Begin(SemanticRole.H1);
        builder.End();
        builder.Begin(SemanticRole.P);
        builder.End();
        builder.End();

        var visitor = new RecordingVisitor();
        builder.Walk(visitor);

        // Expected sequence: enter Document, enter Sect, enter H1, leave H1,
        // enter P, leave P, leave Sect, leave Document.
        Assert.Equal(
            new[]
            {
                "enter:Document", "enter:Sect",
                "enter:H1", "leave:H1",
                "enter:P", "leave:P",
                "leave:Sect", "leave:Document"
            },
            visitor.Events);
    }

    [Fact]
    public void ToTreeStringProducesStableGoldenRepr()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.H1);
        builder.AddMcid(0);
        builder.End();
        var fig = builder.Begin(SemanticRole.Figure);
        builder.AddMcid(1);
        builder.SetAlt("logo");
        builder.End();

        const string expected =
            "Document\n" +
            "  H1 [mcids: 0]\n" +
            "  Figure [mcids: 1] /Alt=logo\n";

        Assert.Equal(expected, builder.Root.ToTreeString());
    }

    [Fact]
    public void RoleToPdfTagMatchesEnumName()
    {
        Assert.Equal("H1", SemanticRole.H1.ToPdfTag());
        Assert.Equal("Figure", SemanticRole.Figure.ToPdfTag());
        Assert.Equal("Artifact", SemanticRole.Artifact.ToPdfTag());
        Assert.Equal("BlockQuote", SemanticRole.BlockQuote.ToPdfTag());
    }

    private sealed class RecordingVisitor : IStructureVisitor
    {
        public List<string> Events { get; } = new();
        public void VisitEnter(StructureElement element) => Events.Add("enter:" + element.Role.ToPdfTag());
        public void VisitLeave(StructureElement element) => Events.Add("leave:" + element.Role.ToPdfTag());
    }
}
