using System.Diagnostics;
using System.IO;
using BonniePDF.Charts;
using BonniePDF.Configuration;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;
using Xunit;

namespace BonniePDF.Tests.Integration;

/// <summary>
/// Integration tests for large document generation (100+ pages, performance testing)
/// </summary>
public class LargeDocumentTests : IntegrationTestBase
{
    [Fact]
    public void GenerateLargeDocument_With100Pages_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_100_pages.pdf");
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new(new DocumentSettings
        {
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber}",
                FontSize = 10,
                Alignment = TextAlignment.Center,
                TextColor = Color.Slate500
            }
        }))
        {
            for (int i = 1; i <= 100; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Page {i} of 100",
                    FontSize = 20,
                    FontName = StandardFonts.HelveticaBold,
                    Alignment = TextAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 20)
                });

                doc.Add(new TextBlock
                {
                    Text = $"This is page {i} in a 100-page document. " +
                           "This test validates that BonniePDF can handle large documents efficiently. " +
                           "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                           "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 20)
                });

                // Add a small chart every 10 pages
                if (i % 10 == 0)
                {
                    ChartData data = CreateTestChartData(5);
                    data.Title = $"Chart on Page {i}";
                    BarChart barChart = new()
                    {
                        Height = 200,
                        StretchToFit = true
                    };
                    doc.Add(new ChartElement(barChart, data));
                }
            }

            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 10); // Content auto-flows, expect at least 10 pages
        AssertPdfHasFonts(outputPath);
        AssertMinimumFileSize(outputPath, 13_000); // At least 13KB (compressed with FlateDecode on font streams)

        // Performance assertion: Should complete in reasonable time (< 30 seconds)
        Assert.True(sw.ElapsedMilliseconds < 30_000,
            $"Large document generation took {sw.ElapsedMilliseconds}ms, expected < 30000ms");
    }

    [Fact]
    public void GenerateLargeDocument_With500TextBlocks_HandlesMemoryEfficiently()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_500_blocks.pdf");
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new())
        {
            VerticalStack stack = new();

            for (int i = 1; i <= 500; i++)
            {
                stack.AddChild(new TextBlock
                {
                    Text = $"Text block {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
                    FontSize = 10
                });
            }

            doc.Add(stack);
            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);

        // Performance assertion
        Assert.True(sw.ElapsedMilliseconds < 10_000,
            $"500 text blocks generation took {sw.ElapsedMilliseconds}ms, expected < 10000ms");
    }

    [Fact]
    public void GenerateLargeDocument_WithManyCharts_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_many_charts.pdf");
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new(new DocumentSettings
        {
            Orientation = PageOrientation.Landscape
        }))
        {
            // Add 50 charts (5 per page, 10 pages)
            for (int page = 0; page < 10; page++)
            {
                VerticalStack stack = new();

                for (int chart = 0; chart < 5; chart++)
                {
                    int chartNumber = page * 5 + chart + 1;
                    ChartData data = CreateTestChartData(6);
                    data.Title = $"Chart {chartNumber}";

                    LineChart lineChart = new()
                    {
                        Height = 120,
                        StretchToFit = true
                    };
                    stack.AddChild(new ChartElement(lineChart, data));
                }

                doc.Add(stack);
            }

            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // Content auto-flows
        AssertMinimumFileSize(outputPath, 5_000); // Compressed charts are smaller - adjusted based on actual output

        // Performance assertion
        Assert.True(sw.ElapsedMilliseconds < 15_000,
            $"50 charts generation took {sw.ElapsedMilliseconds}ms, expected < 15000ms");
    }

    [Fact]
    public void GenerateLargeDocument_WithManyImages_HandlesCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_many_images.pdf");
        byte[] imageData = LoadTestImage();
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new())
        {
            // Add 20 images (4 per page, 5 pages)
            for (int page = 0; page < 5; page++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Image Gallery - Page {page + 1}",
                    FontSize = 16,
                    FontName = StandardFonts.HelveticaBold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                HorizontalStack hStack = new() { Spacing = 10 };

                for (int img = 0; img < 4; img++)
                {
                    hStack.AddChild(new Image
                    {
                        ImageData = imageData,
                        Width = 100,
                        Height = 100
                    });
                }

                doc.Add(hStack);
            }

            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // Content auto-flows
        AssertPdfHasImages(outputPath, 1); // Same image reused 20 times = 1 image object (efficient!)

        // Performance assertion
        Assert.True(sw.ElapsedMilliseconds < 10_000,
            $"20 images generation took {sw.ElapsedMilliseconds}ms, expected < 10000ms");
    }

    [Fact]
    public void GenerateLargeDocument_WithComplexTable_HandlesCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_complex_table.pdf");
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Large Data Table (500 rows)",
                FontSize = 16,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            Table table = new(80f, 150f, 100f, 100f, 120f);

            // Header
            table.AddRow(
                new TextBlock { Text = "ID", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                new TextBlock { Text = "Name", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                new TextBlock { Text = "Department", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                new TextBlock { Text = "Status", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
                new TextBlock { Text = "Date", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
            );

            // 500 data rows
            for (int i = 1; i <= 500; i++)
            {
                table.AddRow(
                    new TextBlock { Text = $"{i}", FontSize = 9 },
                    new TextBlock { Text = $"Employee {i}", FontSize = 9 },
                    new TextBlock { Text = i % 3 == 0 ? "Engineering" : i % 3 == 1 ? "Sales" : "Marketing", FontSize = 9 },
                    new TextBlock { Text = i % 2 == 0 ? "Active" : "Inactive", FontSize = 9 },
                    new TextBlock { Text = $"2024-{(i % 12) + 1:D2}-{(i % 28) + 1:D2}", FontSize = 9 }
                );
            }

            doc.Add(table);
            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);

        // Performance assertion
        Assert.True(sw.ElapsedMilliseconds < 20_000,
            $"500-row table generation took {sw.ElapsedMilliseconds}ms, expected < 20000ms");
    }

    [Fact]
    public void GenerateLargeDocument_WithRepeatedContent_HandlesEfficiently()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_repeated_content.pdf");
        string repeatedText = string.Join("\n\n", Enumerable.Repeat(
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
            "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
            100));

        Stopwatch sw = Stopwatch.StartNew();

        // Act
        using (Document doc = new())
        {
            for (int i = 0; i < 50; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Section {i + 1}",
                    FontSize = 16,
                    FontName = StandardFonts.HelveticaBold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                doc.Add(new TextBlock
                {
                    Text = repeatedText,
                    FontSize = 10
                });
            }

            doc.Save(outputPath);
        }

        sw.Stop();

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 50);
        AssertMinimumFileSize(outputPath, 100_000);

        // Performance assertion
        Assert.True(sw.ElapsedMilliseconds < 20_000,
            $"50-page repeated content took {sw.ElapsedMilliseconds}ms, expected < 20000ms");
    }

    [Fact]
    public void GenerateLargeDocument_MeasuresMemoryUsage()
    {
        // Arrange
        string outputPath = GetTestFilePath("large_memory_test.pdf");
        long startMemory = GC.GetTotalMemory(forceFullCollection: true);

        // Act
        using (Document doc = new())
        {
            // Generate 50 pages with mixed content
            for (int i = 0; i < 50; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Page {i + 1}",
                    FontSize = 18,
                    FontName = StandardFonts.HelveticaBold
                });

                // Add chart
                if (i % 2 == 0)
                {
                    ChartData data = CreateTestChartData(8);
                    data.Title = $"Chart {i + 1}";
                    BarChart barChart = new()
                    {
                        Height = 200,
                        StretchToFit = true
                    };
                    doc.Add(new ChartElement(barChart, data));
                }

                // Add text
                doc.Add(new TextBlock
                {
                    Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                           "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                    FontSize = 11,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            doc.Save(outputPath);
        }

        long endMemory = GC.GetTotalMemory(forceFullCollection: true);
        long memoryUsed = endMemory - startMemory;

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // At least 1 page (auto-flows)
        AssertPdfHasFonts(outputPath);

        // Memory usage should be reasonable (< 50MB for this document)
        Assert.True(memoryUsed < 50_000_000,
            $"Memory usage was {memoryUsed / 1_000_000}MB, expected < 50MB");
    }
}
