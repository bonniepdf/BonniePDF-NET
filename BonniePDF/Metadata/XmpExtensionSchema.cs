using System.Collections.Generic;

namespace BonniePDF.Metadata;

/// <summary>
/// Describes one entry in the PDF/A <c>pdfaExtension:schemas</c> bag -
/// the mechanism ISO 19005-2/3 §6.6.2.3 provides for declaring XMP
/// namespaces that are NOT predefined by the standard (e.g.
/// <c>pdfuaid</c> for PDF/UA-1 identification, <c>fx</c> for
/// ZUGFeRD / Factur-X).
///
/// veraPDF profile <c>2b</c> rule 6.6.2.3.1 rejects any non-predefined
/// XMP property whose namespace is not described here.
/// </summary>
internal sealed class XmpExtensionSchema
{
    /// <summary>The namespace URI being described (matches the
    /// non-predefined namespace used elsewhere in the XMP packet).</summary>
    public string NamespaceUri { get; }

    /// <summary>The XML prefix used elsewhere for this namespace.</summary>
    public string Prefix { get; }

    /// <summary>Human-readable schema name (free text).</summary>
    public string SchemaName { get; }

    /// <summary>Property descriptors - one per property the schema exposes.</summary>
    public IReadOnlyList<XmpExtensionSchemaProperty> Properties { get; }

    public XmpExtensionSchema(
        string namespaceUri,
        string prefix,
        string schemaName,
        IReadOnlyList<XmpExtensionSchemaProperty> properties)
    {
        NamespaceUri = namespaceUri;
        Prefix = prefix;
        SchemaName = schemaName;
        Properties = properties;
    }
}

/// <summary>
/// Describes one property inside an <see cref="XmpExtensionSchema"/>.
/// Maps to the <c>pdfaProperty:*</c> fields required by ISO 19005-2/3
/// §6.6.2.3.2.
/// </summary>
internal sealed class XmpExtensionSchemaProperty
{
    /// <summary>Property name (e.g. <c>"part"</c>).</summary>
    public string Name { get; }

    /// <summary>XMP value type (e.g. <c>"Integer"</c>, <c>"Text"</c>).</summary>
    public string ValueType { get; }

    /// <summary>Either <c>"internal"</c> or <c>"external"</c>.</summary>
    public string Category { get; }

    /// <summary>Human-readable description of the property.</summary>
    public string Description { get; }

    public XmpExtensionSchemaProperty(string name, string valueType, string category, string description)
    {
        Name = name;
        ValueType = valueType;
        Category = category;
        Description = description;
    }
}
