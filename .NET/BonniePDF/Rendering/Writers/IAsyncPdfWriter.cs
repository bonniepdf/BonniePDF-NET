using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Security.Encryption;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Interface for async PDF writers
/// </summary>
public interface IAsyncPdfWriter : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the encryptor for this writer. When set, streams are encrypted.
    /// </summary>
    PdfEncryptor Encryptor { get; set; }

    /// <summary>
    /// Gets a new unique object ID for the PDF
    /// </summary>
    int GetNewId();

    /// <summary>
    /// Writes a PDF object with the specified ID and dictionary
    /// </summary>
    Task WriteObjectAsync(int id, IDictionary<string, string> dict, string streamContent = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compresses and writes a PDF object
    /// </summary>
    Task WriteCompressedObjectAsync(int id, IDictionary<string, string> dict, string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a PDF object with binary stream content
    /// </summary>
    Task WriteBinaryObjectAsync(int id, IDictionary<string, string> dict, byte[] streamContent, int length,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compresses binary data using FlateDecode and writes it as a PDF stream object
    /// </summary>
    Task WriteCompressedBinaryObjectAsync(int id, IDictionary<string, string> dict, byte[] streamContent, int length,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a link annotation object for a hyperlink
    /// </summary>
    Task<int> WriteLinkAnnotationAsync(int id, Hyperlink hyperlink, CancellationToken cancellationToken = default);

    /// Writes a viewer preferences dictionary
    /// <summary>
    /// Writes viewer preferences to the document catalog asynchronously.
    /// </summary>
    Task<int> WriteViewerPreferencesAsync(int id, DocumentSettings settings,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a PDF catalog object with catalog options
    /// </summary>
    Task WriteCatalogAsync(int id, int pagesId, int firstPageId, DocumentSettings settings, CatalogOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an uncompressed binary stream object, bypassing encryption.
    /// Used for XMP metadata which must not be encrypted.
    /// </summary>
    Task WriteUnencryptedBinaryObjectAsync(int id, IDictionary<string, string> dict, byte[] streamContent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalizes the PDF by writing the xref table and trailer
    /// </summary>
    Task CloseAsync(int rootId, int infoId = 0, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the encryption dictionary object
    /// </summary>
    Task<int> WriteEncryptDictionaryAsync(int id, CancellationToken cancellationToken = default);
}