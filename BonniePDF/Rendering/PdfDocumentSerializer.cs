using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Fluent;
using BonniePDF.Rendering.Writers;

namespace BonniePDF.Rendering;

/// <summary>
/// Orchestrates the process of rendering a Document and writing it to a PDF file.
/// Separates the low-level serialization logic from the high-level Document API.
/// </summary>
public class PdfDocumentSerializer
{
    private readonly Document _document;

    /// <summary>
    /// Creates a new document serializer for a given document.
    /// </summary>
    /// <param name="document">The document to be serialized.</param>
    public PdfDocumentSerializer(Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
    }

    /// <summary>
    /// Serializes the document synchronously to the provided writer.
    /// </summary>
    public void Serialize(IPdfWriter writer)
    {
        // 0. Set up encryption if configured
        int encryptId = 0;
        if (_document.Settings.IsEncrypted)
        {
            writer.Encryptor = new Security.Encryption.PdfEncryptor(_document.Settings.Encryption);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = _document.Settings.GetOrientedPageSize();

        IDictionary<string, int> resolvedTocEntries = null;

        if (RequiresTwoPassLayout())
        {
            RenderContext dryRunCtx = new(orientedPageSize, _document.Settings.Margin)
            {
                PdfAEnabled = _document.Settings.PdfAEnabled
            };

            foreach (var _ in PdfRenderPipeline.RenderPagesStreaming(_document.Pages, _document.Settings, dryRunCtx)) { }

            resolvedTocEntries = dryRunCtx.TocEntries;

            if (_document.Settings.Bookmarks.Count == 0 && resolvedTocEntries.Count > 0)
            {
                foreach (var kvp in resolvedTocEntries)
                {
                    _document.Settings.Bookmarks.Add(new BookmarkItem { Title = kvp.Key, PageIndex = kvp.Value - 1 });
                }
            }
        }

        // PDF/UA tagging context — created once, shared between the
        // render pipeline (canvas attaches MCIDs to the builder) and the
        // structure-tree writer at the end (emits the populated tree).
        TaggingContext taggingContext = CreateTaggingContext();

        RenderContext ctx = new(orientedPageSize, _document.Settings.Margin)
        {
            PdfAEnabled = _document.Settings.PdfAEnabled,
            TocEntries = resolvedTocEntries,
            Tagging = taggingContext,
        };

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = [];

        if (RequiresTwoPassLayout())
        {
            int expectedTotalPages = resolvedTocEntries.Count > 0 ? resolvedTocEntries.Values.Max() + 1 : 0;
            // Pre-allocate page IDs with a generous padding
            for (int i = 0; i < Math.Max(expectedTotalPages + 50, 100); i++)
            {
                pageIds.Add(writer.GetNewId());
            }
        }

        int pageIndex = 0;
        // We use the new Streaming pipeline
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(_document.Pages, _document.Settings, ctx))
        {
            int pageId;
            if (pageIndex < pageIds.Count)
            {
                pageId = pageIds[pageIndex];
            }
            else
            {
                pageId = writer.GetNewId();
                pageIds.Add(pageId);
            }
            pageIndex++;
            int contentId = writer.GetNewId();

            // A. Write Content Stream IMMEDIATELY (Flush memory)
            writer.WriteCompressedObject(contentId, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), pageOutput.ContentStream);

            // B. Write Page Object IMMEDIATELY
            // NOTICE: We removed "/Resources" from here! It will go on the Parent.
            Dictionary<string, string> pageDict = new(StringComparer.OrdinalIgnoreCase)
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
                    writer.WriteLinkAnnotation(annotId, link, pageIds);
                    annotIds.Add(annotId);
                }

                pageDict["/Annots"] = "[ " + string.Join(' ', annotIds.Select(id => $"{id} 0 R")) + " ]";
            }

            writer.WriteObject(pageId, pageDict);
        }

        // Trim any pre-allocated page IDs that were never written
        if (pageIds.Count > pageIndex)
            pageIds.RemoveRange(pageIndex, pageIds.Count - pageIndex);

        // 4. Post-Processing (Write Resources)
        string globalResources = ctx.Resources.WriteResources(writer, pageIds.Count);

        // Write ExtGState for watermark opacity if configured
        if (_document.Settings.Watermark != null)
        {
            int gsId = writer.GetNewId();
            // PDF/A-1B forbids CA/ca < 1.0 — use full opacity and blend the color instead
            float opacity = _document.Settings.PdfAEnabled ? 1.0f : _document.Settings.Watermark.Opacity;
            writer.WriteObject(gsId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = opacity.ToString("F4"),
                ["/CA"] = opacity.ToString("F4")
            });
            string extGStateRes = $"<< /ExtGState << /GS_WM {gsId} 0 R >> >>";

            // Inline CombineResources equivalent
            string MergeDicts(string d1, string d2)
            {
                if (string.IsNullOrEmpty(d2)) return d1;
                string t2 = d2.StartsWith("<< ", StringComparison.OrdinalIgnoreCase) ? d2.Substring(3) : d2;
                t2 = t2.EndsWith(" >>", StringComparison.OrdinalIgnoreCase) ? t2.Substring(0, t2.Length - 3) : t2;
                string t1 = d1.EndsWith(" >>", StringComparison.OrdinalIgnoreCase) ? d1.Substring(0, d1.Length - 3) : d1;
                return t1 + ' ' + t2 + " >>";
            }

            globalResources = MergeDicts(globalResources, extGStateRes);
        }

        // 5. Write Parent Pages Object
        string kids = string.Join(' ', pageIds.Select(id => $"{id} 0 R"));
        writer.WriteObject(pagesId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString(),
            ["/Resources"] = globalResources
        });

        // 6. Write Metadata & Close
        MetadataWriter metadataWriter = new(writer);
        metadataWriter.WriteDocumentInfo(infoId, _document.Metadata);

        CatalogOptions catalogOptions = new();

        if (HasViewerPreferences(_document.Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            writer.WriteViewerPreferences(catalogOptions.ViewerPrefsId, _document.Settings);
        }

        // Write XMP Metadata for PDF 1.7
        if (_document.Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.XmpMetadataId = writer.GetNewId();
            metadataWriter.WriteXmpMetadata(catalogOptions.XmpMetadataId, _document.Metadata, _document.Settings);
        }

        // Write Bookmarks for PDF 1.7
        if (_document.Settings.Version == PdfVersion.Pdf17 && _document.Settings.HasBookmarks)
        {
            NavigationWriter navWriter = new(writer);
            int outlinesStartId = writer.GetNewId();
            catalogOptions.OutlinesId = navWriter.WriteOutlineTree(outlinesStartId, _document.Settings.Bookmarks, pageIds);
        }

        // Write PDF/A Output Intents
        if (_document.Settings.PdfAEnabled && _document.Settings.Version == PdfVersion.Pdf17)
        {
            ComplianceWriter complianceWriter = new(writer);
            catalogOptions.PdfAEnabled = true;
            int iccProfileId = writer.GetNewId();
            complianceWriter.WriteIccProfile(iccProfileId);
            int outputIntentId = writer.GetNewId();
            complianceWriter.WriteOutputIntent(outputIntentId, iccProfileId);
            catalogOptions.OutputIntentIds = new List<int> { outputIntentId };
        }

        // Write PDF/UA structure tree. The tree was populated during the
        // render pass via taggingContext (this feature wire MCIDs and
        // semantic roles through the canvas). For this feature the tree is still
        // a Document-only skeleton; this feature adds the renderer-driven
        // population.
        if (taggingContext != null)
        {
            StructureTreeWriter structureTreeWriter = new(writer);
            catalogOptions.StructTreeRootId = structureTreeWriter.Write(taggingContext.Tree);
            catalogOptions.Tagged = true;
            catalogOptions.Language = _document.Settings.Language;
        }

        // Write Catalog
        writer.WriteCatalog(catalogId, pagesId, pageIds[0], _document.Settings, catalogOptions);

        // 7. Write encryption dictionary if encrypted
        if (encryptId > 0) writer.WriteEncryptDictionary(encryptId);

        writer.Close(catalogId, documentInfoId);
    }

    /// <summary>
    /// Serializes the document asynchronously to the provided writer.
    /// </summary>
    public async Task SerializeAsync(IAsyncPdfWriter writer, CancellationToken cancellationToken)
    {
        // 0. Set up encryption if configured
        int encryptId = 0;
        if (_document.Settings.IsEncrypted)
        {
            writer.Encryptor = new Security.Encryption.PdfEncryptor(_document.Settings.Encryption);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = _document.Settings.GetOrientedPageSize();

        IDictionary<string, int> resolvedTocEntries = null;

        if (RequiresTwoPassLayout())
        {
            RenderContext dryRunCtx = new(orientedPageSize, _document.Settings.Margin)
            {
                PdfAEnabled = _document.Settings.PdfAEnabled
            };

            foreach (var _ in PdfRenderPipeline.RenderPagesStreaming(_document.Pages, _document.Settings, dryRunCtx)) { }

            resolvedTocEntries = dryRunCtx.TocEntries;

            if (_document.Settings.Bookmarks.Count == 0 && resolvedTocEntries.Count > 0)
            {
                foreach (var kvp in resolvedTocEntries)
                {
                    _document.Settings.Bookmarks.Add(new BookmarkItem { Title = kvp.Key, PageIndex = kvp.Value - 1 });
                }
            }
        }

        // PDF/UA tagging context — shared between the render pipeline and
        // the async structure-tree writer below.
        TaggingContext taggingContext = CreateTaggingContext();

        RenderContext ctx = new(orientedPageSize, _document.Settings.Margin);
        ctx.PdfAEnabled = _document.Settings.PdfAEnabled;
        ctx.TocEntries = resolvedTocEntries;
        ctx.Tagging = taggingContext;

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = new();

        if (RequiresTwoPassLayout())
        {
            int expectedTotalPages = resolvedTocEntries.Count > 0 ? resolvedTocEntries.Values.Max() + 1 : 0;
            // Pre-allocate page IDs with a generous padding
            for (int i = 0; i < Math.Max(expectedTotalPages + 50, 100); i++)
            {
                pageIds.Add(writer.GetNewId());
            }
        }

        int pageIndex = 0;
        // We use the new Streaming pipeline
        foreach (PageRenderOutput pageOutput in PdfRenderPipeline.RenderPagesStreaming(_document.Pages, _document.Settings, ctx))
        {
            int pageId;
            if (pageIndex < pageIds.Count)
            {
                pageId = pageIds[pageIndex];
            }
            else
            {
                pageId = writer.GetNewId();
                pageIds.Add(pageId);
            }
            pageIndex++;
            int contentId = writer.GetNewId();

            // A. Write Content Stream IMMEDIATELY (Flush memory)
            await writer.WriteCompressedObjectAsync(contentId, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                pageOutput.ContentStream, cancellationToken).ConfigureAwait(false);

            // B. Write Page Object IMMEDIATELY
            Dictionary<string, string> pageDict = new(StringComparer.OrdinalIgnoreCase)
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
                    await writer.WriteLinkAnnotationAsync(annotId, link, pageIds, cancellationToken).ConfigureAwait(false);
                    annotIds.Add(annotId);
                }

                pageDict["/Annots"] = "[ " + string.Join(' ', annotIds.Select(id => $"{id} 0 R")) + " ]";
            }

            await writer.WriteObjectAsync(pageId, pageDict, null, cancellationToken).ConfigureAwait(false);
        }

        // Trim any pre-allocated page IDs that were never written
        if (pageIds.Count > pageIndex)
            pageIds.RemoveRange(pageIndex, pageIds.Count - pageIndex);

        // 4. Post-Processing (Write Resources)
        string globalResources = await ctx.Resources.WriteResourcesAsync(writer, pageIds.Count, cancellationToken).ConfigureAwait(false);

        // Write ExtGState for watermark opacity if configured
        if (_document.Settings.Watermark != null)
        {
            int gsId = writer.GetNewId();
            float opacity = _document.Settings.PdfAEnabled ? 1.0f : _document.Settings.Watermark.Opacity;
            await writer.WriteObjectAsync(gsId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
            {
                ["/Type"] = "/ExtGState",
                ["/ca"] = opacity.ToString("F4"),
                ["/CA"] = opacity.ToString("F4")
            }, null, cancellationToken).ConfigureAwait(false);
            string extGStateRes = $"<< /ExtGState << /GS_WM {gsId} 0 R >> >>";

            static string MergeDicts(string d1, string d2)
            {
                if (string.IsNullOrEmpty(d2)) return d1;
                string t2 = d2.StartsWith("<< ", StringComparison.OrdinalIgnoreCase) ? d2.Substring(3) : d2;
                t2 = t2.EndsWith(" >>", StringComparison.OrdinalIgnoreCase) ? t2.Substring(0, t2.Length - 3) : t2;
                string t1 = d1.EndsWith(" >>", StringComparison.OrdinalIgnoreCase) ? d1.Substring(0, d1.Length - 3) : d1;
                return t1 + ' ' + t2 + " >>";
            }

            globalResources = MergeDicts(globalResources, extGStateRes);
        }

        // 5. Write Parent Pages Object
        string kids = string.Join(' ', pageIds.Select(id => $"{id} 0 R"));
        await writer.WriteObjectAsync(pagesId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString(),
            ["/Resources"] = globalResources
        }, null, cancellationToken).ConfigureAwait(false);

        // 6. Write Metadata & Close
        MetadataWriter metadataWriter = new(writer);
        await metadataWriter.WriteDocumentInfoAsync(infoId, _document.Metadata, cancellationToken).ConfigureAwait(false);

        CatalogOptions catalogOptions = new();

        if (HasViewerPreferences(_document.Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            await writer.WriteViewerPreferencesAsync(catalogOptions.ViewerPrefsId, _document.Settings, cancellationToken).ConfigureAwait(false);
        }

        // Write XMP Metadata for PDF 1.7
        if (_document.Settings.Version == PdfVersion.Pdf17)
        {
            catalogOptions.XmpMetadataId = writer.GetNewId();
            await metadataWriter.WriteXmpMetadataAsync(catalogOptions.XmpMetadataId, _document.Metadata, _document.Settings, cancellationToken).ConfigureAwait(false);
        }

        // Write Bookmarks for PDF 1.7
        if (_document.Settings.Version == PdfVersion.Pdf17 && _document.Settings.HasBookmarks)
        {
            NavigationWriter navWriter = new(writer);
            int outlinesStartId = writer.GetNewId();
            catalogOptions.OutlinesId = await navWriter.WriteOutlineTreeAsync(outlinesStartId, _document.Settings.Bookmarks, pageIds, cancellationToken).ConfigureAwait(false);
        }

        // Write PDF/A Output Intents
        if (_document.Settings.PdfAEnabled && _document.Settings.Version == PdfVersion.Pdf17)
        {
            ComplianceWriter complianceWriter = new(writer);
            catalogOptions.PdfAEnabled = true;
            int iccProfileId = writer.GetNewId();
            await complianceWriter.WriteIccProfileAsync(iccProfileId, cancellationToken).ConfigureAwait(false);
            int outputIntentId = writer.GetNewId();
            await complianceWriter.WriteOutputIntentAsync(outputIntentId, iccProfileId, cancellationToken).ConfigureAwait(false);
            catalogOptions.OutputIntentIds = new List<int> { outputIntentId };
        }

        // Write PDF/UA structure tree (async). See sync path for the
        // rationale. taggingContext carries the same builder that the
        // canvas populated during render.
        if (taggingContext != null)
        {
            StructureTreeWriter structureTreeWriter = new(writer);
            catalogOptions.StructTreeRootId = await structureTreeWriter.WriteAsync(taggingContext.Tree, cancellationToken)
                .ConfigureAwait(false);
            catalogOptions.Tagged = true;
            catalogOptions.Language = _document.Settings.Language;
        }

        // Write Catalog
        await writer.WriteCatalogAsync(catalogId, pagesId, pageIds[0], _document.Settings, catalogOptions, cancellationToken)
            .ConfigureAwait(false);

        // 7. Write encryption dictionary if encrypted
        if (encryptId > 0) await writer.WriteEncryptDictionaryAsync(encryptId, cancellationToken).ConfigureAwait(false);

        await writer.CloseAsync(catalogId, documentInfoId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Returns <c>true</c> if any non-default viewer preferences are configured on the document settings
    /// (e.g. HideToolbar, FitWindow), indicating that a ViewerPreferences dictionary should be written.
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
    /// Returns a <see cref="TaggingContext"/> if the document opted into
    /// PDF/UA tagging (Accessibility != None and a PDF version that
    /// supports structure trees); otherwise <c>null</c>. The returned
    /// context is shared between the render pipeline (canvas writes
    /// MCIDs to the builder) and the structure-tree writer (emits the
    /// populated tree into the PDF object graph).
    /// </summary>
    private TaggingContext CreateTaggingContext()
    {
        if (_document.Settings.Accessibility == AccessibilityMode.None) return null;
        if (_document.Settings.Version != PdfVersion.Pdf17) return null;
        return new TaggingContext(new StructureTreeBuilder());
    }

    /// <summary>
    /// Returns <c>true</c> if the document contains a <see cref="VisualToc"/> element,
    /// which requires a two-pass layout to resolve heading page numbers before rendering.
    /// </summary>
    private bool RequiresTwoPassLayout()
    {
        return _document.Pages.Any(page => page.Elements.Any(ContainsVisualToc));
    }

    /// <summary>
    /// Recursively checks whether an element tree contains a <see cref="VisualToc"/> element.
    /// Descends into layout containers such as <see cref="VerticalStack"/>, <see cref="HorizontalStack"/>,
    /// and <see cref="Container"/>.
    /// </summary>
    private static bool ContainsVisualToc(IElement element)
    {
        if (element is VisualToc) return true;
        if (element is VerticalStack vs) return vs.Children.Any(ContainsVisualToc);
        if (element is HorizontalStack hs) return hs.Children.Any(ContainsVisualToc);
        if (element is Container c && c.Child != null) return ContainsVisualToc(c.Child);
        // IContentContainer covers fluent API slots (ContentSlot) that wrap a single child element
        if (element is IContentContainer cc && cc.Child != null) return ContainsVisualToc(cc.Child);
        return false;
    }
}
