using System;
using System.Collections.Generic;

namespace BonniePDF.Core;

/// <summary>Severity of a <see cref="Diagnostic"/>.</summary>
public enum DiagnosticSeverity
{
    Error,
    Warning,
    Info,
}

/// <summary>Category of a <see cref="Diagnostic"/> - what subsystem produced it.</summary>
public enum DiagnosticCategory
{
    Font,
    FontSubset,
    Image,
    Encryption,
    Compression,
    Tagging,
    Platform,
    Render,
    Attachment,
    /// <summary>
    /// A conformance claim the honest-claim guard withheld; <see cref="Diagnostic.Source"/>
    /// is the claim's token, for example <c>"pdf/a-2a"</c> or <c>"pdf/ua-1"</c>.
    /// </summary>
    Compliance,
}

/// <summary>
/// Structured, non-throwing report of a recoverable issue encountered while
/// generating a PDF - most commonly a font that couldn't be loaded, an image
/// that failed to decode, or a subset operation that fell back to embedding
/// the full font. Callers retrieve the collected diagnostics from
/// <see cref="Document.Diagnostics"/> after Save / SaveAsync / GenerateBytes
/// returns and decide how to surface them (log, display, ignore, throw).
/// </summary>
public sealed class Diagnostic(DiagnosticSeverity severity, DiagnosticCategory category, string message, string? source = null, Exception? cause = null)
{
    public DiagnosticSeverity Severity { get; } = severity;
    public DiagnosticCategory Category { get; } = category;
    public string Message { get; } = message;
    /// <summary>Optional identifier of what the diagnostic is about (e.g. a font path).</summary>
    public string? Source { get; } = source;
    /// <summary>The underlying exception, if any. Preserved for logging or rethrowing.</summary>
    public Exception? Cause { get; } = cause;
}

/// <summary>
/// Collects diagnostics during PDF generation. The Document owns one instance
/// for the lifetime of a Save / SaveAsync / GenerateBytes call; subsystems push
/// diagnostics into it instead of calling <c>Console.WriteLine</c> /
/// <c>Debug.WriteLine</c> directly so the library never writes to a global
/// sink the caller did not opt into.
/// </summary>
public sealed class DiagnosticsCollector
{
    private readonly List<Diagnostic> _entries = new();

    public void Add(Diagnostic d)
    {
        if (d != null) _entries.Add(d);
    }

    public void AddError(DiagnosticCategory category, string message, string? source = null, Exception? cause = null)
        => Add(new Diagnostic(DiagnosticSeverity.Error, category, message, source, cause));

    public void AddWarning(DiagnosticCategory category, string message, string? source = null, Exception? cause = null)
        => Add(new Diagnostic(DiagnosticSeverity.Warning, category, message, source, cause));

    public void AddInfo(DiagnosticCategory category, string message, string? source = null)
        => Add(new Diagnostic(DiagnosticSeverity.Info, category, message, source));

    public IReadOnlyList<Diagnostic> All => _entries;

    public int Count => _entries.Count;

    public bool HasErrors
    {
        get
        {
            foreach (Diagnostic d in _entries)
                if (d.Severity == DiagnosticSeverity.Error) return true;
            return false;
        }
    }

    public bool HasWarnings
    {
        get
        {
            foreach (Diagnostic d in _entries)
                if (d.Severity == DiagnosticSeverity.Warning) return true;
            return false;
        }
    }

    public void Clear() => _entries.Clear();

    /// <summary>
    /// Writes each collected diagnostic to <see cref="Console.Error"/> /
    /// <see cref="Console.Out"/> according to severity. This is a convenience
    /// for callers that just want the messages surfaced; library code never
    /// calls this implicitly.
    /// </summary>
    public void Print()
    {
        foreach (Diagnostic d in _entries)
        {
            string prefix = $"[BonniePDF/{d.Category}]";
            string tail = string.IsNullOrEmpty(d.Source) ? string.Empty : $" (source: {d.Source})";
            string line = $"{prefix} {d.Message}{tail}";
            switch (d.Severity)
            {
                case DiagnosticSeverity.Error:
                    Console.Error.WriteLine(line);
                    if (d.Cause != null) Console.Error.WriteLine(d.Cause);
                    break;
                case DiagnosticSeverity.Warning:
                    Console.Error.WriteLine(line);
                    if (d.Cause != null) Console.Error.WriteLine(d.Cause);
                    break;
                case DiagnosticSeverity.Info:
                    Console.Out.WriteLine(line);
                    break;
            }
        }
    }
}
