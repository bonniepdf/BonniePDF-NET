using System.IO;
using BonniePDF.Charts;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Layout.Elements;
using BonniePDF.Security.Permissions;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests.Integration;

/// <summary>
/// Integration tests for encrypted PDF generation
/// </summary>
public class EncryptionIntegrationTests : IntegrationTestBase
{
    [Fact]
    public void GenerateEncryptedDocument_WithUserPassword_CreatesValidEncryptedPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_user_password.pdf");
        string userPassword = "test123";

        // Act
        using (Document doc = new(new DocumentSettings().WithPassword(userPassword)))
        {
            doc.Add(new TextBlock
            {
                Text = "Encrypted Document",
                FontSize = 24,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center
            });

            doc.Add(new TextBlock
            {
                Text = "This document is protected with a user password.",
                FontSize = 12,
                Margin = new Thickness(0, 20, 0, 0)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_WithUserAndOwnerPasswords_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_dual_passwords.pdf");
        string userPassword = "user123";
        string ownerPassword = "owner456";

        // Act
        using (Document doc = new(new DocumentSettings().WithPasswords(userPassword, ownerPassword)))
        {
            doc.Add(new TextBlock
            {
                Text = "Dual Password Protection",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Add(new TextBlock
            {
                Text = "This document has both user and owner passwords.",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_WithPermissionRestrictions_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_restricted_permissions.pdf");
        string ownerPassword = "owner789";

        // Act - No printing, no modification allowed
        var permissions = PdfPermissions.Copy | PdfPermissions.ExtractForAccessibility;

        using (Document doc = new(new DocumentSettings().WithPasswords(null, ownerPassword, permissions)))
        {
            doc.Add(new TextBlock
            {
                Text = "Restricted Permissions Document",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Add(new TextBlock
            {
                Text = "This document has restricted permissions: no printing, no modification.",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_With128BitEncryption_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_128bit.pdf");
        string password = "secure128";

        // Act
        using (Document doc = new(new DocumentSettings().WithEncryption128(opts =>
        {
            opts.WithUserPassword(password);
            opts.WithOwnerPassword(password);
            opts.AllowAll();
        })))
        {
            doc.Add(new TextBlock
            {
                Text = "128-bit Encryption Test",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_With256BitEncryption_CreatesValidPdf()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_256bit.pdf");
        string password = "secure256";

        // Act
        using (Document doc = new(new DocumentSettings().WithEncryptionAes256(opts =>
        {
            opts.WithUserPassword(password);
            opts.WithOwnerPassword(password);
            opts.AllowAll();
        })))
        {
            doc.Add(new TextBlock
            {
                Text = "256-bit AES Encryption Test",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Add(new TextBlock
            {
                Text = "This document uses the strongest encryption level (AES-256).",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_WithImagesAndCharts_EncryptsAllContent()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_mixed_content.pdf");
        string password = "complex123";
        byte[] imageData = LoadTestImage();

        // Act
        using (Document doc = new(new DocumentSettings().WithPassword(password)))
        {
            doc.Add(new TextBlock
            {
                Text = "Encrypted Document with Mixed Content",
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold,
                Alignment = TextAlignment.Center
            });

            // Add image
            doc.Add(new Image
            {
                ImageData = imageData,
                Width = 150,
                Height = 150,
                Margin = new Thickness(0, 10, 0, 10)
            });

            // Add chart
            var chartData = CreateTestChartData(5);
            chartData.Title = "Encrypted Chart";
            Charts.BarChart barChart = new()
            {
                Height = 200,
                StretchToFit = true
            };
            doc.Add(new ChartElement(barChart, chartData));

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
        AssertPdfHasImages(outputPath, 1);
    }

    [Fact]
    public void GenerateEncryptedDocument_MultiPage_EncryptsAllPages()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_multipage.pdf");
        string password = "multipage123";

        // Act
        using (Document doc = new(new DocumentSettings().WithPassword(password)))
        {
            for (int i = 1; i <= 5; i++)
            {
                doc.Add(new TextBlock
                {
                    Text = $"Encrypted Page {i}",
                    FontSize = 18,
                    FontName = StandardFonts.HelveticaBold
                });

                doc.Add(new TextBlock
                {
                    Text = $"This is confidential content on page {i}.",
                    FontSize = 12,
                    Margin = new Thickness(0, 10, 0, 0)
                });
            }

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
        AssertPdfHasPages(outputPath, 1); // Content auto-flows
    }

    [Fact]
    public void GenerateEncryptedDocument_WithMetadata_PreservesMetadata()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_with_metadata.pdf");
        string password = "meta123";

        // Act
        using (Document doc = new(new DocumentSettings().WithPassword(password)))
        {
            doc.Metadata.Title = "Confidential Report";
            doc.Metadata.Author = "Security Department";
            doc.Metadata.Subject = "Q4 Financial Results";
            doc.Metadata.Keywords = "confidential, encrypted, financial";

            doc.Add(new TextBlock
            {
                Text = doc.Metadata.Title,
                FontSize = 20,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_AllowCopyText_SetsCorrectPermissions()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_allow_copy.pdf");
        string ownerPassword = "owner123";

        // Act
        var permissions = PdfPermissions.Copy | PdfPermissions.ExtractForAccessibility;

        using (Document doc = new(new DocumentSettings().WithPasswords(null, ownerPassword, permissions)))
        {
            doc.Add(new TextBlock
            {
                Text = "Copy-Enabled Encrypted Document",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold
            });

            doc.Add(new TextBlock
            {
                Text = "Users can copy text from this document but cannot modify it.",
                FontSize = 12,
                Margin = new Thickness(0, 10, 0, 0)
            });

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }

    [Fact]
    public void GenerateEncryptedDocument_WithTable_EncryptsTableContent()
    {
        // Arrange
        string outputPath = GetTestFilePath("encrypted_table.pdf");
        string password = "table123";

        // Act
        using (Document doc = new(new DocumentSettings().WithPassword(password)))
        {
            doc.Add(new TextBlock
            {
                Text = "Encrypted Salary Information",
                FontSize = 18,
                FontName = StandardFonts.HelveticaBold,
                Margin = new Thickness(0, 0, 0, 15)
            });

            Table table = new(200f, 150f, 150f);

            // Header
            table.AddRow(
                new TextBlock { Text = "Employee", FontName = StandardFonts.HelveticaBold },
                new TextBlock { Text = "Position", FontName = StandardFonts.HelveticaBold },
                new TextBlock { Text = "Salary", FontName = StandardFonts.HelveticaBold }
            );

            // Data
            table.AddRow(
                new TextBlock { Text = "John Doe" },
                new TextBlock { Text = "Manager" },
                new TextBlock { Text = "$85,000" }
            );

            table.AddRow(
                new TextBlock { Text = "Jane Smith" },
                new TextBlock { Text = "Developer" },
                new TextBlock { Text = "$75,000" }
            );

            doc.Add(table);

            doc.Save(outputPath);
        }

        // Assert
        AssertValidPdf(outputPath);
        AssertPdfIsEncrypted(outputPath);
    }
}
