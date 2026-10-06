using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BonniePDF;

/// <summary>What to do with a construct a definition cannot express (spec 15 §11.2).</summary>
public enum DefinitionUnrepresentable
{
    /// <summary>Throw a <see cref="DefinitionException"/> listing every one, and write nothing.</summary>
    Error,

    /// <summary>Leave each one out and report a <c>dropped</c> note. The result is not a faithful copy.</summary>
    Drop,
}

/// <summary>What the host is told about a resource so it can decide whether it can name it.</summary>
/// <param name="Kind">One of <c>image</c>, <c>font</c>, <c>svg</c>, <c>attachment</c>.</param>
/// <param name="MediaType">The media type the definition will state.</param>
/// <param name="Bytes">The resource's bytes.</param>
public sealed record DefinitionResourceDescriptor(string Kind, string MediaType, byte[] Bytes);

/// <summary>Options for <see cref="Document.ToDefinition(DefinitionWriteOptions?)"/>.</summary>
public sealed class DefinitionWriteOptions
{
    /// <summary>What to do with a construct a definition cannot express. The default is to fail, writing nothing.</summary>
    public DefinitionUnrepresentable OnUnrepresentable { get; init; }

    /// <summary>Always carry resource bytes in the definition (<c>data</c>), even where <see cref="ResourceRef"/> would name them.</summary>
    public bool EmbedResources { get; init; }

    /// <summary>
    /// Names a resource the host can supply again when the definition is read. Return a name and the definition
    /// carries <c>ref</c> and a <c>sha256</c> instead of the bytes; return <see langword="null"/> to embed them.
    /// The writer never invents a name (spec 15 §9).
    /// </summary>
    public Func<DefinitionResourceDescriptor, string?>? ResourceRef { get; init; }

    /// <summary>Write a palette token (<c>slate-800</c>) where a colour equals a palette entry exactly, instead of hex.</summary>
    public bool WritePaletteTokens { get; init; }

    /// <summary>The definition version to write, <c>"1.0"</c>. Anything this release cannot write is refused.</summary>
    public string? Target { get; init; }
}

/// <summary>A definition and what the writer had to say about it.</summary>
public sealed class DefinitionWriteResult
{
    internal DefinitionWriteResult(JsonObject definition, IReadOnlyList<DefinitionIssue> notes)
    {
        Definition = definition;
        Notes = notes;
    }

    /// <summary>The definition, as a JSON tree.</summary>
    public JsonObject Definition { get; }

    /// <summary><c>secret-omitted</c> and <c>dropped</c> notes (spec 15 §11.4).</summary>
    public IReadOnlyList<DefinitionIssue> Notes { get; }

    /// <summary>
    /// The definition as JSON text. Characters that need no escaping in a JSON file (<c>+</c> in base64, letters
    /// outside ASCII) are written as they are.
    /// </summary>
    /// <param name="indented">Write it over several lines.</param>
    public string ToJson(bool indented = false)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = indented, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            Definition.WriteTo(writer);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
