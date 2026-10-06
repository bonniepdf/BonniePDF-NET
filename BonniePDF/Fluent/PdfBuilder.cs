using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BonniePDF.Fluent;

/// <summary>
/// Builds a PDF document from fluent API descriptors.
/// Returned by <see cref="Pdf.Create"/> and provides methods to save the document.
/// </summary>
public sealed class PdfBuilder
{
    private readonly List<PageDescriptor> _pageDescriptors = [];
    private readonly List<FileAttachment> _attachments = [];
    private Action<DocumentMetadata>? _metadataAction;
    private Action<DocumentSettings>? _settingsAction;
    private TextStyle? _documentDefaultStyle;

    /// <summary>
    /// Sets a document-wide default text style. All text elements will inherit these
    /// defaults unless overridden by page-level <see cref="PageDescriptor.DefaultTextStyle"/>
    /// or element-level fluent calls.
    /// </summary>
    public PdfBuilder WithDefaultTextStyle(Action<TextStyle> configure)
    {
        _documentDefaultStyle = new TextStyle();
        configure(_documentDefaultStyle);
        return this;
    }

    /// <summary>
    /// Adds a page to the document. If a default text style is configured
    /// (via <see cref="WithDefaultTextStyle"/> or <see cref="PageDescriptor.DefaultTextStyle"/>),
    /// it is applied to all text elements created within the handler.
    /// </summary>
    public void Page(Action<PageDescriptor> handler)
    {
        var descriptor = new PageDescriptor();

        // Set document-level default before running the handler,
        // so text elements created inside inherit the style.
        // PageDescriptor.DefaultTextStyle() can override this.
        TextElementBase.AmbientDefaultStyle.Value = _documentDefaultStyle;

        try
        {
            handler(descriptor);
        }
        finally
        {
            TextElementBase.AmbientDefaultStyle.Value = null;
        }

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
    /// Embeds a file into the document as an associated file (PDF/A-3 /
    /// ZUGFeRD). See <see cref="Document.Attachments"/> for the conformance
    /// rules that govern when the file is actually written.
    /// </summary>
    public PdfBuilder Attach(FileAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        _attachments.Add(attachment);
        return this;
    }

    /// <summary>
    /// Embeds a file into the document from a name and bytes.
    /// </summary>
    public PdfBuilder Attach(string fileName, byte[] content,
        AttachmentRelationship relationship = AttachmentRelationship.Unspecified,
        string? mediaType = null)
        => Attach(new FileAttachment(fileName, content, relationship, mediaType));

    /// <summary>
    /// Saves the document to the specified file path.
    /// </summary>
    public void Save(string path)
    {
        using var doc = Build();
        doc.Save(path);
    }

    /// <summary>
    /// Saves the document to a stream.
    /// </summary>
    public void Save(Stream stream, bool leaveOpen = false)
    {
        using var doc = Build();
        doc.Save(stream, leaveOpen);
    }

    /// <summary>
    /// Asynchronously saves the document to the specified file path.
    /// </summary>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        using var doc = Build();
        await doc.SaveAsync(path, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Asynchronously saves the document to a stream.
    /// </summary>
    public async Task SaveAsync(Stream stream, bool leaveOpen = false,
        CancellationToken cancellationToken = default)
    {
        using var doc = Build();
        await doc.SaveAsync(stream, leaveOpen, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates the PDF as a byte array.
    /// </summary>
    public byte[] GenerateBytes()
    {
        using var doc = Build();
        using var ms = new MemoryStream();
        doc.Save(ms, leaveOpen: true);
        return ms.ToArray();
    }

    /// <summary>
    /// Builds the Document from all configured page descriptors.
    /// </summary>
    public Document Build()
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

        // full-page background / foreground slots.
        if (firstPage.BackgroundSlot.Child != null)
        {
            IElement backgroundElement = firstPage.BackgroundSlot.Child;
            settings.PageBackground = _ => backgroundElement;
        }

        if (firstPage.ForegroundSlot.Child != null)
        {
            IElement foregroundElement = firstPage.ForegroundSlot.Child;
            settings.PageForeground = _ => foregroundElement;
        }

        var doc = new Document(settings);

        // Apply metadata
        _metadataAction?.Invoke(doc.Metadata);

        // Apply attachments
        foreach (FileAttachment attachment in _attachments)
            doc.Attach(attachment);

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
