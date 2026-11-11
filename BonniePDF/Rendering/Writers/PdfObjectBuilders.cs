using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Security.Encryption;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Builds the dictionaries and trailer text that <see cref="PdfWriter"/> and
/// <see cref="AsyncPdfWriter"/> both write, so the two writers differ only in how they perform I/O.
/// </summary>
internal static class PdfObjectBuilders
{
    /// <summary>Builds a link annotation dictionary for a hyperlink.</summary>
    internal static Dictionary<string, string> LinkAnnotation(Hyperlink hyperlink, IList<int>? pageIds, int? structParentKey)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/Annot",
            [PdfKeys.Common.Subtype] = "/Link",
            [PdfKeys.Annotation.Rectangle] = hyperlink.GetRectangleString(),
            [PdfKeys.Annotation.Flags] = "4", // Print flag (PDF/A-1B requires annotations to be printable)
            // No border: the default is [0 0 1], which many viewers draw as a box
            // around the link. A link's look belongs to its text (spec 11 §2).
            [PdfKeys.Annotation.Border] = "[0 0 0]",
            // PDF/UA rule 7-18: /Contents on the annotation must
            // carry an alt-text that mirrors the visible link text. Used
            // by screen readers when the structure tree omits or can't
            // resolve the link's text. Emitted unconditionally - even
            // untagged docs benefit, and the field is a no-op for
            // viewers that don't surface annotation alt-text.
            [PdfKeys.Annotation.Contents] = $"({PdfSyntax.Escape(hyperlink.Text ?? string.Empty)})",
        };

        // /StructParent wires this annotation into the parent tree
        // for reverse MCID→StructElem lookup by accessible viewers.
        if (structParentKey.HasValue)
            dict[PdfKeys.Annotation.StructureParent] = structParentKey.Value.ToString();

        if (hyperlink.TargetPageIndex.HasValue && pageIds != null)
        {
            int pageIdx = Math.Max(0, Math.Min(hyperlink.TargetPageIndex.Value, pageIds.Count - 1));
            dict[PdfKeys.Annotation.Action] = $"<< /S /GoTo /D [{pageIds[pageIdx]} 0 R /Fit] >>";
        }
        else
        {
            dict[PdfKeys.Annotation.Action] = $"<< /S /URI /URI ({PdfSyntax.Escape(PdfSyntax.EncodeUri(hyperlink.Uri ?? ""))}) >>";
        }

        return dict;
    }

    /// <summary>Builds the viewer preferences dictionary, or null when no preference is set.</summary>
    internal static Dictionary<string, string>? ViewerPreferences(DocumentSettings settings)
    {
        Dictionary<string, string> dict = new();

        if (settings.HideToolbar)
            dict[PdfKeys.ViewerPreferences.HideToolbar] = "true";

        if (settings.HideMenubar)
            dict[PdfKeys.ViewerPreferences.HideMenubar] = "true";

        if (settings.HideWindowUI)
            dict[PdfKeys.ViewerPreferences.HideWindowUi] = "true";

        if (settings.FitWindow)
            dict[PdfKeys.ViewerPreferences.FitWindow] = "true";

        if (settings.CenterWindow)
            dict[PdfKeys.ViewerPreferences.CenterWindow] = "true";

        if (settings.DisplayDocTitle)
            dict[PdfKeys.ViewerPreferences.DisplayDocumentTitle] = "true";

        // Only write if there are actual preferences
        return dict.Count == 0 ? null : dict;
    }

    /// <summary>Builds the catalog dictionary with OpenAction, PageMode, ViewerPreferences and the optional catalog references.</summary>
    internal static Dictionary<string, string> Catalog(int pagesId, int firstPageId, DocumentSettings settings, CatalogOptions? options)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/Catalog",
            [PdfKeys.Catalog.Pages] = $"{pagesId} 0 R"
        };

        // Add OpenAction
        string openAction = GetOpenActionString(firstPageId, settings);
        dict[PdfKeys.Catalog.OpenAction] = openAction;

        // Add PageMode - auto-set to UseOutlines when outlines are present
        if (options?.OutlinesId > 0)
            dict[PdfKeys.Catalog.PageMode] = "/UseOutlines";
        else if (settings.PageMode != PageMode.UseNone)
            dict[PdfKeys.Catalog.PageMode] = $"/{GetPageModeString(settings.PageMode)}";

        // Add ViewerPreferences if provided
        if (options?.ViewerPrefsId > 0) dict[PdfKeys.Catalog.ViewerPreferences] = $"{options.ViewerPrefsId} 0 R";

        // Add Outlines if provided
        if (options?.OutlinesId > 0) dict[PdfKeys.Catalog.Outlines] = $"{options.OutlinesId} 0 R";

        // Add XMP Metadata if provided
        if (options?.XmpMetadataId > 0) dict[PdfKeys.Catalog.Metadata] = $"{options.XmpMetadataId} 0 R";

        // Add OutputIntents if provided (PDF/A)
        if (options?.OutputIntentIds is { Count: > 0 })
        {
            string refs = string.Join(' ', options.OutputIntentIds.ToList().ConvertAll(oid => $"{oid} 0 R"));
            dict[PdfKeys.Catalog.OutputIntents] = $"[ {refs} ]";
        }

        // Add MarkInfo when either PDF/A or tagging is on. Both standards
        // require /MarkInfo /Marked true; they're orthogonal flags so emitting
        // it once when either is set covers both cases.
        if (options?.PdfAEnabled == true || options?.Tagged == true)
            dict[PdfKeys.Catalog.MarkInfo] = "<< /Marked true >>";

        // Add StructTreeRoot when tagging is on.
        if (options?.StructTreeRootId > 0)
            dict[PdfKeys.Catalog.StructureTreeRoot] = $"{options.StructTreeRootId} 0 R";

        // Add /Lang when the document declares a natural language. Required
        // by PDF/UA-1; useful generally for assistive tech.
        if (!string.IsNullOrEmpty(options?.Language))
            dict[PdfKeys.Catalog.Language] = $"({PdfSyntax.Escape(options.Language)})";

        // Add Dests if provided
        if (options?.DestsId > 0)
            dict[PdfKeys.Catalog.Destinations] = $"{options.DestsId} 0 R";

        // Add PageLabels if configured
        if (options?.PageLabelsId > 0)
            dict[PdfKeys.Catalog.PageLabels] = $"{options.PageLabelsId} 0 R";

        // Add /Names /EmbeddedFiles and /AF for document attachments (PDF/A-3).
        if (options?.NamesEmbeddedFilesId > 0)
            dict[PdfKeys.Catalog.Names] = $"<< /EmbeddedFiles {options.NamesEmbeddedFilesId} 0 R >>";
        if (options?.AssociatedFileIds is { Count: > 0 })
        {
            string afRefs = string.Join(' ', options.AssociatedFileIds.ToList().ConvertAll(fid => $"{fid} 0 R"));
            dict[PdfKeys.Catalog.AssociatedFiles] = $"[ {afRefs} ]";
        }

        return dict;
    }

    /// <summary>Builds the minimal catalog dictionary for the legacy string-typed zoom overload.</summary>
    internal static Dictionary<string, string> CatalogWithOpenAction(int pagesId, int firstPageId, string zoomType, double zoomLevel)
    {
        string openAction;

        switch (zoomType.ToUpperInvariant())
        {
            case "FIT":
                // Fit the page to the window
                openAction = $"[ {firstPageId} 0 R /Fit ]";
                break;
            case "FITH":
                // Fit horizontally, top of page
                openAction = $"[ {firstPageId} 0 R /FitH 0 ]";
                break;
            case "FITW":
                // Fit vertically, left of page
                openAction = $"[ {firstPageId} 0 R /FitV 0 ]";
                break;
            case "XYZ":
                // Custom zoom level (1.0 = 100%)
                openAction = $"[ {firstPageId} 0 R /XYZ 0 {zoomLevel} null ]";
                break;
            default:
                openAction = $"[ {firstPageId} 0 R /Fit ]";
                break;
        }

        return new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Catalog",
            [PdfKeys.Catalog.Pages] = $"{pagesId} 0 R",
            [PdfKeys.Catalog.OpenAction] = openAction
        };
    }

    /// <summary>Builds the trailer dictionary text, including its trailing newline.</summary>
    internal static string Trailer(int objectCount, int rootId, int infoId, int encryptId, PdfEncryptor? encryptor)
    {
        List<string> trailerParts = new()
        {
            $"/Size {objectCount + 1}",
            $"/Root {rootId} 0 R"
        };

        if (infoId > 0) trailerParts.Add($"/Info {infoId} 0 R");

        // Add encryption entries if encrypted
        if (encryptor != null && encryptId > 0)
        {
            trailerParts.Add($"/Encrypt {encryptId} 0 R");
            trailerParts.Add($"/ID {encryptor.GetIdArray()}");
        }
        else
        {
            // Generate a random file ID if not encrypted (required for PDF/A)
            // The ID is an array of two byte strings. The first is a permanent identifier
            // based on the contents of the file at the time it was originally created.
            // The second is a changing identifier based on the file's contents at the time it was last updated.
            // For a new file, they should be the same.
            byte[] idBytes = Guid.NewGuid().ToByteArray();
            string hexId = Convert.ToHexString(idBytes);
            trailerParts.Add($"/ID [<{hexId}> <{hexId}>]");
        }

        return $"<< {string.Join(' ', trailerParts)} >>\n";
    }

    /// <summary>
    /// Gets the OpenAction string for the specified zoom level
    /// </summary>
    private static string GetOpenActionString(int firstPageId, DocumentSettings settings)
    {
        return settings.DefaultZoom switch
        {
            ZoomLevel.FitPage => $"[ {firstPageId} 0 R /Fit ]",
            ZoomLevel.FitWidth => $"[ {firstPageId} 0 R /FitH 0 ]",
            ZoomLevel.FitHeight => $"[ {firstPageId} 0 R /FitV 0 ]",
            ZoomLevel.Custom => $"[ {firstPageId} 0 R /XYZ 0 {settings.CustomZoomLevel} null ]",
            _ => $"[ {firstPageId} 0 R /Fit ]"
        };
    }

    /// <summary>
    /// Gets the PageMode string for PDF output
    /// </summary>
    private static string GetPageModeString(PageMode pageMode)
    {
        return pageMode switch
        {
            PageMode.UseOutlines => "UseOutlines",
            PageMode.UseThumbs => "UseThumbs",
            PageMode.FullScreen => "FullScreen",
            PageMode.UseOC => "UseOC",
            PageMode.UseAttachments => "UseAttachments",
            _ => "UseNone"
        };
    }
}
