using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Rendering.Pdf;
using BonniePDF.Security.Encryption;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// An async PDF writer that creates PDF 1.4 compliant files.
/// Manages object writing, offset tracking, and the cross-reference table using async I/O.
/// Supports optional encryption of stream contents.
/// IMPORTANT: This class only implements IAsyncPdfWriter, not IPdfWriter.
/// This prevents the sync-over-async anti-pattern where async methods are blocked
/// synchronously, which can cause thread pool starvation.
/// Use SimplePdfWriter for synchronous operations, AsyncPdfWriter for async operations.
/// </summary>
public class AsyncPdfWriter : IAsyncPdfWriter, IAsyncDisposable
{
    private readonly FileStream _fs;
    private readonly bool _leaveOpen;

    // Maps object ID to file position for the xref table
    private readonly Dictionary<int, long> _offsets = [];
    private readonly Stream _stream;
    private readonly StreamWriter _writer;
    private int _currentObjId;
    private int _encryptId;
    private readonly PdfVersion _version;

    /// <summary>
    /// Creates a new async PDF writer that writes to a file
    /// </summary>
    /// <param name="filename">The path to the file to create</param>
    /// <param name="version">The PDF version to emit</param>
    public AsyncPdfWriter(string filename, PdfVersion version = PdfVersion.Pdf14)
    {
        _version = version;
        _fs = new FileStream(filename, FileMode.Create);
        _stream = _fs;
        _leaveOpen = false;
        _writer = new StreamWriter(_stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        _writer.WriteLine(version == PdfVersion.Pdf17 ? "%PDF-1.7" : "%PDF-1.4");
        _writer.WriteLine("%\u00E2\u00E3\u00CF\u00D3");
    }

    /// <summary>
    /// Creates a new async PDF writer that writes to a stream
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="leaveOpen">If true, the stream will not be disposed when this writer is disposed</param>
    /// <param name="version">The PDF version to emit</param>
    public AsyncPdfWriter(Stream stream, bool leaveOpen = false, PdfVersion version = PdfVersion.Pdf14)
    {
        if (stream == null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable", nameof(stream));

        _version = version;
        _fs = null;
        _stream = stream;
        _leaveOpen = leaveOpen;
        _writer = new StreamWriter(_stream, Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        _writer.WriteLine(version == PdfVersion.Pdf17 ? "%PDF-1.7" : "%PDF-1.4");
        _writer.WriteLine("%\u00E2\u00E3\u00CF\u00D3");
    }

    /// <summary>
    /// Gets or sets the encryptor for this writer. When set, streams are encrypted.
    /// </summary>
    public PdfEncryptor Encryptor { get; set; }

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
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteObjectAsync(int id, Dictionary<string, string> dict, string streamContent = null,
        CancellationToken cancellationToken = default)
    {
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _offsets[id] = _stream.Position;

        await _writer.WriteLineAsync($"{id} 0 obj").ConfigureAwait(false);
        await _writer.WriteLineAsync("<<").ConfigureAwait(false);
        foreach ((string key, string value) in dict)
            await _writer.WriteLineAsync($"  {key} {value}").ConfigureAwait(false);

        if (streamContent != null)
        {
            byte[] contentBytes = Encoding.ASCII.GetBytes(streamContent);

            // Encrypt if encryptor is set
            if (Encryptor != null)
                contentBytes = Encryptor.EncryptData(contentBytes, id);

            await _writer.WriteLineAsync($"  /Length {contentBytes.Length}").ConfigureAwait(false);
            await _writer.WriteLineAsync(">>").ConfigureAwait(false);
            await _writer.WriteLineAsync("stream").ConfigureAwait(false);
            await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            await _stream.WriteAsync(contentBytes, 0, contentBytes.Length, cancellationToken).ConfigureAwait(false);
            await _writer.WriteAsync("\nendstream\n").ConfigureAwait(false);
        }
        else
        {
            await _writer.WriteLineAsync(">>").ConfigureAwait(false);
        }

        await _writer.WriteLineAsync("endobj").ConfigureAwait(false);
    }

    /// <summary>
    /// Compresses the string content using ZLib (FlateDecode) and writes it as a binary object.
    /// This drastically reduces PDF file size.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="content">String content to compress and write</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteCompressedObjectAsync(int id, Dictionary<string, string> dict, string content,
        CancellationToken cancellationToken = default)
    {
        // 1. Convert to bytes
        byte[] rawBytes = Encoding.ASCII.GetBytes(content);

        // 2. RENT a buffer.
        // We guess the compressed size. Usually it's smaller, but ZLib overhead on tiny strings
        // might make it slightly larger. Renting rawBytes.Length is usually safe enough for text,
        // or you can rent slightly larger (rawBytes.Length + 100) to avoid resizing.
        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(rawBytes.Length + 64);

        try
        {
            int actualLength;

            // 3. Create MemoryStream over the rented buffer.
            // recursive: false ensures we don't accidentally close the buffer's lease (though ArrayPool doesn't have "leases" like that, it's good practice).
            // We let the MemoryStream manage writing into the array.
            using (MemoryStream outputMs = new(rentedBuffer))
            {
                // 4. Compress into the MemoryStream
                using (ZLibStream zlib = new(outputMs, CompressionLevel.Optimal, true))
                {
                    await zlib.WriteAsync(rawBytes, 0, rawBytes.Length, cancellationToken).ConfigureAwait(false);
                }
                // ZLibStream is disposed here, forcing a flush to outputMs.

                // 5. Capture the actual length of data written
                actualLength = (int)outputMs.Position;
            }

            // 6. Update Dictionary
            dict["/Filter"] = "/FlateDecode";

            // 7. Write to the actual PDF stream
            // IMPORTANT: Pass 'rentedBuffer' but ONLY write 'actualLength' bytes
            await WriteBinaryObjectAsync(id, dict, rentedBuffer, actualLength, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // 8. CRITICAL: Return the buffer to the pool!
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// <summary>
    /// Writes a link annotation object for a hyperlink
    /// </summary>
    /// <param name="id">The object ID for the annotation</param>
    /// <param name="hyperlink">The hyperlink to create an annotation for</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The object ID of the annotation</returns>
    public async Task<int> WriteLinkAnnotationAsync(int id, Hyperlink hyperlink,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Annot",
            ["/Subtype"] = "/Link",
            ["/Rect"] = hyperlink.GetRectangleString(),
            ["/F"] = "4", // Print flag (PDF/A-1B requires annotations to be printable)
            ["/A"] = $"<< /S /URI /URI ({PdfSyntax.Escape(hyperlink.Uri)}) >>"
        };

        await WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Writes the encryption dictionary object
    /// </summary>
    /// <param name="id">The object ID for the encryption dictionary</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The object ID of the encryption dictionary</returns>
    public async Task<int> WriteEncryptDictionaryAsync(int id, CancellationToken cancellationToken = default)
    {
        if (Encryptor == null)
            return 0;

        _encryptId = id;
        Dictionary<string, string> dict = Encryptor.GetEncryptDictionary();
        await WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Finalizes the PDF by writing the xref table and trailer
    /// </summary>
    /// <param name="rootId">The object ID of the catalog/root object</param>
    /// <param name="infoId">The object ID of the document info dictionary (optional, 0 if none)</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task CloseAsync(int rootId, int infoId = 0, CancellationToken cancellationToken = default)
    {
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        long xrefPos = _stream.Position;

        await _writer.WriteAsync("xref\n").ConfigureAwait(false);
        // The +1 accounts for the special "zero" entry
        await _writer.WriteAsync($"0 {_currentObjId + 1}\n").ConfigureAwait(false);
        await _writer.WriteAsync("0000000000 65535 f \n").ConfigureAwait(false);

        // Loop through IDs 1..N and look up their position
        for (int i = 1; i <= _currentObjId; i++)
            if (_offsets.TryGetValue(i, out long offset))
                await _writer.WriteAsync($"{offset:D10} 00000 n \n").ConfigureAwait(false);
            else
                // Should not happen if logic is correct, but safe fallback
                throw new Exception($"Object ID {i} was reserved but never written to the PDF.");

        await _writer.WriteAsync("trailer\n").ConfigureAwait(false);
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

        string trailerDict = $"<< {string.Join(" ", trailerParts)} >>\n";
        await _writer.WriteAsync(trailerDict).ConfigureAwait(false);
        await _writer.WriteAsync("startxref\n").ConfigureAwait(false);
        await _writer.WriteAsync($"{xrefPos}\n").ConfigureAwait(false);
        await _writer.WriteAsync("%%EOF").ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a viewer preferences dictionary
    /// </summary>
    /// <param name="id">The object ID for the viewer preferences</param>
    /// <param name="settings">The document settings containing viewer preferences</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The object ID of the viewer preferences, or 0 if no preferences are set</returns>
    public async Task<int> WriteViewerPreferencesAsync(int id, DocumentSettings settings,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new();

        if (settings.HideToolbar)
            dict["/HideToolbar"] = "true";

        if (settings.HideMenubar)
            dict["/HideMenubar"] = "true";

        if (settings.HideWindowUI)
            dict["/HideWindowUI"] = "true";

        if (settings.FitWindow)
            dict["/FitWindow"] = "true";

        if (settings.CenterWindow)
            dict["/CenterWindow"] = "true";

        if (settings.DisplayDocTitle)
            dict["/DisplayDocTitle"] = "true";

        // Only write if there are actual preferences
        if (dict.Count == 0)
            return 0;

        await WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <summary>
    /// Writes a PDF catalog object with full support for OpenAction, PageMode, and ViewerPreferences
    /// </summary>
    /// <param name="id">The catalog object ID</param>
    /// <param name="pagesId">The pages object ID</param>
    /// <param name="firstPageId">The first page object ID (for OpenAction)</param>
    /// <param name="settings">The document settings</param>
    /// <param name="viewerPrefsId">The viewer preferences object ID, or 0 if none</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteCatalogAsync(int id, int pagesId, int firstPageId, DocumentSettings settings,
        CatalogOptions options, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new()
        {
            ["/Type"] = "/Catalog",
            ["/Pages"] = $"{pagesId} 0 R"
        };

        // Add OpenAction
        string openAction = GetOpenActionString(firstPageId, settings);
        dict["/OpenAction"] = openAction;

        // Add PageMode — auto-set to UseOutlines when outlines are present
        if (options?.OutlinesId > 0)
            dict["/PageMode"] = "/UseOutlines";
        else if (settings.PageMode != PageMode.UseNone)
            dict["/PageMode"] = $"/{GetPageModeString(settings.PageMode)}";

        // Add ViewerPreferences if provided
        if (options?.ViewerPrefsId > 0) dict["/ViewerPreferences"] = $"{options.ViewerPrefsId} 0 R";

        // Add Outlines if provided
        if (options?.OutlinesId > 0) dict["/Outlines"] = $"{options.OutlinesId} 0 R";

        // Add XMP Metadata if provided
        if (options?.XmpMetadataId > 0) dict["/Metadata"] = $"{options.XmpMetadataId} 0 R";

        // Add OutputIntents if provided (PDF/A)
        if (options?.OutputIntentIds is { Count: > 0 })
        {
            string refs = string.Join(" ", options.OutputIntentIds.ConvertAll(oid => $"{oid} 0 R"));
            dict["/OutputIntents"] = $"[ {refs} ]";
        }

        // Add MarkInfo for PDF/A
        if (options?.PdfAEnabled == true)
            dict["/MarkInfo"] = "<< /Marked true >>";

        await WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes an uncompressed binary stream object, bypassing encryption.
    /// Used for XMP metadata which must not be encrypted even when encryption is active.
    /// </summary>
    public async Task WriteUnencryptedBinaryObjectAsync(int id, Dictionary<string, string> dict, byte[] streamContent,
        CancellationToken cancellationToken = default)
    {
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _offsets[id] = _stream.Position;

        await _writer.WriteAsync($"{id} 0 obj\n<<\n").ConfigureAwait(false);
        foreach (KeyValuePair<string, string> kvp in dict)
            await _writer.WriteAsync($"  {kvp.Key} {kvp.Value}\n").ConfigureAwait(false);

        await _writer.WriteAsync($"  /Length {streamContent.Length}\n").ConfigureAwait(false);
        await _writer.WriteAsync(">>\nstream\n").ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        await _stream.WriteAsync(streamContent, 0, streamContent.Length, cancellationToken).ConfigureAwait(false);

        await _writer.WriteAsync("\nendstream\n").ConfigureAwait(false);
        await _writer.WriteAsync("endobj\n").ConfigureAwait(false);
    }

    /// <summary>
    /// Compresses binary data using ZLib (FlateDecode) and writes it as a PDF stream object.
    /// Used for font data (FontFile2, CIDToGIDMap, CIDSet) to reduce file size.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="streamContent">Binary data to compress and write</param>
    /// <param name="length">The actual number of bytes to compress from the buffer</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteCompressedBinaryObjectAsync(int id, Dictionary<string, string> dict, byte[] streamContent,
        int length, CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new();
        using (ZLibStream zlib = new(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await zlib.WriteAsync(streamContent, 0, length, cancellationToken).ConfigureAwait(false);
        }

        byte[] compressedBytes = memoryStream.ToArray();
        dict["/Filter"] = "/FlateDecode";
        await WriteBinaryObjectAsync(id, dict, compressedBytes, compressedBytes.Length, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a PDF object with binary stream content.
    /// Updated to accept an explicit length to support rented buffers (ArrayPool)
    /// where the buffer size may be larger than the actual content.
    /// Encrypts the stream content if encryption is enabled.
    /// </summary>
    /// <param name="id">The object ID</param>
    /// <param name="dict">Key-value pairs for the object dictionary</param>
    /// <param name="streamContent">Binary stream content (buffer)</param>
    /// <param name="length">The actual number of bytes to write from the buffer</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteBinaryObjectAsync(int id, Dictionary<string, string> dict, byte[] streamContent, int length,
        CancellationToken cancellationToken = default)
    {
        // Encrypt if encryptor is set
        byte[] contentToWrite;
        int lengthToWrite;

        if (Encryptor != null)
        {
            // Create a copy for encryption since we don't own the buffer
            byte[] toEncrypt = new byte[length];
            Buffer.BlockCopy(streamContent, 0, toEncrypt, 0, length);
            Encryptor.EncryptInPlace(toEncrypt, id);
            contentToWrite = toEncrypt;
            lengthToWrite = length;
        }
        else
        {
            contentToWrite = streamContent;
            lengthToWrite = length;
        }

        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        _offsets[id] = _stream.Position;

        // Write object header and dictionary keys
        await _writer.WriteAsync($"{id} 0 obj\n<<\n").ConfigureAwait(false);
        foreach (KeyValuePair<string, string> kvp in dict)
            await _writer.WriteAsync($"  {kvp.Key} {kvp.Value}\n").ConfigureAwait(false);

        // IMPORTANT: Write the explicit length, not the buffer length
        await _writer.WriteAsync($"  /Length {lengthToWrite}\n").ConfigureAwait(false);

        await _writer.WriteAsync(">>\nstream\n").ConfigureAwait(false);
        await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);

        // Write binary data directly to the underlying stream
        // We explicitly write 'lengthToWrite' bytes, ignoring the rest of the buffer
        await _stream.WriteAsync(contentToWrite, 0, lengthToWrite, cancellationToken).ConfigureAwait(false);

        // Continue with ASCII writer
        await _writer.WriteAsync("\nendstream\n").ConfigureAwait(false);
        await _writer.WriteAsync("endobj\n").ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        // Always flush the writer to ensure all buffered data is written
        if (_writer != null)
            await _writer.FlushAsync().ConfigureAwait(false);

        if (!_leaveOpen)
        {
            // Only dispose the writer and streams when we're not leaving them open
            if (_writer != null)
                await _writer.DisposeAsync().ConfigureAwait(false);
            if (_stream != null)
                await _stream.DisposeAsync().ConfigureAwait(false);
            if (_fs != null)
                await _fs.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            // When leaving the stream open, still dispose the writer since it now has leaveOpen: true
            if (_writer != null)
                await _writer.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
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
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    public async Task WriteCatalogWithOpenActionAsync(int id, int pagesId, int firstPageId, string zoomType = "Fit",
        double zoomLevel = 1.0, CancellationToken cancellationToken = default)
    {
        string openAction;

        switch (zoomType.ToUpper())
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

        await WriteObjectAsync(id, new Dictionary<string, string>
        {
            ["/Type"] = "/Catalog",
            ["/Pages"] = $"{pagesId} 0 R",
            ["/OpenAction"] = openAction
        }, null, cancellationToken).ConfigureAwait(false);
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