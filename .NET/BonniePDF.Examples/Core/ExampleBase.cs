using System.Collections.Generic;
using System.Threading.Tasks;

namespace BonniePDF.Examples.Core;

/// <summary>
/// Base class for all BonniePDF examples. Provides a common structure
/// for example metadata and execution.
/// </summary>
public abstract class ExampleBase
{
    // ========== REQUIRED METADATA (Abstract Properties) ==========
    
    /// <summary>
    /// Unique identifier for this example (e.g., "MarginsAndUnits")
    /// </summary>
    public abstract string Id { get; }
    
    /// <summary>
    /// Display title for this example (e.g., "Margins & Units")
    /// </summary>
    public abstract string Title { get; }
    
    /// <summary>
    /// Detailed description of what this example demonstrates
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// Output filename for the generated PDF (e.g., "margin_example.pdf")
    /// </summary>
    public abstract string FileName { get; }
    
    /// <summary>
    /// Category this example belongs to
    /// </summary>
    public abstract ExampleCategory Category { get; }
    
    // ========== OPTIONAL METADATA (Virtual Properties) ==========
    
    /// <summary>
    /// Optional password for encrypted PDFs. Null for non-encrypted documents.
    /// </summary>
    public virtual string? Password => null;
    
    /// <summary>
    /// Optional additional information to display after generation
    /// (e.g., "Password: demo123")
    /// </summary>
    public virtual string[]? AdditionalInfo => null;
    
    /// <summary>
    /// Optional order within category. Lower values appear first.
    /// If null, examples are ordered alphabetically by ID.
    /// </summary>
    public virtual int? Order => null;
    
    // ========== EXECUTION METHODS ==========
    
    /// <summary>
    /// Generates the example PDF synchronously.
    /// Default implementation calls GenerateAsync and awaits the result.
    /// Override this method for purely synchronous examples.
    /// </summary>
    public virtual string Generate(string outputPath)
    {
        return GenerateAsync(outputPath).GetAwaiter().GetResult();
    }
    
    /// <summary>
    /// Generates the example PDF asynchronously.
    /// Default implementation creates a completed task with Generate() result.
    /// Override this method for asynchronous examples.
    /// </summary>
    public virtual Task<string> GenerateAsync(string outputPath)
    {
        return Task.FromResult(Generate(outputPath));
    }
}
