using System.Text.Json;

namespace BonniePDF.Definition;

/// <summary>
/// Entry points for the Document Definition: a versioned, language-neutral JSON description of a whole
/// document. Read one into a <see cref="Document"/> with <see cref="Document.FromDefinitionAsync"/>.
/// </summary>
public static class DocumentDefinition
{
    /// <summary>The versions of the definition this release reads.</summary>
    public static DefinitionVersions SupportedVersions { get; } =
        new(ElementDescription.Data.Definition.Version, 0, ElementDescription.Data.Definition.MinorVersion);

    /// <summary>
    /// Checks a definition completely against the format and the reader's limits, and returns every
    /// problem with its location. Nothing is constructed.
    /// </summary>
    /// <param name="json">The definition as JSON text.</param>
    /// <param name="options">Strictness and limits.</param>
    public static DefinitionValidationResult Validate(string json, DefinitionValidateOptions? options = null) =>
        DefinitionValidator.Validate(json, options);

    /// <summary>Checks a definition that has already been parsed.</summary>
    /// <param name="definition">The parsed definition.</param>
    /// <param name="options">Strictness and limits.</param>
    public static DefinitionValidationResult Validate(JsonElement definition, DefinitionValidateOptions? options = null) =>
        DefinitionValidator.Validate(definition, options);
}
