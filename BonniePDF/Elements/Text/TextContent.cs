using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF;

internal static class TextContent
{
    internal static string Join(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);
        return string.Join("\n", lines.Select(line => line ?? throw new ArgumentNullException(nameof(lines))));
    }
}
