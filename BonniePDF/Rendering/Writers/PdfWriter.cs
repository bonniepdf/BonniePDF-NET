using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Security.Encryption;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// A PDF writer that emits PDF 1.4, 1.7, or 2.0 files (selected via
/// <see cref="PdfVersion"/>). Manages object writing, offset tracking, and
/// the cross-reference table. Supports optional encryption of stream contents.
/// </summary>
internal class PdfWriter : IPdfWriter, IDisposable
{
    // ISO-8859-1 (Latin-1) encoding for PDF streams
    // This matches WinAnsiEncoding used by standard PDF fonts and ensures
    // characters like £ (U+00A3) are encoded as single byte 0xA3 instead of
    // UTF-8's two-byte sequence (0xC2 0xA3) which would display as "Â£"
    private static readonly Encoding Iso88591 = Encoding.GetEncoding("ISO-8859-1");
    private readonly FileStream? _fs;
    private readonly bool _leaveOpen;

    // Maps object ID to file position for the xref table
    private readonly Dictionary<int, long> _offsets = [];
    private readonly Stream _stream;
    private readonly StreamWriter _writer;
    private int _currentObjId;
    private int _encryptId;

    private string EncryptObjectStrings(int id, string fragment) =>
        Encryptor == null || id == _encryptId
            ? fragment
            : PdfStringEncryption.EncryptStrings(fragment, bytes => Encryptor.EncryptData(bytes, id));

    /// <summary>
    /// Creates a new PDF writer that writes to a file
    /// </summary>
    /// <param name="filename">The path to the file to create</param>
    /// <param name="version">The PDF version to emit</param>
    public PdfWriter(string filename, PdfVersion version = PdfVersion.Pdf14)
    {
        _fs = new FileStream(filename, FileMode.Create);
        _stream = _fs;
        _leaveOpen = false;
        _writer = new StreamWriter(_stream, Iso88591) { NewLine = "\n" };
        _writer.WriteLine(version == PdfVersion.Pdf20 ? "%PDF-2.0" : version == PdfVersion.Pdf17 ? "%PDF-1.7" : "%PDF-1.4");
        _writer.WriteLine("%\u00E2\u00E3\u00CF\u00D3");
    }

    /// <summary>
    /// Creates a new PDF writer that writes to a stream
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="leaveOpen">If true, the stream will not be disposed when this writer is disposed</param>
    /// <param name="version">The PDF version to emit</param>
    public PdfWriter(Stream stream, bool leaveOpen = false, PdfVersion version = PdfVersion.Pdf14)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable", nameof(stream));

        _fs = null;
        _stream = stream;
        _leaveOpen = leaveOpen;
        _writer = new StreamWriter(_stream, Iso88591, 1024, leaveOpen) { NewLine = "\n" };
        _writer.WriteLine(version == PdfVersion.Pdf20 ? "%PDF-2.0" : version == PdfVersion.Pdf17 ? "%PDF-1.7" : "%PDF-1.4");
        _writer.WriteLine("%\u00E2\u00E3\u00CF\u00D3");
    }

    /// <summary>
    /// Disposes the underlying stream writer and, unless <c>leaveOpen</c> was set, the stream and file handle.
    /// </summary>
    public void Dispose()
    {
        _writer.Dispose();
        if (!_leaveOpen)
        {
            _stream.Dispose();
            _fs?.Dispose();
        }
    }

    /// <summary>
    /// Gets or sets the encryptor for this writer. When set, streams are encrypted.
    /// </summary>
    public PdfEncryptor? Encryptor { get; set; }

    /// <summary>
    /// Gets a new unique object ID for the PDF
    /// </summary>
    public int GetNewId()
    {
        return ++_currentObjId;
    }

    /// <summary>
    /// Writes a PDF object with the specified ID and dictionary
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="streamContent">Optional stream content for the object</param>
    public void WriteObject(int id, IDictionary<string, string> dict, string? streamContent = null)
    {
        _writer.Flush();
        _offsets[id] = _stream.Position;

        _writer.WriteLine($"{id} 0 obj");
        _writer.WriteLine("<<");
        foreach ((string key, string value) in dict) _writer.WriteLine($"  {key} {EncryptObjectStrings(id, value)}");

        if (streamContent != null)
        {
            byte[] contentBytes = Iso88591.GetBytes(streamContent);

            // Encrypt if encryptor is set
            if (Encryptor != null)
                contentBytes = Encryptor.EncryptData(contentBytes, id);

            _writer.WriteLine($"  /Length {contentBytes.Length}");
            _writer.WriteLine(">>");
            _writer.WriteLine("stream");
            _writer.Flush();
            _stream.Write(contentBytes, 0, contentBytes.Length);
            _writer.Write("\nendstream\n");
        }
        else
        {
            _writer.WriteLine(">>");
        }

        _writer.WriteLine("endobj");
    }

    /// <summary>
    /// Writes a PDF object whose body is already a pre-formatted PDF
    /// fragment (e.g. the global Resources dict composed by
    /// <c>PdfResourceRegistry.WriteResources</c>).
    /// </summary>
    public void WriteRawObject(int id, string body)
    {
        _writer.Flush();
        _offsets[id] = _stream.Position;

        _writer.WriteLine($"{id} 0 obj");
        _writer.WriteLine(EncryptObjectStrings(id, body));
        _writer.WriteLine("endobj");
    }

    /// <summary>
    /// Compresses the string content using ZLib (FlateDecode) and writes it as a binary object.
    /// This drastically reduces PDF file size.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="content">String content to compress and write</param>
    public void WriteCompressedObject(int id, IDictionary<string, string> dict, string content)
    {
        if (!InternalConfiguration.EnableCompression)
        {
            WriteObject(id, dict, content);
            return;
        }

        // 1. Convert content to ISO-8859-1 bytes (matches WinAnsiEncoding used by standard PDF fonts)
        byte[] rawBytes = Iso88591.GetBytes(content);

        // 2. Compress using ZLibStream to ensure correct ZLIB headers (0x78 0x9C)
        using MemoryStream memoryStream = new();
        using (ZLibStream zlib = new(memoryStream, CompressionLevel.SmallestSize))
        {
            zlib.Write(rawBytes, 0, rawBytes.Length);
        }

        byte[] compressedBytes = memoryStream.ToArray();

        // 3. Inject Filter into dictionary
        dict[PdfKeys.Stream.Filter] = "/FlateDecode";

        // 4. Delegate to existing binary writer
        WriteBinaryObject(id, dict, compressedBytes);
    }

    /// <summary>
    /// Writes a link annotation object for a hyperlink
    /// </summary>
    /// <param name="id">The object ID for the annotation</param>
    /// <param name="hyperlink">The hyperlink to create an annotation for</param>
    /// <returns>The object ID of the annotation</returns>
    public int WriteLinkAnnotation(int id, Hyperlink hyperlink, IList<int>? pageIds = null, int? structParentKey = null)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/Annot",
            [PdfKeys.Common.Subtype] = "/Link",
            [PdfKeys.Annotation.Rectangle] = hyperlink.GetRectangleString(),
            [PdfKeys.Annotation.Flags] = "4", // Print flag (PDF/A-1B requires annotations to be printable)
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
            dict[PdfKeys.Annotation.Action] = $"<< /S /URI /URI ({PdfSyntax.Escape(hyperlink.Uri ?? "")}) >>";
        }

        WriteObject(id, dict);
        return id;
    }

    /// <summary>
    /// Writes the encryption dictionary object
    /// </summary>
    /// <param name="id">The object ID for the encryption dictionary</param>
    /// <returns>The object ID of the encryption dictionary</returns>
    public int WriteEncryptDictionary(int id)
    {
        if (Encryptor == null)
            return 0;

        _encryptId = id;
        IDictionary<string, string> dict = Encryptor.GetEncryptDictionary();
        WriteObject(id, dict);
        return id;
    }

    /// <summary>
    /// Finalizes the PDF by writing the xref table and trailer
    /// </summary>
    /// <param name="rootId">The object ID of the catalog/root object</param>
    /// <param name="infoId">The object ID of the document info dictionary (optional, 0 if none)</param>
    public void Close(int rootId, int infoId = 0)
    {
        _writer.Flush();
        long xrefPos = _stream.Position;

        _writer.Write("xref\n");
        // The +1 accounts for the special "zero" entry
        _writer.Write($"0 {_currentObjId + 1}\n");
        _writer.Write("0000000000 65535 f \n");

        // Loop through IDs 1..N and look up their position
        for (int i = 1; i <= _currentObjId; i++)
            if (_offsets.TryGetValue(i, out long offset))
                _writer.Write($"{offset:D10} 00000 n \n");
            else
                // Write a free entry for IDs that were reserved but not written
                // (e.g. pre-allocated page IDs that weren't needed). Valid per PDF spec.
                _writer.Write("0000000000 00000 f \n");

        _writer.Write("trailer\n");
        // Build trailer dictionary
        List<string> trailerParts = new()
        {
            $"/Size {_currentObjId + 1}",
            $"/Root {rootId} 0 R"
        };

        if (infoId > 0) trailerParts.Add($"/Info {infoId} 0 R");

        // Add encryption entries if encrypted
        if (Encryptor != null && _encryptId > 0)
        {
            trailerParts.Add($"/Encrypt {_encryptId} 0 R");
            trailerParts.Add($"/ID {Encryptor.GetIdArray()}");
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

        string trailerDict = $"<< {string.Join(' ', trailerParts)} >>\n";
        _writer.Write(trailerDict);
        _writer.Write("startxref\n");
        _writer.Write($"{xrefPos}\n");
        _writer.Write("%%EOF");
    }

    /// <summary>
    /// Writes a viewer preferences dictionary
    /// </summary>
    /// <param name="id">The object ID for the viewer preferences</param>
    /// <param name="settings">The document settings containing viewer preferences</param>
    /// <returns>The object ID of the viewer preferences, or 0 if no preferences are set</returns>
    public int WriteViewerPreferences(int id, DocumentSettings settings)
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
        if (dict.Count == 0)
            return 0;

        WriteObject(id, dict);
        return id;
    }

    /// <summary>
    /// Writes a PDF catalog object with full support for OpenAction, PageMode, and ViewerPreferences
    /// </summary>
    /// <param name="id">The catalog object ID</param>
    /// <param name="pagesId">The pages object ID</param>
    /// <param name="firstPageId">The first page object ID (for OpenAction)</param>
    /// <param name="settings">The document settings</param>
    /// <param name="options">Optional catalog references such as outlines, XMP metadata, and PDF/A output intents.</param>
    public void WriteCatalog(int id, int pagesId, int firstPageId, DocumentSettings settings, CatalogOptions? options)
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
            dict[PdfKeys.Catalog.Language] = $"({BonniePDF.Rendering.Pdf.PdfSyntax.Escape(options.Language)})";

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

        WriteObject(id, dict);
    }

    /// <summary>
    /// Writes an uncompressed binary stream object, bypassing encryption.
    /// Used for XMP metadata when metadata encryption is explicitly disabled.
    /// </summary>
    public void WriteUnencryptedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent)
    {
        _writer.Flush();
        _offsets[id] = _stream.Position;

        _writer.Write($"{id} 0 obj\n<<\n");
        foreach (KeyValuePair<string, string> kvp in dict)
            _writer.Write($"  {kvp.Key} {EncryptObjectStrings(id, kvp.Value)}\n");

        _writer.Write($"  /Length {streamContent.Length}\n");
        _writer.Write(">>\nstream\n");
        _writer.Flush();

        _stream.Write(streamContent, 0, streamContent.Length);

        _writer.Write("\nendstream\n");
        _writer.Write("endobj\n");
    }

    /// <summary>
    /// Compresses binary data using ZLib (FlateDecode) and writes it as a PDF stream object.
    /// Used for font data (FontFile2, CIDToGIDMap, CIDSet) to reduce file size.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="streamContent">Binary data to compress and write</param>
    public void WriteCompressedBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent)
    {
        if (!InternalConfiguration.EnableCompression)
        {
            WriteBinaryObject(id, dict, streamContent);
            return;
        }

        using MemoryStream memoryStream = new();
        using (ZLibStream zlib = new(memoryStream, CompressionLevel.SmallestSize))
        {
            zlib.Write(streamContent, 0, streamContent.Length);
        }

        byte[] compressedBytes = memoryStream.ToArray();
        dict[PdfKeys.Stream.Filter] = "/FlateDecode";
        WriteBinaryObject(id, dict, compressedBytes);
    }

    /// <summary>
    /// Writes a PDF object with binary stream content (for font embedding).
    /// Unlike WriteObject which uses ASCII encoding, this writes raw binary data.
    /// Encrypts the stream content if encryption is enabled.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="streamContent">Binary stream content</param>
    public void WriteBinaryObject(int id, IDictionary<string, string> dict, byte[] streamContent)
    {
        // Encrypt if encryptor is set
        byte[] contentToWrite;
        if (Encryptor != null)
            contentToWrite = Encryptor.EncryptData(streamContent, id);
        else
            contentToWrite = streamContent;

        _writer.Flush();
        _offsets[id] = _stream.Position;

        _writer.Write($"{id} 0 obj\n<<\n");
        foreach (KeyValuePair<string, string> kvp in dict)
            _writer.Write($"  {kvp.Key} {EncryptObjectStrings(id, kvp.Value)}\n");

        _writer.Write($"  /Length {contentToWrite.Length}\n");
        _writer.Write(">>\nstream\n");
        _writer.Flush();

        // Write binary data directly to the underlying stream
        _stream.Write(contentToWrite, 0, contentToWrite.Length);

        // Continue with ASCII writer
        _writer.Write("\nendstream\n");
        _writer.Write("endobj\n");
    }

    /// <summary>
    /// Writes a PDF catalog object with an OpenAction to set the default zoom level.
    /// Use this as the final catalog object when closing the PDF.
    /// </summary>
    /// <param name="id">The catalog object ID</param>
    /// <param name="pagesId">The pages object ID</param>
    /// <param name="firstPageId">The first page object ID (for OpenAction)</param>
    /// <param name="zoomType">The zoom type: "Fit" (fit page), "FitH" (fit height), "FitW" (fit width), or "XYZ" (custom zoom)</param>
    /// <param name="zoomLevel">For XYZ type: 1.0 = 100%, 0.5 = 50%, etc. Ignored for Fit types.</param>
    [Obsolete("Unused by the library's own render pipeline (superseded by WriteCatalog(int, int, int, DocumentSettings, CatalogOptions), which every Document save path uses) and takes a stringly-typed zoomType. Kept for source compatibility only.")]
    public void WriteCatalogWithOpenAction(int id, int pagesId, int firstPageId, string zoomType = "Fit",
        double zoomLevel = 1.0)
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

        WriteObject(id, new Dictionary<string, string>
        {
            [PdfKeys.Common.Type] = "/Catalog",
            [PdfKeys.Catalog.Pages] = $"{pagesId} 0 R",
            [PdfKeys.Catalog.OpenAction] = openAction
        });
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