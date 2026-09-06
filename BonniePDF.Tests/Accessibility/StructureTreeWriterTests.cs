using BonniePDF.Rendering.Tagging;
using BonniePDF.Rendering.Writers;
using BonniePDF.Security.Encryption;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// Acceptance tests for <see cref="StructureTreeWriter"/>.
/// Drives the writer with a fake <see cref="IPdfWriter"/> that records
/// every object emitted, so the tests can assert the PDF object graph
/// shape without spinning up a full Document.
/// </summary>
public class StructureTreeWriterTests
{
    [Fact]
    public void WriteEmptyTreeEmitsDocumentStructElemAndStructTreeRoot()
    {
        var (writer, structTreeRootId) = WriteWithFake(new StructureTreeBuilder());

        // Two objects: the Document StructElem and the StructTreeRoot.
        Assert.Equal(2, writer.Objects.Count);

        var documentElem = writer.Objects.First(o => o.Dict["/Type"] == "/StructElem");
        Assert.Equal("/Document", documentElem.Dict["/S"]);
        Assert.Equal($"{structTreeRootId} 0 R", documentElem.Dict["/P"]);
        Assert.False(documentElem.Dict.ContainsKey("/K")); // no children, no MCIDs

        var root = writer.Objects.First(o => o.Dict["/Type"] == "/StructTreeRoot");
        Assert.Equal($"[ {documentElem.Id} 0 R ]", root.Dict["/K"]);
    }

    [Fact]
    public void NestedTreeProducesCorrectParentRefsAndKidsArrays()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.Sect);
        builder.Begin(SemanticRole.H1);
        builder.End();
        builder.Begin(SemanticRole.P);
        builder.End();
        builder.End();

        var (writer, structTreeRootId) = WriteWithFake(builder);

        // Should have 4 StructElem objects + 1 StructTreeRoot.
        var elems = writer.Objects.Where(o => o.Dict["/Type"] == "/StructElem").ToList();
        Assert.Equal(4, elems.Count);

        // Find them by role.
        EmittedObject ByRole(string role) =>
            elems.First(e => e.Dict["/S"] == "/" + role);

        var doc = ByRole("Document");
        var sect = ByRole("Sect");
        var h1 = ByRole("H1");
        var p = ByRole("P");

        // Parent pointers.
        Assert.Equal($"{structTreeRootId} 0 R", doc.Dict["/P"]);
        Assert.Equal($"{doc.Id} 0 R", sect.Dict["/P"]);
        Assert.Equal($"{sect.Id} 0 R", h1.Dict["/P"]);
        Assert.Equal($"{sect.Id} 0 R", p.Dict["/P"]);

        // /K arrays.
        Assert.Equal($"[ {sect.Id} 0 R ]", doc.Dict["/K"]);
        Assert.Equal($"[ {h1.Id} 0 R {p.Id} 0 R ]", sect.Dict["/K"]);
        Assert.False(h1.Dict.ContainsKey("/K"));
        Assert.False(p.Dict.ContainsKey("/K"));
    }

    [Fact]
    public void McIdsAppearAsBareIntsInKidsArrayAfterStructRefs()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.P);
        builder.AddMcid(0);
        builder.AddMcid(1);
        builder.Begin(SemanticRole.Span);
        builder.AddMcid(2);
        builder.End();
        builder.AddMcid(3);
        builder.End();

        var (writer, _) = WriteWithFake(builder);

        var p = writer.Objects.First(o =>
            o.Dict["/Type"] == "/StructElem" && o.Dict["/S"] == "/P");
        var span = writer.Objects.First(o =>
            o.Dict["/Type"] == "/StructElem" && o.Dict["/S"] == "/Span");

        // P's /K = [ Span_ref, 0, 1, 3 ] - struct refs first, then MCIDs.
        Assert.Equal($"[ {span.Id} 0 R 0 1 3 ]", p.Dict["/K"]);
        // Span has only MCID 2.
        Assert.Equal("[ 2 ]", span.Dict["/K"]);
    }

    [Fact]
    public void OptionalMetadataAppearsAsEscapedStringLiterals()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.Figure);
        builder.SetAlt("Quarterly revenue (Q4)");
        builder.SetActualText("£12.3M");
        builder.SetLang("en-GB");
        builder.SetTitle("Q4 chart");
        builder.End();

        var (writer, _) = WriteWithFake(builder);

        var fig = writer.Objects.First(o =>
            o.Dict["/Type"] == "/StructElem" && o.Dict["/S"] == "/Figure");

        // Each piece of metadata is wrapped in PDF string-literal parens.
        Assert.StartsWith("(", fig.Dict["/Alt"]);
        Assert.EndsWith(")", fig.Dict["/Alt"]);
        Assert.Contains("Quarterly revenue", fig.Dict["/Alt"]);
        Assert.Equal("(en-GB)", fig.Dict["/Lang"]);
        Assert.Equal("(Q4 chart)", fig.Dict["/T"]);
    }

    [Fact]
    public void EmissionIsPostOrderSoChildrenAppearBeforeParents()
    {
        var builder = new StructureTreeBuilder();
        builder.Begin(SemanticRole.Sect);
        builder.Begin(SemanticRole.H1);
        builder.End();
        builder.End();

        var (writer, structTreeRootId) = WriteWithFake(builder);

        // Sequence of /Type or /S markers in emission order.
        var sequence = new List<string>();
        foreach (var o in writer.Objects)
        {
            if (o.Dict["/Type"] == "/StructElem")
                sequence.Add(o.Dict["/S"]);
            else
                sequence.Add(o.Dict["/Type"]);
        }

        // H1 emitted before Sect; Sect before Document; StructTreeRoot last.
        Assert.Equal(new[] { "/H1", "/Sect", "/Document", "/StructTreeRoot" }, sequence);
    }

    // ── Fake writer helpers ─────────────────────────────────────────

    private static (FakePdfWriter writer, int structTreeRootId) WriteWithFake(StructureTreeBuilder builder)
    {
        var writer = new FakePdfWriter();
        var sut = new StructureTreeWriter(writer);
        int rootId = sut.Write(builder);
        return (writer, rootId);
    }

    private sealed record EmittedObject(int Id, IDictionary<string, string> Dict);

    private sealed class FakePdfWriter : IPdfWriter
    {
        private int _nextId = 100;
        public List<EmittedObject> Objects { get; } = new();

        public PdfEncryptor Encryptor { get; set; }

        public int GetNewId() => _nextId++;

        public void WriteObject(int id, IDictionary<string, string> dict, string streamContent = null)
        {
            // Snapshot the dict - the production writer reads it once and
            // emits, but the tests want to assert against the recorded copy.
            Objects.Add(new EmittedObject(id, new Dictionary<string, string>(dict)));
        }

        public void WriteRawObject(int id, string body) { }

        public void WriteCompressedObject(int id, IDictionary<string, string> dict, string content) =>
            WriteObject(id, dict, content);

        public void WriteBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) =>
            WriteObject(id, dict);

        public void WriteCompressedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) =>
            WriteObject(id, dict);

        public int WriteLinkAnnotation(int id, BonniePDF.Core.Hyperlink hyperlink, IList<int> pageIds = null, int? structParentKey = null) => id;
        public int WriteViewerPreferences(int id, Configuration.DocumentSettings settings) => id;
        public void WriteCatalog(int id, int pagesId, int firstPageId, Configuration.DocumentSettings settings, CatalogOptions options) { }
        public void WriteUnencryptedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent) { }
        public void Close(int rootId, int infoId = 0) { }
        public int WriteEncryptDictionary(int id) => id;
    }
}
