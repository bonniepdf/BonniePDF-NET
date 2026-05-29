using System.Collections.Generic;
using System.Linq;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Rendering.Writers;
using BonniePDF.Security.Encryption;
using Xunit;

namespace BonniePDF.Tests;

/// <summary>
/// Stage-1 prep — /Pg + /ParentTree foundational fix. Drives
/// StructureTreeWriter with explicit page IDs and asserts the
/// per-element /Pg, the spanning-page /MCR layout, and the
/// /ParentTree object's /Nums shape. Pre-existing
/// StructureTreeWriterTests cover the test-mode (no-page) branch.
/// </summary>
public class ParentTreeTests
{
    [Fact]
    public void SinglePageMcids_ProducesBareIntsAndPg()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0, 0);
        builder.AddMcid(1, 0);
        builder.End();

        var (writer, _) = Write(builder, pageIds: new[] { 200, 201 });
        var p = writer.Objects.First(o => o.Dict.TryGetValue("/S", out var s) && s == "/P");

        Assert.Equal("[ 0 1 ]", p.Dict["/K"]);
        Assert.Equal("200 0 R", p.Dict["/Pg"]);
    }

    [Fact]
    public void MultiPageMcids_ProducesMcrDictsWithExplicitPg()
    {
        // Same MCID number on different pages — exactly what
        // page-break-aware tagging produces after a NewPage transition.
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0, 0);
        builder.AddMcid(0, 1);
        builder.End();

        var (writer, _) = Write(builder, pageIds: new[] { 200, 201 });
        var p = writer.Objects.First(o => o.Dict.TryGetValue("/S", out var s) && s == "/P");

        Assert.False(p.Dict.ContainsKey("/Pg"));
        Assert.Equal(
            "[ << /Type /MCR /Pg 200 0 R /MCID 0 >> << /Type /MCR /Pg 201 0 R /MCID 0 >> ]",
            p.Dict["/K"]);
    }

    [Fact]
    public void StructTreeRoot_CarriesParentTreeAndNextKey()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0, 0);
        builder.End();

        var (writer, rootId) = Write(builder, pageIds: new[] { 200, 201 });
        var root = writer.Objects.First(o => o.Dict.TryGetValue("/Type", out var t) && t == "/StructTreeRoot");

        Assert.Equal(rootId, root.Id);
        Assert.True(root.Dict.ContainsKey("/ParentTree"));
        Assert.Equal("2", root.Dict["/ParentTreeNextKey"]);
    }

    [Fact]
    public void ParentTreeNums_MapsPageKeysToStructElemArrays()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0, 0);
        builder.End();
        builder.Begin(SemanticRole.H1);
        builder.AddMcid(0, 1);
        builder.End();

        var (writer, _) = Write(builder, pageIds: new[] { 200, 201 });
        var parentTree = writer.Objects.First(o => o.Dict.ContainsKey("/Nums"));
        var p = writer.Objects.First(o => o.Dict.TryGetValue("/S", out var s) && s == "/P");
        var h1 = writer.Objects.First(o => o.Dict.TryGetValue("/S", out var s) && s == "/H1");

        Assert.Equal($"[ 0 [ {p.Id} 0 R ] 1 [ {h1.Id} 0 R ] ]", parentTree.Dict["/Nums"]);
    }

    [Fact]
    public void EmptyPages_ShowUpInParentTreeNumsAsEmptyArrays()
    {
        var builder = new StructureTreeBuilder();
        var (writer, _) = Write(builder, pageIds: new[] { 200, 201 });
        var parentTree = writer.Objects.First(o => o.Dict.ContainsKey("/Nums"));

        Assert.Equal("[ 0 [ ] 1 [ ] ]", parentTree.Dict["/Nums"]);
    }

    [Fact]
    public void NoPageIds_BehavesAsBefore_NoPgNoParentTree()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0);
        builder.End();

        var writer = new FakeWriter();
        new StructureTreeWriter(writer).Write(builder); // no pageIds

        var p = writer.Objects.First(o => o.Dict.TryGetValue("/S", out var s) && s == "/P");
        Assert.False(p.Dict.ContainsKey("/Pg"));
        Assert.Equal("[ 0 ]", p.Dict["/K"]);

        var root = writer.Objects.First(o => o.Dict.TryGetValue("/Type", out var t) && t == "/StructTreeRoot");
        Assert.False(root.Dict.ContainsKey("/ParentTree"));
    }

    // ── Fake writer helpers ─────────────────────────────────────────

    private static (FakeWriter writer, int structTreeRootId) Write(StructureTreeBuilder builder, IList<int> pageIds)
    {
        var writer = new FakeWriter();
        int rootId = new StructureTreeWriter(writer).Write(builder, pageIds);
        return (writer, rootId);
    }

    private sealed record EmittedObject(int Id, IDictionary<string, string> Dict);

    private sealed class FakeWriter : IPdfWriter
    {
        private int _nextId = 100;
        public List<EmittedObject> Objects { get; } = new();
        public PdfEncryptor Encryptor { get; set; }
        public int GetNewId() => _nextId++;

        public void WriteObject(int id, IDictionary<string, string> dict, string streamContent = null)
            => Objects.Add(new EmittedObject(id, new Dictionary<string, string>(dict)));

        public void WriteRawObject(int id, string body) { }

        public void WriteCompressedObject(int id, IDictionary<string, string> dict, string content) => WriteObject(id, dict, content);
        public void WriteBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) => WriteObject(id, dict);
        public void WriteCompressedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) => WriteObject(id, dict);
        public int WriteLinkAnnotation(int id, Core.Hyperlink hyperlink, IList<int> pageIds = null, int? structParentKey = null) => id;
        public int WriteViewerPreferences(int id, Configuration.DocumentSettings settings) => id;
        public void WriteCatalog(int id, int pagesId, int firstPageId, Configuration.DocumentSettings settings, CatalogOptions options) { }
        public void WriteUnencryptedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) { }
        public void Close(int rootId, int infoId = 0) { }
        public int WriteEncryptDictionary(int id) => id;
    }
}
