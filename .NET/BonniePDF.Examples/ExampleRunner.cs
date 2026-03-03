using System;
using System.Collections.Generic;
using BonniePDF.Examples.Examples._01_Basics;
using BonniePDF.Examples.Examples._02_Layout;
using BonniePDF.Examples.Examples._03_Typography;
using BonniePDF.Examples.Examples._04_Elements;
using BonniePDF.Examples.Examples._05_Charting;
using BonniePDF.Examples.Examples._05_Charting.Components;
using BonniePDF.Examples.Examples._06_Security;
using BonniePDF.Examples.Examples._07_Templates;
using BonniePDF.Examples.Examples._08_Integration;

namespace BonniePDF.Examples;

/// <summary>
/// Provides a centralized, robust registry for executing BonniePDF examples by ID.
/// </summary>
public static class ExampleRunner
{
    private static readonly Dictionary<string, Action<string>> _examples = new(StringComparer.OrdinalIgnoreCase)
    {
        // 01 Basics
        { "MarginsAndUnits", path => MarginExample.Generate(path) },
        { "PageSettings", path => AllPagesExample.GenerateAllPageSizes(path) },
        { "SimpleDocument", path => SimpleDocumentExample.Generate(path) },

        // 02 Layout
        { "HorizontalStacks", path => HorizontalStackExample.Generate(path) },
        { "MultiPageFlow", path => FlowArchitectureExample.GenerateComprehensiveFlowDemo(path) },

        // 03 Typography
        { "BookPageLayout", path => BookPageExample.Generate(path) },
        { "CustomFonts", path => EmbeddedFontExample.Generate(path) },
        { "EmojisAndUnicode", path => EmojiExample.Generate(path) },
        { "RichTextStyles", path => RichTextExample.Generate(path) },
        { "TextAlignment", path => JustifiedTextExample.Generate(path) },
        { "TextDecorations", path => TextDecorationExample.Generate(path) },

        // 04 Elements
        { "Hyperlinks", path => HyperlinkExample.Generate(path) },
        { "Images", path => ImageExample.Generate(path) },
        { "Lists", path => ListExample.Generate(path) },
        { "ShapesAndBorders", path => RoundedRectangleExample.Generate(path) },

        // 05 Charting
        { "ChartShowcase", path => ChartingExample.Generate(path) },
        { "AreaChartExample", path => AreaChartExample.Generate(path) },
        { "BarChartExample", path => BarChartExample.Generate(path) },
        { "DotPlotChartExample", path => DotPlotChartExample.Generate(path) },
        { "GroupedBarChartExample", path => GroupedBarChartExample.Generate(path) },
        { "LineChartExample", path => LineChartExample.Generate(path) },
        { "LollipopChartExample", path => LollipopChartExample.Generate(path) },
        { "PieChartExample", path => PieChartExample.Generate(path) },

        // 06 Security
        { "Encryption", path => EncryptionExample.GeneratePasswordProtected(path) },
        { "Watermarking", path => WatermarkExample.GenerateDraft(path) },

        // 07 Templates
        { "BusinessInvoice", path => InvoiceExample.Generate(path) },
        { "MovieCatalog", path => MoviesTableExample.Generate(path) },
        { "UserDirectory", path => UsersTableExample.Generate(path) },
        
        // 08 Integration
        { "AsyncGeneration", path => AsyncExample.GenerateAsync(path).GetAwaiter().GetResult() },
        { "WebApiStream", path => { System.IO.File.WriteAllBytes(path, WebApiExample.GeneratePdfForWebApiAsync().GetAwaiter().GetResult()); } }
    };

    /// <summary>
    /// Executes the specified example and outputs the PDF to the given path.
    /// </summary>
    public static void GenerateExample(string exampleId, string outputPath)
    {
        if (_examples.TryGetValue(exampleId, out var generateAction))
        {
            generateAction(outputPath);
        }
        else
        {
            throw new ArgumentException($"Example with ID '{exampleId}' was not found in the registry.");
        }
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
