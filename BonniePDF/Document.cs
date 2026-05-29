using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Writers;

namespace BonniePDF;

/// <summary>
/// High-level entry point for creating PDF documents.
/// Provides a simplified API for adding elements, setting metadata, and saving PDFs.
/// </summary>
public class Document : IDisposable
{
    private readonly List<Page> _pages = [];
    private readonly DiagnosticsCollector _diagnostics = new();
    private bool _disposed;

    /// <summary>
    /// Recoverable issues collected during the most recent Save / SaveAsync /
    /// ToByteArray call — typically font load failures or subset fallbacks.
    /// Empty before generation. Call <see cref="PrintDiagnostics"/> to surface
    /// them via the console, or inspect directly to feed into custom UI /
    /// telemetry.
    /// </summary>
    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.All;

    /// <summary>
    /// Convenience: writes each diagnostic to <see cref="Console.Error"/> /
    /// <see cref="Console.Out"/> according to its severity. Library code never
    /// calls this implicitly — callers opt in.
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
    public Document(DocumentSettings settings, DocumentMetadata metadata)
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
    /// Gets the pages in this document
    /// </summary>
    public IReadOnlyList<Page> Pages => _pages.AsReadOnly();

    /// <summary>
    /// Disposes of the document and releases any resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Adds an element to the current page
    /// </summary>
    public void Add(IElement element)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(element);

        if (_pages.Count == 0)
        {
            AddPage();
        }

        _pages[_pages.Count - 1].Add(element);
    }

    /// <summary>
    /// Adds multiple elements to the current page
    /// </summary>
    public void AddRange(IEnumerable<IElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        foreach (IElement element in elements) Add(element);
    }

    /// <summary>
    /// Adds a new page to the document
    /// </summary>
    public Page AddPage()
    {
        PdfSize orientedPageSize = Settings.GetOrientedPageSize();
        Page page = new(orientedPageSize, Settings.Margin);
        _pages.Add(page);
        return page;
    }

    /// <summary>
    /// Adds a new page with the specified page size
    /// </summary>
    public Page AddPage(PdfSize pageSize)
    {
        Page page = new(pageSize, Settings.Margin);
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
        Page page = new(orientedSize, Settings.Margin);
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

    public byte[] ToByteArray()
    {
        MemoryStream ms = new();
        this.Save(ms, leaveOpen: false);
        return ms.ToArray();
    }

    public async Task<byte[]> ToByteArrayAsync()
    {
        MemoryStream ms = new();
        await using (ms.ConfigureAwait(false))
        {
            await this.SaveAsync(ms, leaveOpen: false).ConfigureAwait(false);
            return ms.ToArray();
        }
    }

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a file
    /// </summary>
    private void SaveInternal(string path)
    {
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        using PdfWriter writer = new(path, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        serializer.Serialize(writer);
    }

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a stream
    /// </summary>
    private void SaveInternal(Stream stream, bool leaveOpen)
    {
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        using PdfWriter writer = new(stream, leaveOpen, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        serializer.Serialize(writer);
    }


    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a file
    /// </summary>
    private async Task SaveInternalAsync(string path, CancellationToken cancellationToken)
    {
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        await using AsyncPdfWriter writer = new(path, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a stream
    /// </summary>
    private async Task SaveInternalAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken)
    {
        _diagnostics.Clear();
        ValidateSettings();
        ApplyDefaultStyleToTree();
        await using AsyncPdfWriter writer = new(stream, leaveOpen, Settings.Version);
        var serializer = new PdfDocumentSerializer(this, _diagnostics);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Walks the element tree and applies DefaultTextStyle to elements that haven't
    /// had their style properties explicitly set by user code.
    /// </summary>
    private void ApplyDefaultStyleToTree()
    {
        TextStyle style = Settings.DefaultTextStyle;
        if (style == null) return;
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
            foreach (TableRow row in t.GetAllRows())
                foreach (TableCell cell in row.Cells)
                    if (cell.Content != null)
                        ApplyStyleToElement(cell.Content, style);
    }



    // ---- Validation ----

    private void ValidateSettings()
    {
        // AES-256 requires PDF 1.7
        if (Settings.IsEncrypted &&
            Settings.Encryption.Algorithm == Security.Models.EncryptionAlgorithm.AES_256 &&
            Settings.Version != PdfVersion.Pdf17)
        {
            throw new InvalidOperationException(
                "AES-256 encryption requires PdfVersion.Pdf17. Set Settings.Version = PdfVersion.Pdf17.");
        }

        // PDF/A requires PDF 1.7. Silently upgrade rather than throw —
        // the caller asked for archival output, so honour that intent.
        if (Settings.Conformance != PdfAConformance.None && Settings.Version != PdfVersion.Pdf17)
        {
            Settings.Version = PdfVersion.Pdf17;
        }

        // PDF/A cannot be combined with encryption
        if (Settings.Conformance != PdfAConformance.None && Settings.IsEncrypted)
        {
            throw new InvalidOperationException(
                "PDF/A mode cannot be combined with encryption. Disable encryption or PDF/A.");
        }

        // "a" levels require tagged output (PDF/UA-1).
        if (RequiresTagging(Settings.Conformance) && Settings.Accessibility != AccessibilityMode.PdfUa1)
        {
            throw new InvalidOperationException(
                $"{Settings.Conformance} requires tagged output. Set Settings.Accessibility = AccessibilityMode.PdfUa1.");
        }

        // Bookmarks require PDF 1.7
        if (Settings.HasBookmarks && Settings.Version != PdfVersion.Pdf17)
        {
            throw new InvalidOperationException(
                "Bookmarks require PdfVersion.Pdf17. Set Settings.Version = PdfVersion.Pdf17.");
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
