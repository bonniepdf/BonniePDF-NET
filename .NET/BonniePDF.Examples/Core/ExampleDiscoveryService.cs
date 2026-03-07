using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BonniePDF.Examples.Core;

/// <summary>
/// Discovers and organizes ExampleBase instances using reflection.
/// </summary>
public static class ExampleDiscoveryService
{
    private static readonly Lazy<IReadOnlyList<ExampleBase>> _discoveredExamples 
        = new(DiscoverExamples);
    
    /// <summary>
    /// Gets all discovered examples, cached for performance.
    /// </summary>
    public static IReadOnlyList<ExampleBase> AllExamples => _discoveredExamples.Value;
    
    /// <summary>
    /// Gets examples organized by category.
    /// </summary>
    public static ILookup<ExampleCategory, ExampleBase> ExamplesByCategory 
        => AllExamples.ToLookup(e => e.Category);
    
    /// <summary>
    /// Finds an example by its ID.
    /// </summary>
    public static ExampleBase? FindById(string id)
    {
        return AllExamples.FirstOrDefault(e => 
            string.Equals(e.Id, id, StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Discovers all ExampleBase types in the assembly and instantiates them.
    /// </summary>
    private static List<ExampleBase> DiscoverExamples()
    {
        var examples = new List<ExampleBase>();
        
        // Get the assembly containing ExampleBase
        var assembly = typeof(ExampleBase).Assembly;
        
        // Find all concrete types that inherit from ExampleBase
        var exampleTypes = assembly.GetTypes()
            .Where(t => t.IsClass && 
                       !t.IsAbstract && 
                       typeof(ExampleBase).IsAssignableFrom(t));
        
        foreach (var type in exampleTypes)
        {
            try
            {
                // Instantiate the example (requires parameterless constructor)
                var example = (ExampleBase)Activator.CreateInstance(type)!;
                examples.Add(example);
            }
            catch (Exception ex)
            {
                // Log or handle instantiation failures
                Console.WriteLine($"Warning: Failed to instantiate {type.Name}: {ex.Message}");
            }
        }
        
        // Sort by category enum value, then by Order property (if specified), then by ID
        return examples
            .OrderBy(e => (int)e.Category)
            .ThenBy(e => e.Order ?? int.MaxValue)
            .ThenBy(e => e.Id)
            .ToList();
    }
}
