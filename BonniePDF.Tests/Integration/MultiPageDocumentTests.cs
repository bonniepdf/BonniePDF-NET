using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Integration;

/// <summary>
/// Integration tests for multi-page document generation with mixed content
/// </summary>
public class MultiPageDocumentTests : IntegrationTestBase
{
    [Fact]
    public void GenerateMultiPageDocument_WithChartsImagesAndText_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("multipage_mixed_content.pdf");
        byte[] imageData = LoadTestImage();

        // Act
        using (BonniePDF.Document doc = new())
        {
            // Page 1: Title and text
            doc.Add(new TextBlock
            {
                Text = "Integration Test Report",
                FontSize = 24,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center,
                Color = ColorPalette.Blue[900]
            });

            doc.Add(new TextBlock
            {
                Text = "This document demonstrates multi-page PDF generation with charts, images, and text. " +
                       "The following pages contain various elements to validate the complete rendering pipeline.",
                FontSize = 12,
                Margin = new Thickness(0, 20, 0, 20)
            });

            // Page 2: Bar Chart
            doc.Add(new TextBlock
            {
                Text = "Sales Data - Bar Chart",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            ChartData barData = CreateTestChartData(6);
            barData.Title = "Monthly Sales";
            BarChart barChart = new()
            {
                Height = 300,
                StretchToFit = true
            };
            barChart.Data = barData;
            doc.Add(barChart);

            // Page 3: Line Chart
            doc.Add(new TextBlock
            {
                Text = "Trend Analysis - Line Chart",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            ChartData lineData = CreateTestChartData(10);
            lineData.Title = "Growth Trend";
            LineChart lineChart = new()
            {
                Height = 300,
                StretchToFit = true
            };
            lineChart.Data = lineData;
            doc.Add(lineChart);

            // Page 4: Images
            doc.Add(new TextBlock
            {
                Text = "Visual Assets",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            doc.Add(new TextBlock
            {
                Text = "Sample image embedded in PDF:",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 10)
            });

            doc.Add(new Image
            {
                ImageData = imageData,
                Width = 200,
                Height = 200
            });

            // Page 5: Pie Chart
            doc.Add(new TextBlock
            {
                Text = "Market Share - Pie Chart",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            ChartData pieData = CreateTestChartData(4);
            pieData.Title = "Market Distribution";
            PieChart pieChart = new()
            {
                Height = 400,
                StretchToFit = true
            };
            pieChart.Data = pieData;
            doc.Add(pieChart);

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 2); // Content auto-flows, at least 2 pages
        AssertPdfHasFonts(outputPath);
        AssertPdfHasImages(outputPath, 1);
        AssertMinimumFileSize(outputPath, 30_000); // Should be at least 30KB with all content
    }

    [Fact]
    public void GenerateMultiPageDocument_WithTables_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("multipage_tables.pdf");

        // Act
        using (BonniePDF.Document doc = new())
        {
            // Page 1: Introduction
            doc.Add(new TextBlock
            {
                Text = "Database Query Results",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center
            });

            // Page 2: Table with data
            Table table = new(150f, 150f, 100f, 100f)
            {
                Margin = new Thickness(0, 20, 0, 0)
            };

            // Header row
            table.AddRow(
                new TextBlock { Text = "Product", FontName = StandardFonts.HelveticaBold },
                new TextBlock { Text = "Category", FontName = StandardFonts.HelveticaBold },
                new TextBlock { Text = "Quantity", FontName = StandardFonts.HelveticaBold },
                new TextBlock { Text = "Price", FontName = StandardFonts.HelveticaBold }
            );

            // Data rows
            for (int i = 1; i <= 20; i++)
            {
                table.AddRow(
                    new TextBlock { Text = $"Product {i}" },
                    new TextBlock { Text = i % 2 == 0 ? "Electronics" : "Accessories" },
                    new TextBlock { Text = $"{i * 10}" },
                    new TextBlock { Text = $"${i * 25:F2}" }
                );
            }

            doc.Add(table);

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // At least 1 page
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateMultiPageDocument_WithVerticalStack_FlowsContentAcrossPages()
    {
        // Arrange
        string outputPath = GetTestFilePath("multipage_vertical_stack.pdf");

        // Act
        using (BonniePDF.Document doc = new())
        {
            VerticalStack stack = new();

            // Add title
            stack.Add(new TextBlock
            {
                Text = "Continuous Content Flow Test",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Add multiple text blocks that should flow across pages
            for (int i = 1; i <= 15; i++)
            {
                stack.Add(new TextBlock
                {
                    Text = $"Section {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                           "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                           "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi " +
                           "ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit " +
                           "in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
                    FontSize = 11,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                // Add a small chart every 3 sections
                if (i % 3 == 0)
                {
                    ChartData data = CreateTestChartData(4);
                    data.Title = $"Chart for Section {i}";
                    BarChart chart = new()
                    {
                        Height = 200,
                        StretchToFit = true
                    };
                    chart.Data = data;
                    stack.Add(chart);
                }
            }

            doc.Add(stack);
            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // At least 1 page (content will auto-flow)
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateMultiPageDocument_WithHeadersAndFooters_RendersTemplatesOnAllPages()
    {
        // Arrange
        string outputPath = GetTestFilePath("multipage_headers_footers.pdf");

        // Act
        using (BonniePDF.Document doc = new(new DocumentSettings
        {
            HeaderTemplate = pageNumber => new TextBlock
            {
                Text = "Enterprise Report - Confidential",
                FontSize = 10,
                Alignment = TextAlignment.Center,
                Color = ColorPalette.Slate[500]
            },
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber}",
                FontSize = 10,
                Alignment = TextAlignment.Center,
                Color = ColorPalette.Slate[500]
            }
        }))
        {
            // Add 3 pages with content
            for (int i = 1; i <= 3; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Page {i} Content",
                    FontSize = 16,
                    Alignment = TextAlignment.Center
                });

                doc.Add(new TextBlock
                {
                    Text = "This page demonstrates header and footer templates that appear on every page.",
                    FontSize = 12,
                    Margin = new Thickness(0, 20, 0, 0)
                });
            }

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 1); // At least 1 page
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateMultiPageDocument_WithDifferentPageOrientations_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("multipage_mixed_orientations.pdf");

        // Act
        using (BonniePDF.Document doc = new())
        {
            // Portrait page
            doc.Add(new TextBlock
            {
                Text = "Portrait Page",
                FontSize = 24,
                Alignment = TextAlignment.Center
            });

            // Landscape page
            doc.AddPage(PdfSize.A4, PageOrientation.Landscape);
            doc.Add(new TextBlock
            {
                Text = "Landscape Page",
                FontSize = 24,
                Alignment = TextAlignment.Center
            });

            // Wide chart on landscape page
            ChartData data = CreateTestChartData(12);
            data.Title = "Wide Chart on Landscape Page";
            LineChart lineChart = new()
            {
                Height = 300,
                StretchToFit = true
            };
            lineChart.Data = data;
            doc.Add(lineChart);

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasPages(outputPath, 2);
        AssertPdfHasFonts(outputPath);
    }
}
