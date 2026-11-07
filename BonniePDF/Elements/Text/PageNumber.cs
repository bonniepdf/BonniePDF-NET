using System;

namespace BonniePDF;

/// <summary>Builds text containing the current page number or total page count.</summary>
public static class PageNumber
{
    /// <summary>Creates a text element whose {page} and {total} placeholders resolve on render.</summary>
    public static TextBlock Text(string format = "Page {page} of {total}") =>
        new(format.Replace("{page}", TextElementBase.CurrentPageToken, StringComparison.Ordinal)
            .Replace("{total}", TextElementBase.TotalPagesToken, StringComparison.Ordinal));
}
