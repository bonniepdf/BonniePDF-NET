using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Examples.Core;
using BonniePDF.Examples.Models;

namespace BonniePDF.Examples;

/// <summary>
/// Provides a centralized registry for executing BonniePDF examples.
/// Refactored to use reflection-based discovery.
/// </summary>
public static class ExampleRunner
{
    private static readonly Lazy<List<Models.ExampleCategory>> _categories 
        = new(BuildCategories);
    
    /// <summary>
    /// Gets all example categories with their examples, ordered by category.
    /// </summary>
    public static IList<Models.ExampleCategory> Categories => _categories.Value;
    
    /// <summary>
    /// Builds the category list from discovered examples.
    /// Maintains backward compatibility with existing ExampleCategory model.
    /// </summary>
    private static List<Models.ExampleCategory> BuildCategories()
    {
        var categoryList = new List<Models.ExampleCategory>();
        
        // Group discovered examples by category
        var grouped = ExampleDiscoveryService.ExamplesByCategory;
        
        foreach (var group in grouped.OrderBy(g => (int)g.Key))
        {
            var category = new Models.ExampleCategory
            {
                Id = ((int)group.Key).ToString("D2"),
                Name = GetCategoryDisplayName(group.Key),
                Examples = new List<ExampleInfo>()
            };
            
            // Add examples to the category
            foreach (var example in group)
            {
                category.Examples.Add(new ExampleInfo
                {
                    Id = example.Id,
                    Title = example.Title,
                    Description = example.Description,
                    FileName = example.FileName,
                    Password = example.Password,
                    AdditionalInfo = example.AdditionalInfo,
                    GenerateAction = path => example.Generate(path)
                });
            }
            
            categoryList.Add(category);
        }
        
        return categoryList;
    }
    
    /// <summary>
    /// Gets the display name for a category enum value.
    /// </summary>
    private static string GetCategoryDisplayName(Core.ExampleCategory category)
    {
        return category switch
        {
            Core.ExampleCategory.Basics => "Basics",
            Core.ExampleCategory.Layout => "Layout",
            Core.ExampleCategory.Typography => "Typography",
            Core.ExampleCategory.Elements => "Elements",
            Core.ExampleCategory.Charting => "Charting",
            Core.ExampleCategory.Security => "Security",
            Core.ExampleCategory.Templates => "Templates",
            Core.ExampleCategory.Integration => "Integration",
            _ => category.ToString()
        };
    }
    
    /// <summary>
    /// Executes the specified example and outputs the PDF to the given path.
    /// </summary>
    public static void GenerateExample(string exampleId, string outputPath)
    {
        var example = ExampleDiscoveryService.FindById(exampleId);
        
        if (example == null)
        {
            throw new ArgumentException($"Example with ID '{exampleId}' was not found.");
        }
        
        example.Generate(outputPath);
    }
    
    /// <summary>
    /// Executes the specified example asynchronously and outputs the PDF to the given path.
    /// </summary>
    public static async System.Threading.Tasks.Task<string> GenerateExampleAsync(
        string exampleId, 
        string outputPath)
    {
        var example = ExampleDiscoveryService.FindById(exampleId);
        
        if (example == null)
        {
            throw new ArgumentException($"Example with ID '{exampleId}' was not found.");
        }
        
        return await example.GenerateAsync(outputPath);
    }
    
    /// <summary>
    /// Executes the specified example and returns the PDF as a byte array.
    /// </summary>
    public static byte[] GenerateExampleToBytes(string exampleId)
    {
        var tempFile = System.IO.Path.GetTempFileName() + ".pdf";
        try
        {
            GenerateExample(exampleId, tempFile);
            return System.IO.File.ReadAllBytes(tempFile);
        }
        finally
        {
            if (System.IO.File.Exists(tempFile))
            {
                try { System.IO.File.Delete(tempFile); } catch { }
            }
        }
    }
}
