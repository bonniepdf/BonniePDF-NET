using System;
using BonniePDF.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BonniePDF.Definition;

/// <summary>One problem found in a definition, located by JSON Pointer (RFC 6901).</summary>
/// <param name="Path">JSON Pointer to the value at fault; empty is the whole document.</param>
/// <param name="Code">A stable, kebab-case code; see the Document Definition specification.</param>
/// <param name="Message">English text for a person. May improve between releases; the code may not change meaning.</param>
public sealed record DefinitionIssue(string Path, string Code, string Message)
{
    /// <summary>The <c>id</c> of the element the problem is in, when it has one.</summary>
    public string? Id { get; init; }

    /// <summary>What would have been valid.</summary>
    public string? Expected { get; init; }

    /// <summary>What was there.</summary>
    public string? Found { get; init; }

    /// <summary>The minor version that introduced an item the reader knows but the document's version does not include, when the reader can tell.</summary>
    public string? Since { get; init; }
}

/// <summary>The limits a reader enforces on a definition before constructing anything.</summary>
public sealed class DefinitionLimits
{
    /// <summary>Largest input, in UTF-8 bytes.</summary>
    public long InputBytes { get; init; } = ElementDescription.Data.Limits.InputBytes;

    /// <summary>Deepest JSON nesting.</summary>
    public int JsonDepth { get; init; } = ElementDescription.Data.Limits.JsonDepth;

    /// <summary>Deepest nesting of elements.</summary>
    public int ElementDepth { get; init; } = ElementDescription.Data.Limits.ElementDepth;

    /// <summary>Most element nodes in all.</summary>
    public int ElementNodes { get; init; } = ElementDescription.Data.Limits.ElementNodes;

    /// <summary>Most pages.</summary>
    public int Pages { get; init; } = ElementDescription.Data.Limits.Pages;

    /// <summary>Longest string, in UTF-16 code units.</summary>
    public int StringLength { get; init; } = ElementDescription.Data.Limits.StringLength;

    /// <summary>Longest array.</summary>
    public int ArrayLength { get; init; } = ElementDescription.Data.Limits.ArrayLength;

    /// <summary>Largest single resource, decoded.</summary>
    public long ResourceBytes { get; init; } = ElementDescription.Data.Limits.ResourceBytes;

    /// <summary>Largest total of resources, decoded.</summary>
    public long ResourceBytesTotal { get; init; } = ElementDescription.Data.Limits.ResourceBytesTotal;

    /// <summary>Most chart data points in all.</summary>
    public int ChartDataPoints { get; init; } = ElementDescription.Data.Limits.ChartDataPoints;
}

/// <summary>Options for <see cref="DocumentDefinition.Validate(string, DefinitionValidateOptions?)"/>.</summary>
public class DefinitionValidateOptions
{
    /// <summary>Treat unrecognised keys as errors instead of warnings.</summary>
    public bool Strict { get; init; }

    /// <summary>
    /// Read a document written for a newer minor version, ignoring what is not recognised (each item
    /// becomes a warning).
    /// </summary>
    public bool BestEffort { get; init; }

    /// <summary>Overrides for the default limits.</summary>
    public DefinitionLimits? Limits { get; init; }
}

/// <summary>The result of validating a definition. Nothing was constructed.</summary>
public sealed class DefinitionValidationResult
{
    internal DefinitionValidationResult(IReadOnlyList<DefinitionIssue> errors, IReadOnlyList<DefinitionIssue> warnings, JsonElement? definition)
    {
        Errors = errors;
        Warnings = warnings;
        Definition = definition;
    }

    /// <summary>True when there are no errors.</summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>Everything wrong with the definition.</summary>
    public IReadOnlyList<DefinitionIssue> Errors { get; }

    /// <summary>Things worth knowing that do not stop the definition being read.</summary>
    public IReadOnlyList<DefinitionIssue> Warnings { get; }

    /// <summary>The parsed definition; present only when <see cref="IsValid"/>.</summary>
    public JsonElement? Definition { get; }
}

/// <summary>What the host is asked for when a definition names a resource it does not carry.</summary>
/// <param name="Name">The name in the definition's <c>resources</c>.</param>
/// <param name="Kind">The resource kind: <c>image</c>, <c>font</c>, <c>svg</c> or <c>attachment</c>.</param>
/// <param name="MediaType">The declared media type.</param>
public sealed record DefinitionResourceRequest(string Name, string Kind, string MediaType);

/// <summary>Passwords for an encrypted definition. They are never part of a definition.</summary>
public sealed class DefinitionPasswords
{
    /// <summary>The owner password. Required when the definition is encrypted.</summary>
    public string? Owner { get; init; }

    /// <summary>The user (open) password.</summary>
    public string? User { get; init; }
}

/// <summary>Options for reading a definition into a document.</summary>
public sealed class DefinitionReadOptions : DefinitionValidateOptions
{
    /// <summary>
    /// Supplies the bytes for a resource that has a <c>ref</c>. The reader never opens a path or fetches a
    /// URL on its own: <c>ref</c> is an opaque name the host resolves. Return null when the host has
    /// nothing under that name.
    /// </summary>
    public Func<string, DefinitionResourceRequest, CancellationToken, Task<byte[]?>>? Resources { get; init; }

    /// <summary>Passwords for <c>settings.encryption</c>.</summary>
    public DefinitionPasswords? Passwords { get; init; }

    /// <summary>An SVG rasterizer to register on the document. Supplied by the host, never stored.</summary>
    public Func<string, Task<byte[]>>? Rasterizer { get; init; }
}

/// <summary>Thrown when a definition cannot be turned into a document. Carries every problem found.</summary>
public sealed class DefinitionException : BonniePdfException
{
    /// <summary>Creates the exception from the problems found.</summary>
    public DefinitionException(IReadOnlyList<DefinitionIssue> issues)
        : base(BuildMessage(issues))
    {
        Issues = issues;
    }

    /// <summary>Every problem found, each with its location.</summary>
    public IReadOnlyList<DefinitionIssue> Issues { get; }

    private static string BuildMessage(IReadOnlyList<DefinitionIssue> issues)
    {
        string shown = string.Join("; ", issues.Count > 5 ? issues.Take(5).Select(Describe) : issues.Select(Describe));
        string more = issues.Count > 5 ? $"; and {issues.Count - 5} more" : "";
        return $"The definition has {issues.Count} problem{(issues.Count == 1 ? "" : "s")}: {shown}{more}";
    }

    private static string Describe(DefinitionIssue issue) => $"{(issue.Path.Length == 0 ? "/" : issue.Path)} {issue.Code}";
}

/// <summary>The versions of the Document Definition this release reads and writes.</summary>
/// <param name="Version">The definition version.</param>
/// <param name="MinMinor">The oldest minor version read.</param>
/// <param name="MaxMinor">The newest minor version read.</param>
public sealed record DefinitionVersions(int Version, int MinMinor, int MaxMinor);
