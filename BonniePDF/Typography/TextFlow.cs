using System;
using System.Collections.Generic;
using System.Threading;

namespace BonniePDF;

// Synchronous shaping operations share this scope; AsyncLocal isolates concurrent documents.
internal static class TextFlow
{
    internal sealed record Line(string Paragraph, int Start, int End);
    private sealed record State(ContentDirection Direction, Line? Line);
    private static readonly AsyncLocal<State?> Ambient = new();
    internal static bool RightToLeft => Ambient.Value?.Direction == ContentDirection.RightToLeft;
    internal static Line? CurrentLine => Ambient.Value?.Line;
    internal static string CacheKey => Ambient.Value is { } state
        ? $"{(int)state.Direction}:{state.Line?.Start}:{state.Line?.End}:{state.Line?.Paragraph}" : "";

    internal readonly struct Scope : IDisposable
    {
        private readonly State? previous;
        private readonly bool changed;
        private Scope(State? previous, bool changed) { this.previous = previous; this.changed = changed; }
        public void Dispose() { if (changed) Ambient.Value = previous; }
        internal static Scope Enter(ContentDirection direction, Line? line)
        {
            var prior = Ambient.Value;
            if (line is null && (prior?.Direction ?? ContentDirection.LeftToRight) == direction) return default;
            Ambient.Value = new State(direction, line);
            return new Scope(prior, true);
        }
    }

    internal static Scope Enter(ContentDirection direction, Line? line = null) => Scope.Enter(direction, line);
    internal static bool NeedsBidi(string text)
    {
        foreach (char c in text)
            if (c >= '\u0590' && c <= '\u08ff' || c >= '\u200e' && c <= '\u200f' ||
                c >= '\u202a' && c <= '\u202e' || c >= '\u2066' && c <= '\u2069' || c >= '\ufb1d' && c <= '\ufeff') return true;
        return false;
    }

    internal static Line?[] MapLines(string source, IList<string> lines)
    {
        var paragraphs = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var result = new Line?[lines.Count];
        int paragraph = 0, offset = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            while (paragraph < paragraphs.Length)
            {
                string value = paragraphs[paragraph];
                int start = value.IndexOf(lines[i], offset, StringComparison.Ordinal);
                if (start >= 0)
                {
                    result[i] = new Line(value, start, start + lines[i].Length);
                    offset = start + lines[i].Length;
                    if (offset == value.Length) { paragraph++; offset = 0; }
                    break;
                }
                paragraph++; offset = 0;
            }
        }
        return result;
    }
}
