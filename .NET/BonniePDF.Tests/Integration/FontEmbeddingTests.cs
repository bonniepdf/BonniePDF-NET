using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Integration;

/// <summary>
/// Integration tests for font embedding and text extraction
/// </summary>
public class FontEmbeddingTests : IntegrationTestBase
{
    [Fact]
    public void GenerateDocument_WithStandardFonts_EmbedsCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("standard_fonts.pdf");

        // Act
        using (Document doc = new())
        {
            // Test all standard fonts
            string[] standardFonts = new[]
            {
                StandardFonts.Helvetica,
                StandardFonts.HelveticaBold,
                StandardFonts.HelveticaOblique,
                StandardFonts.HelveticaBoldOblique,
                StandardFonts.TimesRoman,
                StandardFonts.TimesBold,
                StandardFonts.TimesItalic,
                StandardFonts.TimesBoldItalic,
                StandardFonts.Courier,
                StandardFonts.CourierBold,
                StandardFonts.CourierOblique,
                StandardFonts.CourierBoldOblique
            };

            foreach (string font in standardFonts)
            {
                doc.Add(new TextBlock
                {
                    Text = $"This is {font}",
                    FontName = font,
                    FontSize = 12,
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
        AssertPdfContains(outputPath, "Helvetica", "Times", "Courier");
    }

    [Fact]
    public void GenerateDocument_WithVariousTextSizes_RendersCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("text_sizes.pdf");

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Font Size Demonstration",
                FontSize = 24,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            int[] fontSizes = { 8, 10, 12, 14, 16, 18, 20, 24, 28, 32, 36, 48 };
            foreach (int size in fontSizes)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Font size {size}pt - The quick brown fox jumps over the lazy dog",
                    FontSize = size,
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
        AssertPdfContains(outputPath, "Font Size Demonstration");
    }

    [Fact]
    public void GenerateDocument_WithUnicodeCharacters_RendersCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("unicode_text.pdf");

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Unicode Character Support",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 20)
            });

            // Various Unicode characters
            doc.Add(new TextBlock
            {
                Text = "Basic Latin: ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Add(new TextBlock
            {
                Text = "Numbers and Symbols: 0123456789 !@#$%^&*()_+-=[]{}|;:',.<>?/~`",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Add(new TextBlock
            {
                Text = "Currency: $ € £ ¥ ¢",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Add(new TextBlock
            {
                Text = "Math: ± × ÷ ≈ ≠ ≤ ≥",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateDocument_WithLongText_HandlesTextWrapping()
    {
        // Arrange
        string outputPath = GetTestFilePath("text_wrapping.pdf");
        string longText = string.Join(" ", Enumerable.Repeat(
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            20));

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Text Wrapping Test",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 10)
            });

            doc.Add(new TextBlock
            {
                Text = longText,
                FontSize = 11,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
        AssertMinimumFileSize(outputPath, 1_000); // Should have content (text compresses well)
    }

    [Fact]
    public void GenerateDocument_WithMultipleTextAlignments_RendersCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("text_alignments.pdf");

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Text Alignment Examples",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 30)
            });

            doc.Add(new TextBlock
            {
                Text = "Left-aligned text: This text should be aligned to the left margin of the page.",
                FontSize = 12,
                Alignment = TextAlignment.Left,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Add(new TextBlock
            {
                Text = "Center-aligned text: This text should be centered on the page.",
                FontSize = 12,
                Alignment = TextAlignment.Center,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Add(new TextBlock
            {
                Text = "Right-aligned text: This text should be aligned to the right margin of the page.",
                FontSize = 12,
                Alignment = TextAlignment.Right,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Add(new TextBlock
            {
                Text = "Justified text: This is a longer paragraph that will be justified across the full width " +
                       "of the page. Each line except the last should extend from the left margin to the right margin, " +
                       "creating a clean, professional appearance commonly used in books and formal documents.",
                FontSize = 12,
                Alignment = TextAlignment.Justify,
                Margin = new Thickness(0, 10, 0, 10)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateDocument_WithColoredText_RendersCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("colored_text.pdf");

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Colored Text Examples",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            });

            Color[] colors = new[]
            {
                Color.Red,
                Color.Green500,
                Color.Blue,
                Color.Orange500,
                Color.Purple700,
                Color.Teal600,
                Color.Pink500
            };

            string[] colorNames = { "Red", "Green", "Blue", "Orange", "Purple", "Teal", "Pink" };

            for (int i = 0; i < colors.Length; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"This text is {colorNames[i]}",
                    FontSize = 14,
                    TextColor = colors[i],
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
    }

    [Fact]
    public void GenerateDocument_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        string outputPath = GetTestFilePath("special_characters.pdf");

        // Act
        using (Document doc = new())
        {
            doc.Add(new TextBlock
            {
                Text = "Special Characters Test",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // Parentheses (need escaping in PDF strings)
            doc.Add(new TextBlock
            {
                Text = "Parentheses: (test) and ((nested))",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // Backslashes
            doc.Add(new TextBlock
            {
                Text = @"Backslashes: \ and \\ and \\\",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // Line breaks and tabs
            doc.Add(new TextBlock
            {
                Text = "Line breaks:\nNew line\nAnother line",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            // Quotes
            doc.Add(new TextBlock
            {
                Text = "Quotes: \"Double quotes\" and 'Single quotes'",
                FontSize = 12,
                Margin = new Thickness(0, 5, 0, 5)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfHasFonts(outputPath);
    }
}
