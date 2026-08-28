using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Attachments;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Fluent;
using BonniePDF.Rendering.Writers;
using BonniePDF.Typography;

namespace BonniePDF.Rendering;

/// <summary>
/// Orchestrates the process of rendering a Document and writing it to a PDF file.
/// Separates the low-level serialization logic from the high-level Document API.
/// </summary>
public class PdfDocumentSerializer
{
    private readonly Document _document;
    private readonly DiagnosticsCollector _diagnostics;

    /// <summary>
    /// Creates a new document serializer for a given document.
    /// </summary>
    /// <param name="document">The document to be serialized.</param>
    /// <param name="diagnostics">Optional collector for recoverable failures; defaults to a private instance.</param>
    public PdfDocumentSerializer(Document document, DiagnosticsCollector? diagnostics = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _diagnostics = diagnostics ?? new DiagnosticsCollector();
    }

    /// <summary>
    /// Serializes the document synchronously to the provided writer.
    /// </summary>
    public void Serialize(IPdfWriter writer)
    {
        using IDisposable fontScope = FontMetrics.BeginDocumentScope(UsesEmbeddedStandardFonts());

        // 0. Set up encryption if configured
        int encryptId = 0;
        if (_document.Settings.IsEncrypted)
        {
            writer.Encryptor = new Security.Encryption.PdfEncryptor(_document.Settings.Encryption!);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;

        // Resources are written as a single indirect object that every
        // page references explicitly (PDF/A-2b clause 6.2.2.2 / ISO
        // 32000-1 §7.8.3 - relying on /Pages-inheritance is rejected by
        // veraPDF profile 2b even though §7.7.3.4 allows it). Allocate
        // the ID up-front so pages can reference it during the streaming
        // page loop; the body is written after rendering completes.
        int resourcesId = writer.GetNewId();

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = _document.Settings.GetOrientedPageSize();

        IDictionary<string, int>? resolvedTocEntries = null;

        if (RequiresTwoPassLayout())
        {
            RenderContext dryRunCtx = new(orientedPageSize, _document.Settings.Margin)
            {
                Conformance = _document.Settings.Conformance,
                TocEntries = new Dictionary<string, int>(),
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

        // PDF/UA tagging context - created once, shared between the
        // render pipeline (canvas attaches MCIDs to the builder) and the
        // structure-tree writer at the end (emits the populated tree).
        TaggingContext? taggingContext = CreateTaggingContext();

        RenderContext ctx = new(orientedPageSize, _document.Settings.Margin)
        {
            Conformance = _document.Settings.Conformance,
            TocEntries = resolvedTocEntries ?? new Dictionary<string, int>(),
            Tagging = taggingContext,
        };
        ctx.Resources.Diagnostics = _diagnostics;
        // PDF/UA-1 mandates font embedding (ISO 14289-1, veraPDF 7.21.4.1)
        // independently of any PDF/A claim - substitute embedded Liberation for
        // the standard-14 references in tagged output too.
        ctx.Resources.EmbedStandardFonts = _document.Settings.Accessibility == AccessibilityMode.PdfUa1;

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = [];

        if (RequiresTwoPassLayout())
        {
            int expectedTotalPages = resolvedTocEntries!.Count > 0 ? resolvedTocEntries.Values.Max() + 1 : 0;
            // Pre-allocate page IDs with a generous padding
            for (int i = 0; i < Math.Max(expectedTotalPages + 50, 100); i++)
            {
                pageIds.Add(writer.GetNewId());
            }
        }

        // annotation bindings for /StructParent + /OBJR wiring.
        // Keys start well above the page count so the /ParentTree number
        // tree has no collision between page keys (0..N-1) and annotation keys.
        const int AnnotStructParentBase = 10_000;
        var annotBindings = new List<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>();
        int globalAnnotIndex = 0;

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
                ["/Contents"] = $"{contentId} 0 R",
                // PDF/A-2b clause 6.2.2.2 / ISO 32000-1 §7.8.3: every
                // page whose content stream references external objects
                // (fonts, images, XObjects) must have an explicitly
                // associated Resources dict. Inheritance from /Pages is
                // valid per §7.7.3.4 but veraPDF profile 2b rejects it.
                ["/Resources"] = $"{resourcesId} 0 R"
            };

            // Tagged PDF: each page gets /StructParents equal to its
            // page index. The /ParentTree (emitted at the end by
            // StructureTreeWriter) uses the same key, so the reverse
            // lookup MCID→StructElem works.
            if (taggingContext != null)
            {
                pageDict["/StructParents"] = (pageIndex - 1).ToString();
                pageDict["/Tabs"] = "/S"; // follow structure order for tab navigation
            }

            // Write link annotations if present
            if (pageOutput.Hyperlinks.Count > 0)
            {
                List<int> annotIds = new();
                foreach (Hyperlink link in pageOutput.Hyperlinks)
                {
                    int annotId = writer.GetNewId();
                    int? structParentKey = null;
                    if (taggingContext != null)
                    {
                        structParentKey = AnnotStructParentBase + globalAnnotIndex++;
                    }
                    writer.WriteLinkAnnotation(annotId, link, pageIds, structParentKey);
                    annotIds.Add(annotId);
                    if (taggingContext != null && link.LinkedStructElement != null && structParentKey.HasValue)
                    {
                        link.LinkedStructElement.ObjrAnnotations.Add((annotId, pageId));
                        annotBindings.Add((link.LinkedStructElement, annotId, pageId, structParentKey.Value));
                    }
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

        // ── Honest PDF/A claim ──────────────────────────────────
        // Settings.PdfAEnabled is opt-in, but PDF/A-1b §6.3.4 forbids
        // non-embedded fonts. When the caller asked for PDF/A but the
        // document references any of the standard 14 (which we don't
        // embed), we silently drop the claim: no pdfaid block in XMP,
        // no OutputIntent. The document is still a valid PDF - it just
        // doesn't lie about being PDF/A-1b. To actually deliver PDF/A
        // the caller must register a real TTF.
        //
        // Implemented by demoting Settings.PdfAEnabled to the effective
        // value (rather than threading a separate flag through the
        // remaining writes); mirrors the DisplayDocTitle = true
        // mutation we do for PDF/UA-1.
        if (_document.Settings.PdfAEnabled && ctx.Resources.HasNonEmbeddedStandardFont())
        {
            _document.Settings.PdfAEnabled = false;
        }

        // 5. Write the global Resources object that every page references.
        // Written as an indirect object so individual pages can satisfy
        // PDF/A-2b clause 6.2.2.2 (see page-dict comment above) without
        // duplicating the resources dict per page.
        writer.WriteRawObject(resourcesId, globalResources);

        // 6. Write Parent Pages Object. /Resources is intentionally
        // omitted - the per-page indirect references are authoritative.
        string kids = string.Join(' ', pageIds.Select(id => $"{id} 0 R"));
        writer.WriteObject(pagesId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString()
        });

        // 6. Write Metadata & Close
        MetadataWriter metadataWriter = new(writer);
        // ISO 19005-4 clauses 6.1.3-4/5 prohibit the legacy trailer /Info
        // dictionary (except for a narrow PieceInfo/ModDate case). PDF/A-4
        // metadata lives in XMP, so omit /Info entirely for the effective claim.
        if (_document.Settings.Conformance == PdfAConformance.PdfA4)
        {
            documentInfoId = 0;
        }
        else
        {
            metadataWriter.WriteDocumentInfo(infoId, _document.Metadata);
        }

        CatalogOptions catalogOptions = new();

        // PDF/UA-1 mandates /DisplayDocTitle true (Matterhorn Protocol §7-1).
        if (taggingContext != null) _document.Settings.DisplayDocTitle = true;
        if (HasViewerPreferences(_document.Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            writer.WriteViewerPreferences(catalogOptions.ViewerPrefsId, _document.Settings);
        }

        bool effectivePdfA = _document.Settings.PdfAEnabled && !ctx.Resources.HasNonEmbeddedStandardFont();

        // Write XMP Metadata for PDF 1.7 and 2.0
        if (_document.Settings.Version == PdfVersion.Pdf17 || _document.Settings.Version == PdfVersion.Pdf20)
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

        // Write PDF/A Output Intents (PDF/A-1/2/3 on PDF 1.7, PDF/A-4 on PDF 2.0)
        if (effectivePdfA && (_document.Settings.Version == PdfVersion.Pdf17 || _document.Settings.Version == PdfVersion.Pdf20))
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
        // population. this feature threads annotation bindings for /OBJR.
        if (taggingContext != null)
        {
            StructureTreeWriter structureTreeWriter = new(writer);
            catalogOptions.StructTreeRootId = structureTreeWriter.Write(taggingContext.Tree, pageIds, annotBindings);
            catalogOptions.Tagged = true;
            catalogOptions.Language = _document.Settings.Language;
        }

        // Write /PageLabels number tree if configured (PDF §12.4.2).
        if (_document.Settings.PageLabel is { } syncPageLabel)
        {
            int pageLabelsId = writer.GetNewId();
            writer.WriteObject(pageLabelsId, new Dictionary<string, string>
            {
                ["/Nums"] = $"[ 0 << {BuildPageLabelEntry(syncPageLabel)} >> ]"
            });
            catalogOptions.PageLabelsId = pageLabelsId;
        }

        // Write document attachments (embedded files) subject to the
        // conformance guard. Populates /Names /EmbeddedFiles and /AF.
        IList<FileAttachment>? attachmentsToWrite = ResolveAttachments();
        if (attachmentsToWrite != null)
        {
            AttachmentWriter attachmentWriter = new(writer);
            AttachmentWriter.Result attachmentResult = attachmentWriter.WriteAttachments(attachmentsToWrite);
            catalogOptions.NamesEmbeddedFilesId = attachmentResult.NamesEmbeddedFilesId;
            catalogOptions.AssociatedFileIds = attachmentResult.AssociatedFileIds;
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
        using IDisposable fontScope = FontMetrics.BeginDocumentScope(UsesEmbeddedStandardFonts());

        // 0. Set up encryption if configured
        int encryptId = 0;
        if (_document.Settings.IsEncrypted)
        {
            writer.Encryptor = new Security.Encryption.PdfEncryptor(_document.Settings.Encryption!);
            encryptId = writer.GetNewId();
        }

        // 1. Allocate IDs
        int catalogId = writer.GetNewId();
        int infoId = writer.GetNewId();
        int pagesId = writer.GetNewId(); // Parent object (written at the end)
        int documentInfoId = infoId;
        // See sync path for the PDF/A-2b clause 6.2.2.2 rationale.
        int resourcesId = writer.GetNewId();

        // 2. Initialize Shared Context
        PdfSize orientedPageSize = _document.Settings.GetOrientedPageSize();

        IDictionary<string, int>? resolvedTocEntries = null;

        if (RequiresTwoPassLayout())
        {
            RenderContext dryRunCtx = new(orientedPageSize, _document.Settings.Margin)
            {
                Conformance = _document.Settings.Conformance,
                TocEntries = new Dictionary<string, int>(),
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

        // PDF/UA tagging context - shared between the render pipeline and
        // the async structure-tree writer below.
        TaggingContext? taggingContext = CreateTaggingContext();

        RenderContext ctx = new(orientedPageSize, _document.Settings.Margin);
        ctx.Conformance = _document.Settings.Conformance;
        // PDF/UA-1 mandates font embedding (ISO 14289-1, veraPDF 7.21.4.1)
        // independently of any PDF/A claim - substitute embedded Liberation for
        // the standard-14 references in tagged output too.
        ctx.Resources.EmbedStandardFonts = _document.Settings.Accessibility == AccessibilityMode.PdfUa1;
        ctx.TocEntries = resolvedTocEntries ?? new Dictionary<string, int>();
        ctx.Tagging = taggingContext;
        ctx.Resources.Diagnostics = _diagnostics;

        // 3. Stream Processing (Low Memory)
        List<int> pageIds = new();

        if (RequiresTwoPassLayout())
        {
            int expectedTotalPages = resolvedTocEntries!.Count > 0 ? resolvedTocEntries.Values.Max() + 1 : 0;
            // Pre-allocate page IDs with a generous padding
            for (int i = 0; i < Math.Max(expectedTotalPages + 50, 100); i++)
            {
                pageIds.Add(writer.GetNewId());
            }
        }

        // annotation bindings for /StructParent + /OBJR wiring.
        const int AnnotStructParentBase = 10_000;
        var annotBindings = new List<(StructureElement structElem, int annotId, int pageObjectId, int structParentKey)>();
        int globalAnnotIndex = 0;

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
                ["/Contents"] = $"{contentId} 0 R",
                // PDF/A-2b clause 6.2.2.2 / ISO 32000-1 §7.8.3: every
                // page whose content stream references external objects
                // (fonts, images, XObjects) must have an explicitly
                // associated Resources dict. Inheritance from /Pages is
                // valid per §7.7.3.4 but veraPDF profile 2b rejects it.
                ["/Resources"] = $"{resourcesId} 0 R"
            };

            // Tagged PDF: /StructParents keyed by 0-based page index.
            if (taggingContext != null)
            {
                pageDict["/StructParents"] = (pageIndex - 1).ToString();
                pageDict["/Tabs"] = "/S"; // follow structure order for tab navigation
            }

            // Write link annotations if present
            if (pageOutput.Hyperlinks.Count > 0)
            {
                List<int> annotIds = new();
                foreach (Hyperlink link in pageOutput.Hyperlinks)
                {
                    int annotId = writer.GetNewId();
                    int? structParentKey = null;
                    if (taggingContext != null)
                    {
                        structParentKey = AnnotStructParentBase + globalAnnotIndex++;
                    }
                    await writer.WriteLinkAnnotationAsync(annotId, link, pageIds, structParentKey, cancellationToken).ConfigureAwait(false);
                    annotIds.Add(annotId);
                    if (taggingContext != null && link.LinkedStructElement != null && structParentKey.HasValue)
                    {
                        link.LinkedStructElement.ObjrAnnotations.Add((annotId, pageId));
                        annotBindings.Add((link.LinkedStructElement, annotId, pageId, structParentKey.Value));
                    }
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

        // drop the PDF/A claim if any standard-14 font slipped
        // through unembedded - see the sync path for the rationale.
        if (_document.Settings.PdfAEnabled && ctx.Resources.HasNonEmbeddedStandardFont())
        {
            _document.Settings.PdfAEnabled = false;
        }

        // 5. Write the global Resources object (see sync path for rationale).
        await writer.WriteRawObjectAsync(resourcesId, globalResources, cancellationToken).ConfigureAwait(false);

        // 6. Write Parent Pages Object - /Resources omitted; pages own it.
        string kids = string.Join(' ', pageIds.Select(id => $"{id} 0 R"));
        await writer.WriteObjectAsync(pagesId, new Dictionary<string, string>
(StringComparer.OrdinalIgnoreCase)
        {
            ["/Type"] = "/Pages",
            ["/Kids"] = $"[ {kids} ]",
            ["/Count"] = pageIds.Count.ToString()
        }, null, cancellationToken).ConfigureAwait(false);

        // 6. Write Metadata & Close
        MetadataWriter metadataWriter = new(writer);
        // PDF/A-4 metadata is carried by XMP; see the sync path for the
        // ISO 19005-4 /Info restriction.
        if (_document.Settings.Conformance == PdfAConformance.PdfA4)
        {
            documentInfoId = 0;
        }
        else
        {
            await metadataWriter.WriteDocumentInfoAsync(infoId, _document.Metadata, cancellationToken).ConfigureAwait(false);
        }

        CatalogOptions catalogOptions = new();

        // PDF/UA-1 mandates /DisplayDocTitle true (Matterhorn Protocol §7-1).
        if (taggingContext != null) _document.Settings.DisplayDocTitle = true;
        if (HasViewerPreferences(_document.Settings))
        {
            catalogOptions.ViewerPrefsId = writer.GetNewId();
            await writer.WriteViewerPreferencesAsync(catalogOptions.ViewerPrefsId, _document.Settings, cancellationToken).ConfigureAwait(false);
        }

        bool effectivePdfA = _document.Settings.PdfAEnabled && !ctx.Resources.HasNonEmbeddedStandardFont();

        // Write XMP Metadata for PDF 1.7 and 2.0
        if (_document.Settings.Version == PdfVersion.Pdf17 || _document.Settings.Version == PdfVersion.Pdf20)
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

        // Write PDF/A Output Intents (PDF/A-1/2/3 on PDF 1.7, PDF/A-4 on PDF 2.0)
        if (effectivePdfA && (_document.Settings.Version == PdfVersion.Pdf17 || _document.Settings.Version == PdfVersion.Pdf20))
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
        // canvas populated during render. this feature threads annotation bindings
        // for /OBJR wiring.
        if (taggingContext != null)
        {
            StructureTreeWriter structureTreeWriter = new(writer);
            catalogOptions.StructTreeRootId = await structureTreeWriter.WriteAsync(taggingContext.Tree, pageIds, annotBindings, cancellationToken)
                .ConfigureAwait(false);
            catalogOptions.Tagged = true;
            catalogOptions.Language = _document.Settings.Language;
        }

        // Write /PageLabels number tree if configured (PDF §12.4.2).
        if (_document.Settings.PageLabel is { } asyncPageLabel)
        {
            int pageLabelsId = writer.GetNewId();
            await writer.WriteObjectAsync(pageLabelsId, new Dictionary<string, string>
            {
                ["/Nums"] = $"[ 0 << {BuildPageLabelEntry(asyncPageLabel)} >> ]"
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
            catalogOptions.PageLabelsId = pageLabelsId;
        }

        // Write document attachments (embedded files) subject to the
        // conformance guard. Populates /Names /EmbeddedFiles and /AF.
        IList<FileAttachment>? attachmentsToWrite = ResolveAttachments();
        if (attachmentsToWrite != null)
        {
            AttachmentWriter attachmentWriter = new(writer);
            AttachmentWriter.Result attachmentResult = await attachmentWriter
                .WriteAttachmentsAsync(attachmentsToWrite, cancellationToken).ConfigureAwait(false);
            catalogOptions.NamesEmbeddedFilesId = attachmentResult.NamesEmbeddedFilesId;
            catalogOptions.AssociatedFileIds = attachmentResult.AssociatedFileIds;
        }

        // Write Catalog
        await writer.WriteCatalogAsync(catalogId, pagesId, pageIds[0], _document.Settings, catalogOptions, cancellationToken)
            .ConfigureAwait(false);

        // 7. Write encryption dictionary if encrypted
        if (encryptId > 0) await writer.WriteEncryptDictionaryAsync(encryptId, cancellationToken).ConfigureAwait(false);

        await writer.CloseAsync(catalogId, documentInfoId, cancellationToken).ConfigureAwait(false);
    }

    private bool UsesEmbeddedStandardFonts() =>
        _document.Settings.Conformance != PdfAConformance.None ||
        _document.Settings.Accessibility == AccessibilityMode.PdfUa1;

    /// <summary>
    /// Applies the embedded-file conformance guard (<see cref="AttachmentRules"/>)
    /// and returns the attachments to write, or <c>null</c> when there is
    /// nothing to embed. Under a PDF/A level that forbids embedded files the
    /// attachments are dropped and a warning diagnostic recorded, keeping the
    /// emitted PDF's conformance claim honest.
    /// </summary>
    private IList<FileAttachment>? ResolveAttachments()
    {
        IList<FileAttachment> attachments = _document.Attachments;
        if (attachments == null || attachments.Count == 0)
            return null;

        if (!AttachmentRules.AllowsEmbeddedFiles(_document.Settings.Conformance))
        {
            _diagnostics.AddWarning(DiagnosticCategory.Attachment,
                $"{_document.Settings.Conformance} does not permit embedded files; " +
                $"{attachments.Count} attachment(s) were dropped. Use PdfAConformance.PdfA3b " +
                "for e-invoicing (ZUGFeRD / Factur-X).");
            return null;
        }

        return attachments;
    }

    /// <summary>
    /// Builds the inner PDF dictionary string for a single <see cref="PageLabel"/> entry
    /// in the /PageLabels number tree (PDF §12.4.2).
    /// Returns the raw PDF tokens between &lt;&lt; and &gt;&gt;, e.g. <c>/S /D /St 42</c>.
    /// </summary>
    private static string BuildPageLabelEntry(PageLabel label)
    {
        string s = label.Style switch
        {
            PageLabelStyle.LowercaseRoman => "/r",
            PageLabelStyle.UppercaseRoman => "/R",
            PageLabelStyle.LowercaseAlpha => "/a",
            PageLabelStyle.UppercaseAlpha => "/A",
            _ => "/D",
        };
        List<string> parts = [$"/S {s}"];
        if (!string.IsNullOrEmpty(label.Prefix)) parts.Add($"/P ({label.Prefix})");
        if (label.StartAt != 1) parts.Add($"/St {label.StartAt}");
        return string.Join(' ', parts);
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
    private TaggingContext? CreateTaggingContext()
    {
        if (_document.Settings.Accessibility == AccessibilityMode.None) return null;
        if (_document.Settings.Version != PdfVersion.Pdf17) return null;
        return new TaggingContext(new StructureTreeBuilder());
    }

    /// <summary>
    /// Returns <c>true</c> if the document contains a <see cref="VisualToc"/> element,
    /// which requires a two-pass layout to resolve heading page numbers before rendering.
    /// Delegates to <see cref="TocLayout.RequiresTwoPass"/> so the SVG export path
    /// makes the identical decision.
    /// </summary>
    private bool RequiresTwoPassLayout() => TocLayout.RequiresTwoPass(_document);
}
