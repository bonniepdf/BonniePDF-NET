using System.Text;

namespace BonniePDF.Tests.Accessibility;

internal static class PdfPageStreamSplitter
{
    /// <summary>
    /// Pulls each page-content stream from the PDF body. Heuristic:
    /// any uncompressed stream containing BT and ET is treated as a
    /// page content stream. Good enough for tests; the real writer
    /// always emits page content uncompressed for now.
    /// </summary>
    public static string[] Split(string pdf)
    {
        // Normalize CRLF to LF so the search works on both Windows and Linux/macOS.
        // PdfWriter.WriteLine emits \r\n on Windows; the literals in Write() use \n.
        pdf = pdf.Replace("\r\n", "\n", System.StringComparison.Ordinal);

        var result = new System.Collections.Generic.List<string>();
        int idx = 0;
        while (true)
        {
            int streamStart = pdf.IndexOf("stream\n", idx, System.StringComparison.Ordinal);
            if (streamStart < 0) break;
            int contentStart = streamStart + "stream\n".Length;
            int streamEnd = pdf.IndexOf("endstream", contentStart, System.StringComparison.Ordinal);
            if (streamEnd < 0) break;
            string content = pdf.Substring(contentStart, streamEnd - contentStart);
            if (content.Contains("BT") && content.Contains("ET"))
                result.Add(content);
            idx = streamEnd + "endstream".Length;
        }
        return result.ToArray();
    }
}
