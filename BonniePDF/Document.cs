using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Writers;

namespace BonniePDF;

/// <summary>
/// High-level entry point for creating PDF documents.
/// Provides a simplified API for adding elements, setting metadata, and saving PDFs.
/// </summary>
public class Document : IDisposable
{
    private readonly List<Page> _pages = [];
    private readonly List<FileAttachment> _attachments = [];
    private readonly DiagnosticsCollector _diagnostics = new();
    private bool _disposed;

    /// <summary>
    /// Recoverable issues collected during the most recent Save / SaveAsync /
    /// GenerateBytes call - typically font load failures or subset fallbacks.
    /// Empty before generation. Call <see cref="PrintDiagnostics"/> to surface
    /// them via the console, or inspect directly to feed into custom UI /
    /// telemetry.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.All;

    /// <summary>
    /// Convenience: writes each diagnostic to <see cref="Console.Error"/> /
    /// <see cref="Console.Out"/> according to its severity. Library code never
    /// calls this implicitly - callers opt in.
    /// </summary>
    public void PrintDiagnostics() => _diagnostics.Print();

    /// <summary>
    /// Creates a new instance of Document with default settings
    /// </summary>
    public Document() : this(null, null)
    {
    }

    /// <summary>
    /// Creates a new instance of Document with the specified settings
    /// </summary>
    public Document(DocumentSettings settings) : this(settings, null)
    {
    }

    /// <summary>
    /// Creates a new instance of Document with the specified metadata
    /// </summary>
    public Document(DocumentMetadata metadata) : this(null, metadata)
    {
    }

    /// <summary>
    /// Creates a new instance of Document with the specified settings and metadata
    /// </summary>
    public Document(DocumentSettings? settings, DocumentMetadata? metadata)
    {
        Settings = settings ?? new DocumentSettings();
        Metadata = metadata ?? new DocumentMetadata();
        // Create the first page AFTER settings are applied
        AddPage();
    }

    /// <summary>
    /// Gets or sets the document metadata (title, author, etc.)
    /// </summary>
    public DocumentMetadata Metadata { get; set; }

    /// <summary>
    /// Gets or sets the document settings (page size, margins, zoom, etc.)
    /// </summary>
    public DocumentSettings Settings { get; set; }

    /// <summary>
    /// The fonts that belong to this document: any number of families and faces. Nothing added
    /// here is visible to another document, and the global font registries are unchanged. A
    /// family named here is resolved before the global and standard ones.
    /// </summary>
    /// <example>
    /// <code>
    /// doc.Fonts.Add(regularBytes, family: "Inter");
    /// doc.Fonts.Add(boldBytes, family: "Inter", weight: FontWeight.Bold);
    /// doc.Add(new TextBlock { Text = "Hello", FontFamily = "Inter", FontWeight = FontWeight.Bold });
    /// </code>
    /// </example>
    public DocumentFonts Fonts { get; } = new();

    /// <summary>
    /// Builds a document from a Document Definition given as JSON text. The definition is validated
    /// completely first; if it has any problem this throws a <see cref="DefinitionException"/>
    /// that lists them all, and nothing is constructed.
    /// </summary>
    /// <remarks>
    /// Resources with a <c>ref</c> are supplied by the host through
    /// <see cref="DefinitionReadOptions.Resources"/>; the reader never opens a path or fetches a
    /// URL itself.
    /// </remarks>
    public static async Task<Document> FromDefinitionAsync(string json, DefinitionReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new DefinitionReadOptions();
        DefinitionValidationResult result = DocumentDefinition.Validate(json, options);
        if (!result.IsValid) throw new DefinitionException(result.Errors);
        return await DefinitionReader.ReadAsync(result.Definition!.Value, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Builds a document from a Document Definition that has already been parsed.</summary>
    public static async Task<Document> FromDefinitionAsync(System.Text.Json.JsonElement definition, DefinitionReadOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new DefinitionReadOptions();
        DefinitionValidationResult result = DocumentDefinition.Validate(definition, options);
        if (!result.IsValid) throw new DefinitionException(result.Errors);
        return await DefinitionReader.ReadAsync(result.Definition!.Value, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a document from a Document Definition given as JSON text, waiting for any resources the host
    /// supplies. See <see cref="FromDefinitionAsync(string, DefinitionReadOptions?, CancellationToken)"/>.
    /// </summary>
    public static Document FromDefinition(string json, DefinitionReadOptions? options = null) =>
        FromDefinitionAsync(json, options).GetAwaiter().GetResult();

    /// <summary>
    /// Writes this document as a Document Definition: JSON data, with a note for anything the writer had to say.
    /// Reading it back with <see cref="FromDefinitionAsync(string, DefinitionReadOptions?, CancellationToken)"/>
    /// gives a document that renders identically.
    /// </summary>
    /// <remarks>
    /// A construct a definition cannot express (a function-valued page template, a <c>ShowIf</c> predicate, a custom
    /// element, an <c>Svg</c> generator) makes this throw a <see cref="DefinitionException"/> that lists
    /// every one; pass <see cref="DefinitionUnrepresentable.Drop"/> to leave them out and receive a
    /// <c>dropped</c> note for each instead. Encryption is written without its passwords, which the host supplies
    /// again on reading.
    /// </remarks>
    /// <param name="options">What to do with what cannot be written, and how to write resources.</param>
    /// <returns>The definition and its notes.</returns>
    public DefinitionWriteResult ToDefinition(DefinitionWriteOptions? options = null) =>
        DefinitionWriter.Write(this, options);

    /// <summary>
    /// Gets the pages in this document
    /// </summary>
    public IReadOnlyList<Page> Pages => _pages.AsReadOnly();

    /// <summary>
    /// Files embedded into the generated PDF as associated files (PDF/A-3 /
    /// ZUGFeRD). Add to this collection directly, or use the
    /// <see cref="Attach(FileAttachment)"/> / <see cref="Attach(string, byte[], AttachmentRelationship, string)"/>
    /// helpers. Embedded files are honoured under <c>None</c>, PDF/A-3, and
    /// PDF/A-4; under levels that forbid them (PDF/A-1, PDF/A-2) they are
    /// dropped at save time with a diagnostic, keeping the conformance claim
    /// honest.
    /// </summary>
    /// <example>
    /// <code>
    /// doc.Attachments.Add(new FileAttachment("data.csv", bytes, AttachmentRelationship.Data, "text/csv"));
    /// Console.WriteLine(doc.Attachments.Count); // 1
    /// </code>
    /// </example>
    public IList<FileAttachment> Attachments => _attachments;

    /// <summary>
    /// Embeds a file into the document. Returns this document for chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// doc.Attach(new FileAttachment
    /// {
    ///     FileName = "invoice.xml",
    ///     Content = xmlBytes,
    ///     Relationship = AttachmentRelationship.Alternative,
    ///     MediaType = "text/xml",
    /// });
    /// </code>
    /// </example>
    public Document Attach(FileAttachment attachment)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(attachment);
        _attachments.Add(attachment);
        return this;
    }

    /// <summary>
    /// Embeds a file into the document from a name and bytes. Returns this
    /// document for chaining.
    /// </summary>
    /// <example>
    /// <code>
    /// doc.Attach("data.csv", bytes, AttachmentRelationship.Data, "text/csv");
    /// </code>
    /// </example>
    public Document Attach(string fileName, byte[] content,
        AttachmentRelationship relationship = AttachmentRelationship.Unspecified,
        string? mediaType = null)
        => Attach(new FileAttachment(fileName, content, relationship, mediaType));

    /// <summary>
    /// Disposes of the document and releases any resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Adds one paragraph from lines joined by hard line breaks.</summary>
    public void Add(params string[] lines) => Add(new TextBlock(lines));

    /// <summary>Adds one paragraph from lines joined by hard line breaks.</summary>
    public void Add(IEnumerable<string> lines) => Add(new TextBlock(lines));

    /// <summary>
    /// Adds an element to the current page
    /// </summary>
    /// <example>
    /// <code>
    /// var doc = new Document();
    /// doc.Add(new TextBlock("Quarterly report") { FontSize = 20 });
    /// doc.Add(new TextBlock("Prepared by Operations"));
    /// </code>
    /// </example>
    public void Add(IElement element)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(element);

        if (_pages.Count == 0)
        {
            AddPage();
        }

        _pages[^1].Add(element);
    }

    /// <summary>
    /// Adds multiple elements to the current page
    /// </summary>
    /// <example>
    /// <code>
    /// doc.AddRange([new TextBlock("One"), new TextBlock("Two"), new TextBlock("Three")]);
    /// </code>
    /// </example>
    public void AddRange(IEnumerable<IElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        foreach (IElement element in elements) Add(element);
    }

    /// <summary>
    /// Adds a new page to the document
    /// </summary>
    /// <example>
    /// <code>
    /// var doc = new Document();
    /// doc.Add(new TextBlock("First page"));
    /// doc.AddPage(); // start a new page
    /// doc.Add(new TextBlock("Second page"));
    /// </code>
    /// </example>
    public Page AddPage()
    {
        PdfSize orientedPageSize = Settings.GetOrientedPageSize();
        Page page = new(orientedPageSize, Settings.Margin, inheritMargin: true);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a new page with the specified page size
    /// </summary>
    public Page AddPage(PdfSize pageSize)
    {
        Page page = new(pageSize, Settings.Margin, inheritMargin: true);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a new page with the specified page size and margin
    /// </summary>
    public Page AddPage(PdfSize pageSize, float margin)
    {
        Page page = new(pageSize, margin);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a new page with the specified page size and orientation
    /// </summary>
    public Page AddPage(PdfSize pageSize, PageOrientation orientation)
    {
        PdfSize orientedSize = pageSize.GetOrientedSize(orientation);
        Page page = new(orientedSize, Settings.Margin, inheritMargin: true);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a new page with the specified page size, orientation, and margin
    /// </summary>
    public Page AddPage(PdfSize pageSize, PageOrientation orientation, float margin)
    {
        PdfSize orientedSize = pageSize.GetOrientedSize(orientation);
        Page page = new(orientedSize, margin);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Updates the settings of the current (last added) page.
    /// Useful for changing the first page's settings after initialization.
    /// </summary>
    public void SetCurrentPageSettings(PdfSize pageSize, PageOrientation orientation, float margin)
    {
        if (_pages.Count == 0)
            return;

        // Create a new page with the updated settings
        PdfSize orientedSize = pageSize.GetOrientedSize(orientation);
        Page newPage = new(orientedSize, margin);

        // Copy existing elements to the new page
        Page oldPage = _pages[_pages.Count - 1];
        foreach (IElement element in oldPage.Elements)
        {
            newPage.Add(element);
        }

        // Replace the old page
        _pages[_pages.Count - 1] = newPage;
    }

    /// <summary>
    /// Saves the document to the specified file path
    /// </summary>
    public void Save(string path)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("File path cannot be null or empty", nameof(path));

        SaveInternal(path);
    }

    /// <summary>
    /// Saves the document to a stream
    /// </summary>
    /// <param name="stream">The stream to write the PDF to</param>
    public void Save(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        SaveInternal(stream, false);
    }

    /// <summary>
    /// Saves the document to a stream
    /// </summary>
    /// <param name="stream">The stream to write the PDF to</param>
    /// <param name="leaveOpen">If true, the stream will not be disposed after saving</param>
    public void Save(Stream stream, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(stream);

        SaveInternal(stream, leaveOpen);
    }

    /// <summary>
    /// Asynchronously saves the document to the specified file path
    /// </summary>
    /// <param name="path">The path to save the PDF to</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <example>
    /// <code>
    /// await doc.SaveAsync("report.pdf");
    /// </code>
    /// </example>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(path))
            throw new ArgumentException("File path cannot be null or empty", nameof(path));

        await SaveInternalAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves the document to a stream
    /// </summary>
    /// <param name="stream">The stream to write the PDF to</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task SaveAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await SaveInternalAsync(stream, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves the document to a stream
    /// </summary>
    /// <param name="stream">The stream to write the PDF to</param>
    /// <param name="leaveOpen">If true, the stream will not be disposed after saving</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task SaveAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await SaveInternalAsync(stream, leaveOpen, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes the document to PDF bytes.
    /// </summary>
    /// <example>
    /// <code>
    /// byte[] bytes = doc.GenerateBytes();
    /// </code>
    /// </example>
    public byte[] GenerateBytes()
    {
        MemoryStream ms = new();
        Save(ms, leaveOpen: false);
        return ms.ToArray();
    }

    /// <summary>
    /// Asynchronously serializes the document to PDF bytes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <example>
    /// <code>
    /// byte[] bytes = await doc.GenerateBytesAsync();
    ///
    /// // Cancel with a CancellationToken:
    /// using var cts = new CancellationTokenSource();
    /// Task&lt;byte[]&gt; pending = doc.GenerateBytesAsync(cts.Token);
    /// cts.Cancel();
    /// </code>
    /// </example>
    public async Task<byte[]> GenerateBytesAsync(CancellationToken cancellationToken = default)
    {
        MemoryStream ms = new();
        await using (ms.ConfigureAwait(false))
        {
            await SaveAsync(ms, leaveOpen: false, cancellationToken).ConfigureAwait(false);
            return ms.ToArray();
        }
    }

    // ─── SVG page export ────────────────────────────────────────────────

    /// <summary>
    /// Renders every laid-out page of the document to a standalone SVG string.
    ///
    /// Runs the same layout, pagination, watermark, and header/footer pipeline as
    /// PDF output, so the returned list has one entry per generated PDF page and
    /// the visual layout matches.
    /// </summary>
    /// <param name="options">Export options; defaults are used when <c>null</c>.</param>
    public IReadOnlyList<string> GenerateSvgPages(SvgPageExportOptions? options = null)
    {
        LicenseChecker.ValidateLicense();
        ApplyDefaultStyleToTree();
        PrepareSvgElements();
        return SvgPageExporter.Export(this, options);
    }

    /// <summary>
    /// Like <see cref="GenerateSvgPages"/>, but also returns <c>@font-face</c> CSS
    /// embedding a minimal subset of the fonts the document uses. Inject the CSS
    /// once into the host page so the SVG renders with the exact fonts used for
    /// layout, independent of what the viewer has installed.
    /// </summary>
    public SvgDocumentExport GenerateSvgDocument(SvgPageExportOptions? options = null)
    {
        LicenseChecker.ValidateLicense();
        ApplyDefaultStyleToTree();
        PrepareSvgElements();
        return SvgPageExporter.ExportDocument(this, options);
    }

    /// <summary>Asynchronous sibling of <see cref="GenerateSvgDocument"/>.</summary>
    public async Task<SvgDocumentExport> GenerateSvgDocumentAsync(SvgPageExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PrepareSvgElementsAsync(cancellationToken).ConfigureAwait(false);
        return GenerateSvgDocument(options);
    }

    /// <summary>
    /// Renders a single page (zero-based index) to a standalone SVG string.
    /// </summary>
    public string GenerateSvgPage(int pageIndex, SvgPageExportOptions? options = null)
    {
        IReadOnlyList<string> pages = GenerateSvgPages(options);
        if (pageIndex < 0 || pageIndex >= pages.Count)
            throw new ArgumentOutOfRangeException(nameof(pageIndex),
                $"Page index {pageIndex} out of range (document has {pages.Count} page(s)).");
        return pages[pageIndex];
    }

    /// <summary>
    /// Convenience for example cards and social previews: renders the first page
    /// to a standalone SVG string.
    /// </summary>
    public string GenerateFirstPageSvg(SvgPageExportOptions? options = null) => GenerateSvgPage(0, options);

    /// <summary>
    /// Asynchronous sibling of <see cref="GenerateSvgPages"/>. SVG export is
    /// CPU-bound and runs synchronously, after calling any registered
    /// <see cref="DocumentSettings.SvgRasterizer"/> the document's SVG elements need.
    /// </summary>
    /// <example>
    /// <code>
    /// IReadOnlyList&lt;string&gt; pages = await doc.GenerateSvgPagesAsync(new SvgPageExportOptions { MaxPages = 1 });
    /// </code>
    /// </example>
    public async Task<IReadOnlyList<string>> GenerateSvgPagesAsync(SvgPageExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PrepareSvgElementsAsync(cancellationToken).ConfigureAwait(false);
        return GenerateSvgPages(options);
    }

    /// <summary>Asynchronous sibling of <see cref="GenerateSvgPage"/>.</summary>
    public async Task<string> GenerateSvgPageAsync(int pageIndex, SvgPageExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await PrepareSvgElementsAsync(cancellationToken).ConfigureAwait(false);
        return GenerateSvgPage(pageIndex, options);
    }

    /// <summary>Asynchronous sibling of <see cref="GenerateFirstPageSvg"/>.</summary>
    public Task<string> GenerateFirstPageSvgAsync(SvgPageExportOptions? options = null,
        CancellationToken cancellationToken = default) => GenerateSvgPageAsync(0, options, cancellationToken);

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a file
    /// </summary>
    private void SaveInternal(string path)
    {
        LicenseChecker.ValidateLicense();
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        PrepareSvgElements();
        using PdfWriter writer = new(path, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        serializer.Serialize(writer);
    }

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a stream
    /// </summary>
    private void SaveInternal(Stream stream, bool leaveOpen)
    {
        LicenseChecker.ValidateLicense();
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        PrepareSvgElements();
        using PdfWriter writer = new(stream, leaveOpen, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        serializer.Serialize(writer);
    }


    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a file
    /// </summary>
    private async Task SaveInternalAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LicenseChecker.ValidateLicense();
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        await PrepareSvgElementsAsync(cancellationToken).ConfigureAwait(false);
        AsyncPdfWriter writer = new(path, Settings.Version);
        await using var _ = writer.ConfigureAwait(false);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a stream
    /// </summary>
    private async Task SaveInternalAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LicenseChecker.ValidateLicense();
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        await PrepareSvgElementsAsync(cancellationToken).ConfigureAwait(false);
        AsyncPdfWriter writer = new(stream, leaveOpen, Settings.Version);
        await using var _ = writer.ConfigureAwait(false);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Parses every <see cref="Svg"/> element before layout. One the registered
    /// rasterizer would draw differently fails here, since only the async path can
    /// call it.
    /// </summary>
    private void PrepareSvgElements()
    {
        bool rasterizerRegistered = Settings.SvgRasterizer is not null;
        foreach (Svg svg in CollectSvgElements())
            svg.Prepare(rasterizerRegistered);
    }

    /// <summary>
    /// Parses every <see cref="Svg"/> element before layout, rasterising what needs it.
    /// Sequentially and in document order, so a rasterizer sees a deterministic
    /// sequence of calls.
    /// </summary>
    private async Task PrepareSvgElementsAsync(CancellationToken cancellationToken)
    {
        foreach (Svg svg in CollectSvgElements())
            await svg.PrepareAsync(Settings.SvgRasterizer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Every <see cref="Svg"/> element, once each, in document order.</summary>
    private List<Svg> CollectSvgElements()
    {
        var found = new List<Svg>();
        var seen = new HashSet<IElement>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        void Walk(IElement? element)
        {
            if (element is null) return;
            IEnumerable<IElement> children = element.GetChildren();
            // A leaf other than an Svg finds nothing however often it is walked, so only Svg
            // elements and containers need a visited entry; most of a document is leaves.
            bool leaf = element is not Svg && children is ICollection<IElement> { Count: 0 };
            if (!leaf && !seen.Add(element)) return;
            if (element is Svg svg) found.Add(svg);
            foreach (IElement child in children) Walk(child);
        }

        foreach (Page page in _pages)
            foreach (IElement element in page.Elements)
                Walk(element);

        // Calling each template with both page parities discovers alternating content.
        foreach (int pageNumber in (ReadOnlySpan<int>)[1, 2])
        {
            Walk(Settings.HeaderTemplate?.Invoke(pageNumber));
            Walk(Settings.FooterTemplate?.Invoke(pageNumber));
        }

        return found;
    }

    /// <summary>
    /// Walks the element tree and applies DefaultTextStyle to elements that haven't
    /// had their style properties explicitly set by user code.
    /// </summary>
    private bool _defaultStyleCascaded;

    private void ApplyDefaultStyleToTree()
    {
        foreach (Page page in _pages)
            foreach (IElement element in page.Elements)
                ContentDirectionResolver.Apply(element, Settings.ContentDirection);
        TextStyle? style = Settings.DefaultTextStyle;
        if (style == null)
        {
            // A default that was removed since the last generation: return what it supplied to the library defaults.
            if (!_defaultStyleCascaded) return;
            style = new TextStyle();
            _defaultStyleCascaded = false;
        }
        else
        {
            _defaultStyleCascaded = true;
        }
        foreach (Page page in _pages)
            foreach (IElement el in page.Elements)
                ApplyStyleToElement(el, style);
    }

    private static void ApplyStyleToElement(IElement el, TextStyle style)
    {
        if (el is TextElementBase text)
            text.ApplyDefaultStyleIfNotSet(style);

        if (el is VerticalStack vs)
            foreach (IElement child in vs.Children) ApplyStyleToElement(child, style);
        else if (el is HorizontalStack hs)
            foreach (IElement child in hs.Children) ApplyStyleToElement(child, style);
        else if (el is Container c && c.Child != null)
            ApplyStyleToElement(c.Child, style);
        else if (el is ListElementBase list)
            foreach (ListElementBase child in list.GetNestedLists())
                ApplyStyleToElement(child, style);
        else if (el is Table t)
        {
            // A table builds its running-total rows during layout, so it keeps the style for them.
            t.CascadedTextStyle = style;
            // Every cell's non-null content, in row order, as GetAllRows would give it.
            foreach (IElement content in t.GetChildren())
                ApplyStyleToElement(content, style);
        }
    }



    // ---- Validation ----

    private void ValidateSettings()
    {
        // PDF/A cannot be combined with encryption. Checked before the
        // version-specific encryption rule so this clearer message wins
        // for e.g. PDF/A-4 + AES-256.
        if (Settings.Conformance != PdfAConformance.None && Settings.IsEncrypted)
        {
            throw new InvalidOperationException(
                "PDF/A mode cannot be combined with encryption. Disable encryption or PDF/A.");
        }

        // AES-256 requires PDF 1.7 or later
        if (Settings.IsEncrypted &&
            Settings.Encryption!.Algorithm == EncryptionAlgorithm.AES_256 &&
            Settings.Version != PdfVersion.Pdf17 && Settings.Version != PdfVersion.Pdf20)
        {
            throw new InvalidOperationException(
                "AES-256 encryption requires PdfVersion.Pdf17 or PdfVersion.Pdf20. Set Settings.Version accordingly.");
        }

        // PDF/A version requirements. Silently upgrade rather than throw -
        // the caller asked for archival output, so honour that intent.
        // PDF/A-1/2/3 are built on PDF 1.7; PDF/A-4 is built on PDF 2.0.
        if (Settings.Conformance == PdfAConformance.PdfA4)
        {
            Settings.Version = PdfVersion.Pdf20;
        }
        else if (Settings.Conformance != PdfAConformance.None && Settings.Version != PdfVersion.Pdf17)
        {
            Settings.Version = PdfVersion.Pdf17;
        }

        // "a" levels require tagged output (PDF/UA-1).
        if (RequiresTagging(Settings.Conformance) && Settings.Accessibility != AccessibilityMode.PdfUa1)
        {
            throw new InvalidOperationException(
                $"{Settings.Conformance} requires tagged output. Set Settings.Accessibility = AccessibilityMode.PdfUa1.");
        }

        // PDF/UA-1 tagging requires PDF 1.7 too, and nothing else upgrades
        // the version for an accessibility-only request (the PDF/A branch
        // above only covers its own conformance levels). Silently upgrade
        // the default rather than silently dropping tagging - the caller
        // asked for accessible output, so honour that intent, same as PDF/A
        // above. An explicit Pdf20 request is left alone and still hits the
        // throw below.
        if (Settings.Accessibility == AccessibilityMode.PdfUa1 && Settings.Version != PdfVersion.Pdf17 &&
            Settings.Version != PdfVersion.Pdf20)
        {
            Settings.Version = PdfVersion.Pdf17;
        }

        // PDF/UA-1 is bound to PDF 1.7. PDF/UA-2 (the PDF 2.0 accessibility
        // standard) is not implemented yet, so reject PDF/UA-1 on a PDF 2.0
        // document rather than silently dropping the tagging.
        if (Settings.Accessibility == AccessibilityMode.PdfUa1 && Settings.Version == PdfVersion.Pdf20)
        {
            throw new InvalidOperationException(
                "PDF/UA-1 output is only supported with PdfVersion.Pdf17. PDF/UA-2 for PdfVersion.Pdf20 is not implemented yet.");
        }

        // Bookmarks require PDF 1.7 or later
        if (Settings.HasBookmarks && Settings.Version != PdfVersion.Pdf17 && Settings.Version != PdfVersion.Pdf20)
        {
            throw new InvalidOperationException(
                "Bookmarks require PdfVersion.Pdf17 or PdfVersion.Pdf20. Set Settings.Version accordingly.");
        }
    }

    /// <summary>
    /// True for PDF/A "a" (accessible) conformance levels, which mandate
    /// tagged structure on top of the underlying "b" requirements.
    /// </summary>
    private static bool RequiresTagging(PdfAConformance conformance) => conformance
        is PdfAConformance.PdfA1a
        or PdfAConformance.PdfA2a
        or PdfAConformance.PdfA3a;


    /// <summary>
    /// Disposes of the document
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
            // Dispose managed resources
            _pages.Clear();

        _disposed = true;
    }
}
