namespace BonniePDF.Examples.Core;

/// <summary>
/// Defines the category for an example, providing type-safe categorization
/// and automatic ordering via enum values.
/// </summary>
public enum ExampleCategory
{
    /// <summary>Basic PDF generation concepts</summary>
    Basics = 1,
    
    /// <summary>Layout and flow architecture</summary>
    Layout = 2,
    
    /// <summary>Typography and text rendering</summary>
    Typography = 3,
    
    /// <summary>PDF elements (images, links, lists, etc.)</summary>
    Elements = 4,
    
    /// <summary>Charting and data visualization</summary>
    Charting = 5,
    
    /// <summary>Security features (encryption, watermarks)</summary>
    Security = 6,
    
    /// <summary>Real-world template examples</summary>
    Templates = 7,
    
    /// <summary>Integration scenarios (async, web API)</summary>
    Integration = 8
}
