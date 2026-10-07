using BonniePDF.Rendering.Pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Metadata;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// Writes PDF/A compliance objects (ICC Profiles, Output Intents) to PDF.
/// </summary>
internal class ComplianceWriter
{
    private readonly IPdfWriter? _writer;
    private readonly IAsyncPdfWriter? _asyncWriter;

    public ComplianceWriter(IPdfWriter writer)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _asyncWriter = null;
    }

    public ComplianceWriter(IAsyncPdfWriter asyncWriter)
    {
        _asyncWriter = asyncWriter ?? throw new ArgumentNullException(nameof(asyncWriter));
        _writer = null;
    }

    /// <summary>
    /// Writes the sRGB ICC color profile to the PDF.
    /// Required for PDF/A-1b compliance.
    /// </summary>
    /// <param name="id">The object ID for the ICC profile.</param>
    public void WriteIccProfile(int id)
    {
        byte[] iccData = SRgbIccProfile.Generate();

        // Compress the ICC profile
        using MemoryStream ms = new();
        using (ZLibStream zlib = new(ms, CompressionLevel.SmallestSize))
        {
            zlib.Write(iccData, 0, iccData.Length);
        }
        byte[] compressed = ms.ToArray();

        Dictionary<string, string> dict = new()
        {
            [PdfKeys.ColorProfile.Components] = "3",
            [PdfKeys.Stream.Filter] = "/FlateDecode",
            [PdfKeys.Stream.DecodedLength] = iccData.Length.ToString()
        };

        _writer!.WriteBinaryObject(id, dict, compressed);
    }

    /// <summary>
    /// Writes the sRGB ICC color profile to the PDF asynchronously.
    /// Required for PDF/A-1b compliance.
    /// </summary>
    /// <param name="id">The object ID for the ICC profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteIccProfileAsync(int id, CancellationToken cancellationToken = default)
    {
        byte[] iccData = SRgbIccProfile.Generate();

        using MemoryStream ms = new();
        using (ZLibStream zlib = new(ms, CompressionLevel.SmallestSize))
        {
            await zlib.WriteAsync(iccData, cancellationToken).ConfigureAwait(false);
        }
        byte[] compressed = ms.ToArray();

        Dictionary<string, string> dict = new()
        {
            [PdfKeys.ColorProfile.Components] = "3",
            [PdfKeys.Stream.Filter] = "/FlateDecode",
            [PdfKeys.Stream.DecodedLength] = iccData.Length.ToString()
        };

        await _asyncWriter!.WriteBinaryObjectAsync(id, dict, compressed, compressed.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the Output Intent dictionary to the PDF.
    /// Required for PDF/A-1b compliance to specify the color reproduction characteristics.
    /// </summary>
    /// <param name="id">The object ID for the Output Intent dictionary.</param>
    /// <param name="iccProfileId">The object ID of the referenced ICC profile.</param>
    public void WriteOutputIntent(int id, int iccProfileId)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/OutputIntent",
            [PdfKeys.OutputIntent.Subtype] = "/GTS_PDFA1",
            [PdfKeys.OutputIntent.ConditionIdentifier] = "(sRGB IEC61966-2.1)",
            [PdfKeys.OutputIntent.RegistryName] = "(http://www.color.org)",
            [PdfKeys.OutputIntent.DestinationProfile] = $"{iccProfileId} 0 R"
        };

        _writer!.WriteObject(id, dict);
    }

    /// <summary>
    /// Writes the Output Intent dictionary to the PDF asynchronously.
    /// Required for PDF/A-1b compliance to specify the color reproduction characteristics.
    /// </summary>
    /// <param name="id">The object ID for the Output Intent dictionary.</param>
    /// <param name="iccProfileId">The object ID of the referenced ICC profile.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task WriteOutputIntentAsync(int id, int iccProfileId, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> dict = new()
        {
            [PdfKeys.Common.Type] = "/OutputIntent",
            [PdfKeys.OutputIntent.Subtype] = "/GTS_PDFA1",
            [PdfKeys.OutputIntent.ConditionIdentifier] = "(sRGB IEC61966-2.1)",
            [PdfKeys.OutputIntent.RegistryName] = "(http://www.color.org)",
            [PdfKeys.OutputIntent.DestinationProfile] = $"{iccProfileId} 0 R"
        };

        await _asyncWriter!.WriteObjectAsync(id, dict, null, cancellationToken).ConfigureAwait(false);
    }
}
