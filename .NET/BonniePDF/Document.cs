using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Metadata;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Rendering.Writers;
using BonniePDF.Security.Encryption;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;

namespace BonniePDF;

/// <summary>
/// High-level entry point for creating PDF documents.
/// Provides a simplified API for adding elements, setting metadata, and saving PDFs.
/// </summary>
public class Document : IDisposable
{
    private readonly List<Page> _pages = [];
    private bool _disposed;

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
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (_pages.Count == 0)
            AddPage();

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

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a file
    /// </summary>
    private void SaveInternal(string path)
    {
        ValidateSettings();
        using PdfWriter writer = new(path, Settings.Version);
        var serializer = new PdfDocumentSerializer(this);
        serializer.Serialize(writer);
    }

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a stream
    /// </summary>
    private void SaveInternal(Stream stream, bool leaveOpen)
    {
        ValidateSettings();
        using PdfWriter writer = new(stream, leaveOpen, Settings.Version);
        var serializer = new PdfDocumentSerializer(this);
        serializer.Serialize(writer);
    }


    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a file
    /// </summary>
    private async Task SaveInternalAsync(string path, CancellationToken cancellationToken)
    {
        ValidateSettings();
        await using AsyncPdfWriter writer = new(path, Settings.Version);
        var serializer = new PdfDocumentSerializer(this);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a stream
    /// </summary>
    private async Task SaveInternalAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken)
    {
        ValidateSettings();
        await using AsyncPdfWriter writer = new(stream, leaveOpen, Settings.Version);
        var serializer = new PdfDocumentSerializer(this);
        await serializer.SerializeAsync(writer, cancellationToken).ConfigureAwait(false);
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

        // PDF/A requires PDF 1.7
        if (Settings.PdfAEnabled && Settings.Version != PdfVersion.Pdf17)
        {
            throw new InvalidOperationException(
                "PDF/A-1b mode requires PdfVersion.Pdf17. Set Settings.Version = PdfVersion.Pdf17.");
        }

        // PDF/A cannot be combined with encryption
        if (Settings.PdfAEnabled && Settings.IsEncrypted)
        {
            throw new InvalidOperationException(
                "PDF/A-1b mode cannot be combined with encryption. Disable encryption or PDF/A.");
        }

        // Bookmarks require PDF 1.7
        if (Settings.HasBookmarks && Settings.Version != PdfVersion.Pdf17)
        {
            throw new InvalidOperationException(
                "Bookmarks require PdfVersion.Pdf17. Set Settings.Version = PdfVersion.Pdf17.");
        }
    }


    /// <summary>
    /// Disposes of the document
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
                // Dispose managed resources
                _pages.Clear();

            _disposed = true;
        }
    }
}