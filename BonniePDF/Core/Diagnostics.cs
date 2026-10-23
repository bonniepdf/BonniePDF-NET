using System;
using System.Collections.Generic;

namespace BonniePDF;

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
    /// <summary>How serious the issue is.</summary>
    public DiagnosticSeverity Severity { get; } = severity;
    /// <summary>The subsystem the issue came from.</summary>
    public DiagnosticCategory Category { get; } = category;
    /// <summary>Human-readable description of the issue.</summary>
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

    /// <summary>Adds a diagnostic to the collection. A <c>null</c> diagnostic is ignored.</summary>
    public void Add(Diagnostic? d)
    {
        if (d != null) _entries.Add(d);
    }

    /// <summary>
    /// Adds an error-severity diagnostic with the given category, message, optional source and underlying
    /// exception.
    /// </summary>
    public void AddError(DiagnosticCategory category, string message, string? source = null, Exception? cause = null)
        => Add(new Diagnostic(DiagnosticSeverity.Error, category, message, source, cause));

    /// <summary>
    /// Adds a warning-severity diagnostic with the given category, message, optional source and underlying
    /// exception.
    /// </summary>
    public void AddWarning(DiagnosticCategory category, string message, string? source = null, Exception? cause = null)
        => Add(new Diagnostic(DiagnosticSeverity.Warning, category, message, source, cause));

    /// <summary>Adds an informational diagnostic with the given category, message and optional source.</summary>
    public void AddInfo(DiagnosticCategory category, string message, string? source = null)
        => Add(new Diagnostic(DiagnosticSeverity.Info, category, message, source));

    /// <summary>Every diagnostic collected so far, in the order it was added.</summary>
    public IReadOnlyList<Diagnostic> All => _entries;

    /// <summary>The number of diagnostics collected so far.</summary>
    public int Count => _entries.Count;

    /// <summary><c>true</c> when at least one error-severity diagnostic has been collected.</summary>
    public bool HasErrors
    {
        get
        {
            foreach (Diagnostic d in _entries)
                if (d.Severity == DiagnosticSeverity.Error) return true;
            return false;
        }
    }

    /// <summary><c>true</c> when at least one warning-severity diagnostic has been collected.</summary>
    public bool HasWarnings
    {
        get
        {
            foreach (Diagnostic d in _entries)
                if (d.Severity == DiagnosticSeverity.Warning) return true;
            return false;
        }
    }

    /// <summary>Removes every collected diagnostic.</summary>
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
