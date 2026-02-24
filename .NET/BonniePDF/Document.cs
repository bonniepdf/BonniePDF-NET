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
    // Maps RenderContext image keys to actual PDF object IDs
    private readonly Dictionary<string, int> _imagePdfObjectIds = new();
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
    public void AddPage()
    {
        PdfSize orientedPageSize = Settings.GetOrientedPageSize();
        Page page = new(orientedPageSize, Settings.Margin);
        _pages.Add(page);
    }

    /// <summary>
    /// Adds a new page with the specified page size
    /// </summary>
    public void AddPage(PdfSize pageSize)
    {
        Page page = new(pageSize, Settings.Margin);
        _pages.Add(page);
    }

    /// <summary>
    /// Adds a new page with the specified page size and margin
    /// </summary>
    public void AddPage(PdfSize pageSize, float margin)
    {
        Page page = new(pageSize, margin);
        _pages.Add(page);
    }

    /// <summary>
    /// Adds a new page with the specified page size and orientation
    /// </summary>
    public void AddPage(PdfSize pageSize, PageOrientation orientation)
    {
        PdfSize orientedSize = pageSize.GetOrientedSize(orientation);
        Page page = new(orientedSize, Settings.Margin);
        _pages.Add(page);
    }

    /// <summary>
    /// Adds a new page with the specified page size, orientation, and margin
    /// </summary>
    public void AddPage(PdfSize pageSize, PageOrientation orientation, float margin)
    {
        PdfSize orientedSize = pageSize.GetOrientedSize(orientation);
        Page page = new(orientedSize, margin);
        _pages.Add(page);
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
        SaveWithWriter(writer);
    }

    /// <summary>
    /// Internal implementation of Save that generates the PDF to a stream
    /// </summary>
    private void SaveInternal(Stream stream, bool leaveOpen)
    {
        ValidateSettings();
        using PdfWriter writer = new(stream, leaveOpen, Settings.Version);
        SaveWithWriter(writer);
    }

    /// <summary>
    /// Common save logic that works with any IPdfWriter instance
    /// Uses the shared PdfRenderPipeline to eliminate code duplication with SaveWithWriterAsync.
    /// The rendering phase (Phase 1) is performed once by the pipeline, then this method
    /// handles the synchronous I/O operations.
    /// MEMORY EFFICIENCY: This method uses streaming to process pages one at a time,
    /// keeping memory usage low for documents with 1000+ pages. Resources are moved to
    /// the parent Pages object, allowing pages to be written immediately.
    /// </summary>
    private void SaveWithWriter(IPdfWriter writer)
    {
        FontWriter fontWriter = new(writer);
        ImageWriter imageWriter = new(writer);
        imageWriter.PdfAEnabled = Settings.PdfAEnabled;

        // 0. Set up encryption if configured
        int encryptId = 0;
        if (Settings.IsEncrypted)
        {
            writer.Encryptor = new PdfEncryptor(Settings.Encryption);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = Settings.GetOrientedPageSize();
        RenderContext ctx = new(orientedPageSize, Settings.Margin);
        ctx.PdfAEnabled = Settings.PdfAEnabled;

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = new();

        // We use the new Streaming pipeline
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(_pages, Settings, ctx))
        {
            int pageId = writer.GetNewId();
            int contentId = writer.GetNewId();
            pageIds.Add(pageId);

            // A. Write Content Stream IMMEDIATELY (Flush memory)
            writer.WriteCompressedObject(contentId, new Dictionary<string, string>(), pageOutput.ContentStream);

            // B. Write Page Object IMMEDIATELY
            // NOTICE: We removed "/Resources" from here! It will go on the Parent.
            Dictionary<string, string> pageDict = new()
            {
                ["/Type"] = "/Page",
                ["/Parent"] = $"{pagesId} 0 R", // Refers to the parent we write later
                ["/MediaBox"] = $"[0 0 {pageOutput.Size.Width} {pageOutput.Size.Height}]",
                ["/Contents"] = $"{contentId} 0 R"
            };

            // Write link annotations if present
            if (pageOutput.Hyperlinks.Count > 0)
            {
                List<int> annotIds = new();
                foreach (Hyperlink link in pageOutput.Hyperlinks)
                {
                    int annotId = writer.GetNewId();
                    writer.WriteLinkAnnotation(annotId, link);
                    annotIds.Add(annotId);
                }

                pageDict["/Annots"] = "[ " + string.Join(" ", annotIds.Select(id => $"{id} 0 R")) + " ]";
            }

            writer.WriteObject(pageId, pageDict);

            // C. Clean up
            // The 'pageOutput' goes out of scope and is eligible for GC. 
            // We only hold 'pageIds' (integers) in memory.
        }

        // 4. Post-Processing (Write Resources)
        // Now that we've processed all pages, 'ctx' knows every Font/Image used.

        WriteFontResources(writer, fontWriter, ctx);
        WriteImageResources(writer, imageWriter, ctx);
        Dictionary<string, int> deferredIds = WriteDeferredXObjects(writer, ctx, pageIds.Count);

        string fontRes = BuildFontResourcesString(ctx);
        string imageRes = BuildXObjectResourcesString(ctx, deferredIds);
        string globalResources = CombineResources(fontRes, imageRes);

        // Write ExtGState for watermark opacity if configured
        if (Settings.Watermark != null)
        {
            int gsId = writer.GetNewId();
            // PDF/A-1B forbids CA/ca < 1.0 — use full opacity and blend the color instead
            float opacity = Settings.PdfAEnabled ? 1.0f : Settings.Watermark.Opacity;
            writer.WriteObject(gsId, new Dictionary<string, string>
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = opacity.ToString("F4"),
                ["/CA"] = opacity.ToString("F4")
            });
            string extGStateRes = $"<< /ExtGState << /GS_WM {gsId} 0 R >> >>";
            globalResources = CombineResources(globalResources, extGStateRes);
        }

        // 5. Write Parent Pages Object
        // We attach the Global Resources here. They inherit down to all pages.
        string kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
        writer.WriteObject(pagesId, new Dictionary<string, string>
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString(),
            ["/Resources"] = globalResources // <--- Resources moved here!
        });

        // 6. Write Metadata & Close
        writer.WriteDocumentInfo(infoId, Metadata);

        CatalogOptions catalogOptions = new();

        if (HasViewerPreferences(Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            writer.WriteViewerPreferences(catalogOptions.ViewerPrefsId, Settings);
        }

        // Write XMP Metadata for PDF 1.7
        if (Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.XmpMetadataId = writer.GetNewId();
            WriteXmpMetadata(writer, catalogOptions.XmpMetadataId);
        }

        // Write Bookmarks for PDF 1.7
        if (Settings.Version == PdfVersion.Pdf17 && Settings.HasBookmarks)
        {
            int outlinesStartId = writer.GetNewId();
            catalogOptions.OutlinesId = WriteOutlineTree(writer, outlinesStartId, Settings.Bookmarks, pageIds);
        }

        // Write PDF/A Output Intents
        if (Settings.PdfAEnabled && Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.PdfAEnabled = true;
            int iccProfileId = writer.GetNewId();
            WriteIccProfile(writer, iccProfileId);
            int outputIntentId = writer.GetNewId();
            WriteOutputIntent(writer, outputIntentId, iccProfileId);
            catalogOptions.OutputIntentIds = new List<int> { outputIntentId };
        }

        // Write Catalog
        writer.WriteCatalog(catalogId, pagesId, pageIds[0], Settings, catalogOptions);

        // 7. Write encryption dictionary if encrypted
        if (encryptId > 0) writer.WriteEncryptDictionary(encryptId);

        writer.Close(catalogId, documentInfoId);
    }

    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a file
    /// </summary>
    private async Task SaveInternalAsync(string path, CancellationToken cancellationToken)
    {
        ValidateSettings();
        await using AsyncPdfWriter writer = new(path, Settings.Version);
        await SaveWithWriterAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Internal async implementation of Save that generates the PDF to a stream
    /// </summary>
    private async Task SaveInternalAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken)
    {
        ValidateSettings();
        await using AsyncPdfWriter writer = new(stream, leaveOpen, Settings.Version);
        await SaveWithWriterAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Common async save logic that works with any AsyncPdfWriter instance
    /// Uses the shared PdfRenderPipeline to eliminate code duplication with SaveWithWriter.
    /// The rendering phase (Phase 1) is performed once by the pipeline, then this method
    /// handles the asynchronous I/O operations.
    /// PERFORMANCE NOTE: The rendering phase (PHASE 1) is CPU-bound and runs synchronously.
    /// Only the I/O operations (PHASE 2+) are truly async. For large documents,
    /// consider wrapping the entire SaveAsync call in Task.Run() to offload CPU work.
    /// MEMORY EFFICIENCY: This method uses streaming to process pages one at a time,
    /// keeping memory usage low for documents with 1000+ pages. Resources are moved to
    /// the parent Pages object, allowing pages to be written immediately.
    /// </summary>
    private async Task SaveWithWriterAsync(AsyncPdfWriter writer, CancellationToken cancellationToken)
    {
        // 0. Set up encryption if configured
        int encryptId = 0;
        if (Settings.IsEncrypted)
        {
            writer.Encryptor = new PdfEncryptor(Settings.Encryption);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = Settings.GetOrientedPageSize();
        RenderContext ctx = new(orientedPageSize, Settings.Margin);
        ctx.PdfAEnabled = Settings.PdfAEnabled;

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = new();

        // We use the new Streaming pipeline
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(_pages, Settings, ctx))
        {
            int pageId = writer.GetNewId();
            int contentId = writer.GetNewId();
            pageIds.Add(pageId);

            // A. Write Content Stream IMMEDIATELY (Flush memory)
            await writer.WriteCompressedObjectAsync(contentId, new Dictionary<string, string>(),
                pageOutput.ContentStream, cancellationToken).ConfigureAwait(false);

            // B. Write Page Object IMMEDIATELY
            // NOTICE: We removed "/Resources" from here! It will go on the Parent.
            Dictionary<string, string> pageDict = new()
            {
                ["/Type"] = "/Page",
                ["/Parent"] = $"{pagesId} 0 R", // Refers to the parent we write later
                ["/MediaBox"] = $"[0 0 {pageOutput.Size.Width} {pageOutput.Size.Height}]",
                ["/Contents"] = $"{contentId} 0 R"
            };

            // Write link annotations if present
            if (pageOutput.Hyperlinks.Count > 0)
            {
                List<int> annotIds = new();
                foreach (Hyperlink link in pageOutput.Hyperlinks)
                {
                    int annotId = writer.GetNewId();
                    await writer.WriteLinkAnnotationAsync(annotId, link, cancellationToken).ConfigureAwait(false);
                    annotIds.Add(annotId);
                }

                pageDict["/Annots"] = "[ " + string.Join(" ", annotIds.Select(id => $"{id} 0 R")) + " ]";
            }

            await writer.WriteObjectAsync(pageId, pageDict, null, cancellationToken).ConfigureAwait(false);

            // C. Clean up
            // The 'pageOutput' goes out of scope and is eligible for GC. 
            // We only hold 'pageIds' (integers) in memory.
        }

        // 4. Post-Processing (Write Resources)
        // Now that we've processed all pages, 'ctx' knows every Font/Image used.

        await WriteFontResourcesAsync(writer, ctx, cancellationToken).ConfigureAwait(false);
        await WriteImageResourcesAsync(writer, ctx, cancellationToken).ConfigureAwait(false);
        Dictionary<string, int> deferredIds =
            await WriteDeferredXObjectsAsync(writer, ctx, pageIds.Count, cancellationToken)
                .ConfigureAwait(false);

        string fontRes = BuildFontResourcesString(ctx);
        string imageRes = BuildXObjectResourcesString(ctx, deferredIds);
        string globalResources = CombineResources(fontRes, imageRes);

        // Write ExtGState for watermark opacity if configured
        if (Settings.Watermark != null)
        {
            int gsId = writer.GetNewId();
            // PDF/A-1B forbids CA/ca < 1.0 — use full opacity and blend the color instead
            float opacity = Settings.PdfAEnabled ? 1.0f : Settings.Watermark.Opacity;
            await writer.WriteObjectAsync(gsId, new Dictionary<string, string>
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = opacity.ToString("F4"),
                ["/CA"] = opacity.ToString("F4")
            }, null, cancellationToken).ConfigureAwait(false);
            string extGStateRes = $"<< /ExtGState << /GS_WM {gsId} 0 R >> >>";
            globalResources = CombineResources(globalResources, extGStateRes);
        }

        // 5. Write Parent Pages Object
        // We attach the Global Resources here. They inherit down to all pages.
        string kids = string.Join(" ", pageIds.Select(id => $"{id} 0 R"));
        await writer.WriteObjectAsync(pagesId, new Dictionary<string, string>
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString(),
            ["/Resources"] = globalResources // <--- Resources moved here!
        }, null, cancellationToken).ConfigureAwait(false);

        // 6. Write Metadata & Close
        await writer.WriteDocumentInfoAsync(infoId, Metadata, cancellationToken).ConfigureAwait(false);

        CatalogOptions catalogOptions = new();

        if (HasViewerPreferences(Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            await writer.WriteViewerPreferencesAsync(catalogOptions.ViewerPrefsId, Settings, cancellationToken).ConfigureAwait(false);
        }

        // Write XMP Metadata for PDF 1.7
        if (Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.XmpMetadataId = writer.GetNewId();
            await WriteXmpMetadataAsync(writer, catalogOptions.XmpMetadataId, cancellationToken).ConfigureAwait(false);
        }

        // Write Bookmarks for PDF 1.7
        if (Settings.Version == PdfVersion.Pdf17 && Settings.HasBookmarks)
        {
            int outlinesStartId = writer.GetNewId();
            catalogOptions.OutlinesId = await WriteOutlineTreeAsync(writer, outlinesStartId, Settings.Bookmarks, pageIds, cancellationToken).ConfigureAwait(false);
        }

        // Write PDF/A Output Intents
        if (Settings.PdfAEnabled && Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.PdfAEnabled = true;
            int iccProfileId = writer.GetNewId();
            await WriteIccProfileAsync(writer, iccProfileId, cancellationToken).ConfigureAwait(false);
            int outputIntentId = writer.GetNewId();
            await WriteOutputIntentAsync(writer, outputIntentId, iccProfileId, cancellationToken).ConfigureAwait(false);
            catalogOptions.OutputIntentIds = new List<int> { outputIntentId };
        }

        // Write Catalog
        await writer.WriteCatalogAsync(catalogId, pagesId, pageIds[0], Settings, catalogOptions, cancellationToken)
            .ConfigureAwait(false);

        // 7. Write encryption dictionary if encrypted
        if (encryptId > 0) await writer.WriteEncryptDictionaryAsync(encryptId, cancellationToken).ConfigureAwait(false);

        await writer.CloseAsync(catalogId, documentInfoId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Async version: Writes all font resources (both standard and embedded)
    /// NOTE: This duplicates logic from WriteFontResources - keep both in sync!
    /// </summary>
    private static async Task WriteFontResourcesAsync(AsyncPdfWriter writer, RenderContext ctx,
        CancellationToken cancellationToken)
    {
        FontWriter fontWriter = new(writer);

        foreach (KeyValuePair<string, string> fontEntry in ctx.FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value; // e.g., "/F1"

            // Skip alias entries (see WriteFontResources for explanation)
            if (ctx.FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (ctx.IsEmbeddedFont(fontIdentifier))
            {
                // Embedded TTF font - use async FontWriter
                EmbeddedFontInfo fontInfo = ctx.EmbeddedFonts[fontIdentifier];
                int type0Id = writer.GetNewId();
                fontInfo.Type0ObjectId = type0Id;
                ctx.FontIdMap[fontIdentifier] = type0Id;

                // Use full async font writing with subsetting support
                await fontWriter.WriteEmbeddedTtfFontAsync(type0Id, fontInfo, true, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                // Standard Type1 font — map to valid PDF base font name
                int fontId = writer.GetNewId();
                ctx.FontIdMap[fontIdentifier] = fontId;
                await fontWriter.WriteType1FontAsync(fontId, MapToStandardFontName(fontIdentifier), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Async version: Writes all image resources to the PDF
    /// NOTE: This duplicates logic from WriteImageResources - keep both in sync!
    /// </summary>
    private async Task WriteImageResourcesAsync(AsyncPdfWriter writer, RenderContext ctx,
        CancellationToken cancellationToken)
    {
        ImageWriter imageWriter = new(writer);
        imageWriter.PdfAEnabled = ctx.PdfAEnabled;

        _imagePdfObjectIds.Clear();

        foreach (KeyValuePair<string, ImageInfo> imageEntry in ctx.LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            // Allocate a proper PDF object ID from the writer
            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            // Get the resource name (e.g., /I1) based on RenderContext's internal ID
            int internalId = ctx.ImageIdMap[key];
            string resourceName = ctx.GetImageResourceName(internalId);

            // Use auto-detecting WriteImageAsync which handles both JPEG and PNG
            await imageWriter.WriteImageAsync(pdfObjectId, info, resourceName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Writes all font resources (both standard and embedded)
    /// </summary>
    private static void WriteFontResources(IPdfWriter writer, FontWriter fontWriter, RenderContext ctx)
    {
        foreach (KeyValuePair<string, string> fontEntry in ctx.FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value; // e.g., "/F1"

            // Skip alias entries — these are standard font names (e.g. "Helvetica") that were
            // auto-resolved to an embedded TTF path by PdfA mode. The TTF path entry in
            // FontNameMap will handle writing the embedded font; writing the alias as a
            // separate Type1 font would create a duplicate non-embedded entry.
            if (ctx.FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (ctx.IsEmbeddedFont(fontIdentifier))
            {
                // Embedded TTF font
                EmbeddedFontInfo fontInfo = ctx.EmbeddedFonts[fontIdentifier];
                int type0Id = writer.GetNewId();
                fontInfo.Type0ObjectId = type0Id;
                ctx.FontIdMap[fontIdentifier] = type0Id;
                fontWriter.WriteEmbeddedTtfFont(type0Id, fontInfo);
            }
            else
            {
                // Standard Type1 font — map to valid PDF base font name
                int fontId = writer.GetNewId();
                ctx.FontIdMap[fontIdentifier] = fontId;
                fontWriter.WriteType1Font(fontId, MapToStandardFontName(fontIdentifier));
            }
        }
    }

    /// <summary>
    /// Maps a user-supplied font identifier (e.g. "Arial, sans-serif") to a valid PDF standard font name.
    /// PDF name objects cannot contain commas or spaces.
    /// </summary>
    private static string MapToStandardFontName(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();
        return lower switch
        {
            var s when s.Contains("arial") || s.Contains("helvetica") => StandardFonts.Helvetica,
            var s when s.Contains("times") => StandardFonts.TimesRoman,
            var s when s.Contains("courier") || s.Contains("mono") => StandardFonts.Courier,
            _ => StandardFonts.Helvetica
        };
    }

    /// <summary>
    /// Resolves a font identifier to its PDF object ID, following aliases when PdfA
    /// auto-resolved a standard font name to an embedded TTF path.
    /// </summary>
    private static int ResolveFontObjectId(RenderContext ctx, string fontIdentifier)
    {
        if (ctx.FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath))
            return ctx.FontIdMap[ttfPath];
        return ctx.FontIdMap[fontIdentifier];
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

    // ---- XMP Metadata ----

    private void WriteXmpMetadata(IPdfWriter writer, int id)
    {
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(Metadata, Settings.PdfAEnabled);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        // XMP metadata must NOT be encrypted and must NOT be compressed
        writer.WriteUnencryptedBinaryObject(id, dict, xmpBytes);
    }

    private async Task WriteXmpMetadataAsync(AsyncPdfWriter writer, int id, CancellationToken cancellationToken)
    {
        string xmpXml = XmpMetadataWriter.GenerateXmpXml(Metadata, Settings.PdfAEnabled);
        byte[] xmpBytes = Encoding.UTF8.GetBytes(xmpXml);

        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Metadata",
            ["/Subtype"] = "/XML"
        };

        await writer.WriteUnencryptedBinaryObjectAsync(id, dict, xmpBytes, cancellationToken).ConfigureAwait(false);
    }

    // ---- ICC Profile (PDF/A) ----

    private static void WriteIccProfile(IPdfWriter writer, int id)
    {
        byte[] iccData = SRgbIccProfile.Generate();

        // Compress the ICC profile
        using MemoryStream ms = new();
        using (ZLibStream zlib = new(ms, CompressionLevel.Optimal))
        {
            zlib.Write(iccData, 0, iccData.Length);
        }
        byte[] compressed = ms.ToArray();

        Dictionary<string, string> dict = new()
        {
            ["/N"] = "3",
            ["/Filter"] = "/FlateDecode",
            ["/Length1"] = iccData.Length.ToString()
        };

        writer.WriteBinaryObject(id, dict, compressed);
    }

    private static async Task WriteIccProfileAsync(AsyncPdfWriter writer, int id, CancellationToken cancellationToken)
    {
        byte[] iccData = SRgbIccProfile.Generate();

        using MemoryStream ms = new();
        using (ZLibStream zlib = new(ms, CompressionLevel.Optimal))
        {
            await zlib.WriteAsync(iccData, 0, iccData.Length, cancellationToken).ConfigureAwait(false);
        }
        byte[] compressed = ms.ToArray();

        Dictionary<string, string> dict = new()
        {
            ["/N"] = "3",
            ["/Filter"] = "/FlateDecode",
            ["/Length1"] = iccData.Length.ToString()
        };

        await writer.WriteBinaryObjectAsync(id, dict, compressed, compressed.Length, cancellationToken).ConfigureAwait(false);
    }

    // ---- Output Intent (PDF/A) ----

    private static void WriteOutputIntent(IPdfWriter writer, int id, int iccProfileId)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/OutputIntent",
            ["/S"] = "/GTS_PDFA1",
            ["/OutputConditionIdentifier"] = "(sRGB IEC61966-2.1)",
            ["/RegistryName"] = "(http://www.color.org)",
            ["/DestOutputProfile"] = $"{iccProfileId} 0 R"
        };

        writer.WriteObject(id, dict);
    }

    private static async Task WriteOutputIntentAsync(AsyncPdfWriter writer, int id, int iccProfileId, CancellationToken cancellationToken)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/OutputIntent",
            ["/S"] = "/GTS_PDFA1",
            ["/OutputConditionIdentifier"] = "(sRGB IEC61966-2.1)",
            ["/RegistryName"] = "(http://www.color.org)",
            ["/DestOutputProfile"] = $"{iccProfileId} 0 R"
        };

        await writer.WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
    }

    // ---- Bookmarks / Outlines ----

    private static int WriteOutlineTree(IPdfWriter writer, int rootId, List<BookmarkItem> bookmarks, List<int> pageIds)
    {
        if (bookmarks == null || bookmarks.Count == 0)
            return 0;

        // Flatten the bookmark tree to assign IDs
        List<(BookmarkItem item, int id, int parentId, int depth)> flatList = new();
        int nextId = rootId + 1; // rootId is reserved for the /Outlines root

        void Flatten(List<BookmarkItem> items, int parentId, int depth)
        {
            foreach (BookmarkItem item in items)
            {
                int itemId = writer.GetNewId();
                flatList.Add((item, itemId, parentId, depth));
                if (item.Children.Count > 0)
                    Flatten(item.Children, itemId, depth + 1);
            }
        }

        Flatten(bookmarks, rootId, 0);

        // Group children by parent
        Dictionary<int, List<int>> childrenByParent = new();
        foreach (var entry in flatList)
        {
            if (!childrenByParent.ContainsKey(entry.parentId))
                childrenByParent[entry.parentId] = new List<int>();
            childrenByParent[entry.parentId].Add(flatList.IndexOf(entry));
        }

        // Count total visible items
        int CountVisible(int parentId)
        {
            if (!childrenByParent.ContainsKey(parentId))
                return 0;
            int count = 0;
            foreach (int idx in childrenByParent[parentId])
            {
                count++;
                count += CountVisible(flatList[idx].id);
            }
            return count;
        }

        int totalCount = CountVisible(rootId);

        // Write root /Outlines object
        List<int> rootChildren = childrenByParent.GetValueOrDefault(rootId) ?? new();
        Dictionary<string, string> rootDict = new()
        {
            ["/Type"] = "/Outlines",
            ["/Count"] = totalCount.ToString()
        };
        if (rootChildren.Count > 0)
        {
            rootDict["/First"] = $"{flatList[rootChildren[0]].id} 0 R";
            rootDict["/Last"] = $"{flatList[rootChildren[^1]].id} 0 R";
        }
        writer.WriteObject(rootId, rootDict);

        // Write each bookmark node
        for (int i = 0; i < flatList.Count; i++)
        {
            var (item, itemId, parentId, depth) = flatList[i];
            List<int> siblings = childrenByParent[parentId];
            int siblingIdx = siblings.IndexOf(i);

            Dictionary<string, string> dict = new()
            {
                ["/Title"] = $"({PdfSyntax.Escape(item.Title)})",
                ["/Parent"] = $"{parentId} 0 R"
            };

            // Destination
            int pageIdx = Math.Min(item.PageIndex, pageIds.Count - 1);
            pageIdx = Math.Max(pageIdx, 0);
            dict["/Dest"] = $"[ {pageIds[pageIdx]} 0 R /Fit ]";

            // Prev/Next siblings
            if (siblingIdx > 0)
                dict["/Prev"] = $"{flatList[siblings[siblingIdx - 1]].id} 0 R";
            if (siblingIdx < siblings.Count - 1)
                dict["/Next"] = $"{flatList[siblings[siblingIdx + 1]].id} 0 R";

            // Children
            List<int> myChildren = childrenByParent.GetValueOrDefault(itemId);
            if (myChildren is { Count: > 0 })
            {
                int childCount = CountVisible(itemId);
                dict["/Count"] = childCount.ToString();
                dict["/First"] = $"{flatList[myChildren[0]].id} 0 R";
                dict["/Last"] = $"{flatList[myChildren[^1]].id} 0 R";
            }

            writer.WriteObject(itemId, dict);
        }

        return rootId;
    }

    private static async Task<int> WriteOutlineTreeAsync(AsyncPdfWriter writer, int rootId, List<BookmarkItem> bookmarks, List<int> pageIds, CancellationToken cancellationToken)
    {
        if (bookmarks == null || bookmarks.Count == 0)
            return 0;

        List<(BookmarkItem item, int id, int parentId, int depth)> flatList = new();

        void Flatten(List<BookmarkItem> items, int parentId, int depth)
        {
            foreach (BookmarkItem item in items)
            {
                int itemId = writer.GetNewId();
                flatList.Add((item, itemId, parentId, depth));
                if (item.Children.Count > 0)
                    Flatten(item.Children, itemId, depth + 1);
            }
        }

        Flatten(bookmarks, rootId, 0);

        Dictionary<int, List<int>> childrenByParent = new();
        foreach (var entry in flatList)
        {
            if (!childrenByParent.ContainsKey(entry.parentId))
                childrenByParent[entry.parentId] = new List<int>();
            childrenByParent[entry.parentId].Add(flatList.IndexOf(entry));
        }

        int CountVisible(int parentId)
        {
            if (!childrenByParent.ContainsKey(parentId))
                return 0;
            int count = 0;
            foreach (int idx in childrenByParent[parentId])
            {
                count++;
                count += CountVisible(flatList[idx].id);
            }
            return count;
        }

        int totalCount = CountVisible(rootId);

        List<int> rootChildren = childrenByParent.GetValueOrDefault(rootId) ?? new();
        Dictionary<string, string> rootDict = new()
        {
            ["/Type"] = "/Outlines",
            ["/Count"] = totalCount.ToString()
        };
        if (rootChildren.Count > 0)
        {
            rootDict["/First"] = $"{flatList[rootChildren[0]].id} 0 R";
            rootDict["/Last"] = $"{flatList[rootChildren[^1]].id} 0 R";
        }
        await writer.WriteObjectAsync(rootId, rootDict, null, cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < flatList.Count; i++)
        {
            var (item, itemId, parentId, depth) = flatList[i];
            List<int> siblings = childrenByParent[parentId];
            int siblingIdx = siblings.IndexOf(i);

            Dictionary<string, string> dict = new()
            {
                ["/Title"] = $"({PdfSyntax.Escape(item.Title)})",
                ["/Parent"] = $"{parentId} 0 R"
            };

            int pageIdx = Math.Min(item.PageIndex, pageIds.Count - 1);
            pageIdx = Math.Max(pageIdx, 0);
            dict["/Dest"] = $"[ {pageIds[pageIdx]} 0 R /Fit ]";

            if (siblingIdx > 0)
                dict["/Prev"] = $"{flatList[siblings[siblingIdx - 1]].id} 0 R";
            if (siblingIdx < siblings.Count - 1)
                dict["/Next"] = $"{flatList[siblings[siblingIdx + 1]].id} 0 R";

            List<int> myChildren = childrenByParent.GetValueOrDefault(itemId);
            if (myChildren is { Count: > 0 })
            {
                int childCount = CountVisible(itemId);
                dict["/Count"] = childCount.ToString();
                dict["/First"] = $"{flatList[myChildren[0]].id} 0 R";
                dict["/Last"] = $"{flatList[myChildren[^1]].id} 0 R";
            }

            await writer.WriteObjectAsync(itemId, dict, null, cancellationToken).ConfigureAwait(false);
        }

        return rootId;
    }

    /// <summary>
    /// Checks if the settings contain any viewer preferences that need to be written
    /// </summary>
    private static bool HasViewerPreferences(DocumentSettings settings)
    {
        return settings.HideToolbar ||
               settings.HideMenubar ||
               settings.HideWindowUI ||
               settings.FitWindow ||
               settings.CenterWindow ||
               settings.DisplayDocTitle;
    }

    /// <summary>
    /// Builds the font resources string for page objects
    /// </summary>
    private static string BuildFontResourcesString(RenderContext ctx)
    {
        List<string> fontRefs = new();

        foreach (KeyValuePair<string, string> fontEntry in ctx.FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value; // e.g., "/F1"

            // Skip alias entries — the TTF path entry covers the same resource name
            if (ctx.FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            int objectId = ctx.FontIdMap[fontIdentifier];

            fontRefs.Add($"{resourceName} {objectId} 0 R");
        }

        return "<< /Font << " + string.Join(" ", fontRefs) + " >> >>";
    }

    /// <summary>
    /// Writes all image resources to the PDF
    /// </summary>
    private void WriteImageResources(IPdfWriter writer, ImageWriter imageWriter, RenderContext ctx)
    {
        _imagePdfObjectIds.Clear();

        foreach (KeyValuePair<string, ImageInfo> imageEntry in ctx.LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            // Allocate a proper PDF object ID from the writer
            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            // Get the resource name (e.g., /I1) based on RenderContext's internal ID
            int internalId = ctx.ImageIdMap[key];
            string resourceName = ctx.GetImageResourceName(internalId);

            // Use auto-detecting WriteImage which handles both JPEG and PNG
            imageWriter.WriteImage(pdfObjectId, info, resourceName);
        }
    }


    /// <summary>
    /// Combines font and image resource strings
    /// </summary>
    private static string CombineResources(string fontResources, string imageResources)
    {
        if (string.IsNullOrEmpty(imageResources))
            return fontResources;

        // Merge the two resource dictionaries into one
        // fontResources: "<< /Font << /F1 3 0 R >> >>"
        // imageResources: "<< /XObject << /I1 5 0 R >> >>"
        // Result: "<< /Font << /F1 3 0 R >> /XObject << /I1 5 0 R >> >>"

        // Strip the outer "<< " and " >>" from imageResources (exact prefix/suffix, not TrimStart/TrimEnd
        // which would remove ALL leading/trailing angle brackets and corrupt inner dictionaries)
        string imageContent = imageResources;
        if (imageContent.StartsWith("<< "))
            imageContent = imageContent.Substring(3);
        if (imageContent.EndsWith(" >>"))
            imageContent = imageContent.Substring(0, imageContent.Length - 3);

        // Strip the trailing " >>" from fontResources and append the image content
        string fontBase = fontResources;
        if (fontBase.EndsWith(" >>"))
            fontBase = fontBase.Substring(0, fontBase.Length - 3);

        return fontBase + " " + imageContent + " >>";
    }

    /// <summary>
    /// Builds the XObject resources string for page objects (Images + Deferred Forms)
    /// </summary>
    private string BuildXObjectResourcesString(RenderContext ctx, Dictionary<string, int> deferredIds)
    {
        List<string> refs = new();

        // 1. Images
        if (ctx.ImageIdMap.Count > 0)
            foreach (KeyValuePair<string, int> imageEntry in ctx.ImageIdMap)
            {
                string key = imageEntry.Key;
                int internalId = imageEntry.Value;

                // Get the actual PDF object ID we allocated
                if (_imagePdfObjectIds.TryGetValue(key, out int pdfObjectId))
                {
                    string resourceName = ctx.GetImageResourceName(internalId);
                    refs.Add($"{resourceName} {pdfObjectId} 0 R");
                }
            }

        // 2. Deferred XObjects (Total Pages, etc.)
        if (deferredIds != null && deferredIds.Count > 0)
            foreach (KeyValuePair<string, int> kvp in deferredIds)
            {
                string signature = kvp.Key;
                int objectId = kvp.Value;

                if (ctx.DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo info))
                    refs.Add($"{info.ResourceName} {objectId} 0 R");
            }

        if (refs.Count == 0)
            return "";

        return "<< /XObject << " + string.Join(" ", refs) + " >> >>";
    }

    private Dictionary<string, int> WriteDeferredXObjects(IPdfWriter writer, RenderContext ctx, int totalPages)
    {
        Dictionary<string, int> ids = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in ctx.DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            ids[kvp.Key] = id;

            // Content: Draw the number text
            // We draw at (0, -FontSize) to position baseline correctly relative to Top-Left origin
            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            // Encode text appropriately — embedded fonts use hex glyph IDs
            TtfFont embeddedFont = ctx.ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                ctx.TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                // BBox: Covers the text area.
                // X: 0 to 100 (plenty of space).
                // Y: -FontSize*2 to 0 (covers the text drawn at -FontSize).
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                // Font Resource for the text
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(ctx, info.FontIdentifier)} 0 R >> >>"
            };

            writer.WriteObject(id, dict, content);
        }

        return ids;
    }

    private async Task<Dictionary<string, int>> WriteDeferredXObjectsAsync(AsyncPdfWriter writer, RenderContext ctx,
        int totalPages, CancellationToken cancellationToken)
    {
        Dictionary<string, int> ids = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in ctx.DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            ids[kvp.Key] = id;

            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            // Encode text appropriately — embedded fonts use hex glyph IDs
            TtfFont embeddedFont = ctx.ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                ctx.TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(ctx, info.FontIdentifier)} 0 R >> >>"
            };

            await writer.WriteObjectAsync(id, dict, content, cancellationToken).ConfigureAwait(false);
        }

        return ids;
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