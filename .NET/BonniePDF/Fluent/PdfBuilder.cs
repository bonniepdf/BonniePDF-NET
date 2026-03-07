using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Fluent.Descriptors;

namespace BonniePDF.Fluent;

/// <summary>
/// Builds a PDF document from fluent API descriptors.
/// Returned by <see cref="Pdf.Create"/> and provides methods to save the document.
/// </summary>
public sealed class PdfBuilder
{
    private readonly List<PageDescriptor> _pageDescriptors = [];
    private Action<DocumentMetadata>? _metadataAction;
    private Action<DocumentSettings>? _settingsAction;

    /// <summary>
    /// Adds a page to the document.
    /// </summary>
    public void Page(Action<PageDescriptor> handler)
    {
        var descriptor = new PageDescriptor();
        handler(descriptor);
        _pageDescriptors.Add(descriptor);
    }

    /// <summary>
    /// Configures document metadata (title, author, etc.).
    /// </summary>
    public PdfBuilder WithMetadata(Action<DocumentMetadata> configure)
    {
        _metadataAction = configure;
        return this;
    }

    /// <summary>
    /// Configures document settings (encryption, PDF/A, zoom, etc.).
    /// </summary>
    public PdfBuilder WithSettings(Action<DocumentSettings> configure)
    {
        _settingsAction = configure;
        return this;
    }

    /// <summary>
    /// Saves the document to the specified file path.
    /// </summary>
    public void Save(string path)
    {
        using var doc = BuildDocument();
        doc.Save(path);
    }

    /// <summary>
    /// Saves the document to a stream.
    /// </summary>
    public void Save(Stream stream, bool leaveOpen = false)
    {
        using var doc = BuildDocument();
        doc.Save(stream, leaveOpen);
    }

    /// <summary>
    /// Asynchronously saves the document to the specified file path.
    /// </summary>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        using var doc = BuildDocument();
        await doc.SaveAsync(path, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the document to a stream.
    /// </summary>
    public async Task SaveAsync(Stream stream, bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var doc = BuildDocument();
        await doc.SaveAsync(stream, leaveOpen, cancellationToken);
    }

    /// <summary>
    /// Generates the PDF as a byte array.
    /// </summary>
    public byte[] GenerateBytes()
    {
        using var doc = BuildDocument();
        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds the Document from all configured page descriptors.
    /// </summary>
    internal Document BuildDocument()
    {
        if (_pageDescriptors.Count == 0)
            throw new InvalidOperationException("At least one page must be defined. Call .Page() in the document builder.");

        // Build settings from the first page descriptor (or defaults)
        var firstPage = _pageDescriptors[0];
        var settings = new DocumentSettings
        {
            PageSize = firstPage.PageSize,
            Orientation = firstPage.PageOrientation,
            Margin = firstPage.PageMargin
        };

        // Apply settings action
        _settingsAction?.Invoke(settings);

        // Build header/footer templates from the first page descriptor
        if (firstPage.HeaderSlot.Child != null)
        {
            IElement headerElement = firstPage.HeaderSlot.Child;
            settings.HeaderTemplate = _ => headerElement;
        }

        if (firstPage.FooterSlot.Child != null)
        {
            IElement footerElement = firstPage.FooterSlot.Child;
            settings.FooterTemplate = _ => footerElement;
        }

        var doc = new Document(settings);

        // Apply metadata
        _metadataAction?.Invoke(doc.Metadata);

        // Build content for the first page
        if (firstPage.ContentSlot.Child != null)
            doc.Add(firstPage.ContentSlot.Child);

        // Build additional pages
        for (int i = 1; i < _pageDescriptors.Count; i++)
        {
            var pageDesc = _pageDescriptors[i];
            var orientedSize = pageDesc.PageSize.GetOrientedSize(pageDesc.PageOrientation);
            doc.AddPage(orientedSize, pageDesc.PageMargin);

            if (pageDesc.ContentSlot.Child != null)
                doc.Add(pageDesc.ContentSlot.Child);
        }

        return doc;
    }
}
