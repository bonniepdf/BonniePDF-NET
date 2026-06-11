using System.Text;

namespace BonniePDF.Core;

/// <summary>
/// StringBuilder helpers shared across the rendering paths (PDF content streams,
/// ToUnicode CMaps, SVG export).
/// </summary>
internal static class StringBuilderExtensions
{
    /// <summary>
    /// Appends <paramref name="text"/> followed by a single LF (<c>\n</c>). Unlike
    /// <see cref="StringBuilder.AppendLine(string)"/> - which uses
    /// <see cref="System.Environment.NewLine"/> (<c>\r\n</c> on Windows) - this is
    /// platform-independent, so generated output is byte-identical across operating
    /// systems (and aligned with the TypeScript port, which emits <c>\n</c>).
    /// </summary>
    internal static StringBuilder AppendLf(this StringBuilder sb, string text = "")
        => sb.Append(text).Append('\n');
}
