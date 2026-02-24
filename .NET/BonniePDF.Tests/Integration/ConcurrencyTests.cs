using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using BonniePDF.Charts;
using BonniePDF.Charts.Models;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Layout.Elements;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Integration;

/// <summary>
/// Integration tests for concurrent document generation
/// </summary>
public class ConcurrencyTests : IntegrationTestBase
{
    [Fact]
    public async Task GenerateDocuments_Concurrently_AllSucceed()
    {
        // Arrange
        int documentCount = 10;
        ConcurrentBag<string> generatedFiles = new();
        ConcurrentBag<Exception> exceptions = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    string outputPath = GetTestFilePath($"concurrent_{docNum}.pdf");

                    using Document doc = new();
                    doc.Add(new TextBlock
                    {
                        Text = $"Concurrent Document {docNum}",
                        FontSize = 20,
                        FontName = StandardFonts.HelveticaBold
                    });

                    doc.Add(new TextBlock
                    {
                        Text = $"Generated in parallel with {documentCount - 1} other documents.",
                        FontSize = 12,
                        Margin = new Thickness(0, 10, 0, 0)
                    });

                    doc.Save(outputPath);
                    generatedFiles.Add(outputPath);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Empty(exceptions); // No exceptions should occur
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyWithCharts_AllSucceed()
    {
        // Arrange
        int documentCount = 5;
        ConcurrentBag<string> generatedFiles = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_charts_{docNum}.pdf");

                using Document doc = new();
                doc.Add(new TextBlock
                {
                    Text = $"Chart Document {docNum}",
                    FontSize = 18,
                    FontName = StandardFonts.HelveticaBold,
                    Margin = new Thickness(0, 0, 0, 10)
                });

                ChartData data = CreateTestChartData(6);
                data.Title = $"Chart {docNum}";
                BarChart barChart = new()
                {
                    Height = 250,
                    StretchToFit = true
                };
                doc.Add(new ChartElement(barChart, data));

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyWithImages_AllSucceed()
    {
        // Arrange
        int documentCount = 5;
        byte[] imageData = LoadTestImage();
        ConcurrentBag<string> generatedFiles = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_images_{docNum}.pdf");

                using Document doc = new();
                doc.Add(new TextBlock
                {
                    Text = $"Image Document {docNum}",
                    FontSize = 16,
                    FontName = StandardFonts.HelveticaBold
                });

                doc.Add(new Image
                {
                    ImageData = imageData,
                    Width = 200,
                    Height = 200,
                    Margin = new Thickness(0, 10, 0, 0)
                });

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
            AssertPdfHasImages(filePath, 1);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyWithEncryption_AllSucceed()
    {
        // Arrange
        int documentCount = 5;
        ConcurrentBag<string> generatedFiles = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_encrypted_{docNum}.pdf");

                using Document doc = new(new DocumentSettings().WithPassword($"password{docNum}"));
                doc.Add(new TextBlock
                {
                    Text = $"Encrypted Document {docNum}",
                    FontSize = 16,
                    FontName = StandardFonts.HelveticaBold
                });

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
            AssertPdfIsEncrypted(filePath);
        }
    }

    [Fact]
    public async Task GenerateDocuments_HighConcurrency_HandlesStress()
    {
        // Arrange
        int documentCount = 50;
        ConcurrentBag<string> generatedFiles = new();
        ConcurrentBag<Exception> exceptions = new();
        Stopwatch sw = Stopwatch.StartNew();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    string outputPath = GetTestFilePath($"stress_test_{docNum}.pdf");

                    using Document doc = new();
                    doc.Add(new TextBlock
                    {
                        Text = $"Stress Test Document {docNum}",
                        FontSize = 14
                    });

                    doc.Save(outputPath);
                    generatedFiles.Add(outputPath);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }));
        }

        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.Empty(exceptions); // No exceptions under stress
        Assert.Equal(documentCount, generatedFiles.Count);

        // Should complete in reasonable time (< 30 seconds for 50 concurrent documents)
        Assert.True(sw.ElapsedMilliseconds < 30_000,
            $"50 concurrent documents took {sw.ElapsedMilliseconds}ms, expected < 30000ms");

        // Validate a sample of the files
        int sampleSize = Math.Min(10, generatedFiles.Count);
        foreach (string filePath in generatedFiles.Take(sampleSize))
        {
            AssertValidPdf(filePath);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyToStreams_AllSucceed()
    {
        // Arrange
        int documentCount = 10;
        ConcurrentBag<byte[]> generatedPdfs = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                using MemoryStream stream = new();
                using Document doc = new();

                doc.Add(new TextBlock
                {
                    Text = $"Stream Document {docNum}",
                    FontSize = 16
                });

                doc.Save(stream, leaveOpen: true);
                generatedPdfs.Add(stream.ToArray());
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedPdfs.Count);

        foreach (byte[] pdfBytes in generatedPdfs)
        {
            Assert.True(pdfBytes.Length > 0);
            string header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 8);
            Assert.StartsWith("%PDF-", header);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyWithSharedResources_HandlesCorrectly()
    {
        // Arrange
        int documentCount = 10;
        byte[] sharedImageData = LoadTestImage();
        ChartData sharedChartData = CreateTestChartData(5);
        ConcurrentBag<string> generatedFiles = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_shared_{docNum}.pdf");

                using Document doc = new();
                doc.Add(new TextBlock
                {
                    Text = $"Shared Resources Document {docNum}",
                    FontSize = 16,
                    FontName = StandardFonts.HelveticaBold
                });

                // Use shared image
                doc.Add(new Image
                {
                    ImageData = sharedImageData,
                    Width = 150,
                    Height = 150
                });

                // Use shared chart data
                ChartData chartDataCopy = CreateTestChartData(5);
                chartDataCopy.Title = $"Shared Chart - Doc {docNum}";
                BarChart barChart = new()
                {
                    Height = 200,
                    StretchToFit = true
                };
                doc.Add(new ChartElement(barChart, chartDataCopy));

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
            AssertPdfHasImages(filePath, 1);
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyMultiPage_AllSucceed()
    {
        // Arrange
        int documentCount = 5;
        ConcurrentBag<string> generatedFiles = new();

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_multipage_{docNum}.pdf");

                using Document doc = new();

                // Add 10 pages to each document
                for (int p = 0; p < 10; p++)
                {
                    doc.Add(new TextBlock
                    {
                        Text = $"Document {docNum}, Page {p + 1}",
                        FontSize = 16,
                        FontName = StandardFonts.HelveticaBold
                    });
                }

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
            AssertPdfHasPages(filePath, 1); // Content auto-flows
        }
    }

    [Fact]
    public async Task GenerateDocuments_ConcurrentlyWithDifferentSettings_AllSucceed()
    {
        // Arrange
        int documentCount = 6;
        ConcurrentBag<string> generatedFiles = new();
        PageOrientation[] orientations = { PageOrientation.Portrait, PageOrientation.Landscape };

        // Act
        List<Task> tasks = new();
        for (int i = 0; i < documentCount; i++)
        {
            int docNum = i;
            tasks.Add(Task.Run(() =>
            {
                string outputPath = GetTestFilePath($"concurrent_settings_{docNum}.pdf");

                using Document doc = new(new DocumentSettings
                {
                    Orientation = orientations[docNum % 2]
                });

                doc.Add(new TextBlock
                {
                    Text = $"Document {docNum}: {orientations[docNum % 2]}",
                    FontSize = 16
                });

                doc.Save(outputPath);
                generatedFiles.Add(outputPath);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(documentCount, generatedFiles.Count);

        foreach (string filePath in generatedFiles)
        {
            AssertValidPdf(filePath);
        }
    }
}
