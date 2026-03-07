using System.Linq;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;

namespace BonniePDF.Examples.Examples._02_Layout;

/// <summary>
/// Generates a comprehensive document demonstrating the flow architecture with:
/// - A long table with repeating headers on each page
/// - Followed by a long text section that flows across pages
/// </summary>
public class MultiPageFlowExample : ExampleBase
{
    public override string Id => "MultiPageFlow";
    public override string Title => "Multi-Page Flow";
    public override string Description => "Shows how elements seamlessly flow across multiple pages.";
    public override string FileName => "flow_demo.pdf";
    public override ExampleCategory Category => ExampleCategory.Layout;
    
    public override string Generate(string outputPath)
    {
        // IMPORTANT: Set orientation in DocumentSettings BEFORE creating the Document
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 50
        };

        using Document doc = new(settings);
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Flow Architecture Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates tables with repeating headers and text overflow";

        // === SECTION 1: Document Title ===
        doc.Add(new TextBlock
        {
            Text = "Employee Directory & Company Overview",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            TextColor = Color.Slate800,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "Annual Report 2024",
            FontName = StandardFonts.Helvetica,
            FontSize = 14,
            TextColor = Color.Slate500,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 0, 0, 30)
        });

        // === SECTION 2: Large Table with Employee Data ===
        doc.Add(new TextBlock
        {
            Text = "Part 1: Complete Employee Directory",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 0, 0, 15)
        });

        doc.Add(new TextBlock
        {
            Text =
                "The following table lists all employees across all departments. Headers repeat on each page for easy reference.",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 15)
        });

        // Create a table with 5 columns
        Table table = new(0.5f, 1.5f, 1f, 1f, 1f)
        {
            RepeatHeadersOnNewPage = true,
            HeaderBackgroundColor = Color.Blue600,
            HeaderTextColor = Color.White,
            CellPadding = 8
        };

        // Add header row
        table.AddHeader(
            new TextBlock { Text = "ID", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Full Name", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Department", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Position", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Start Date", FontName = StandardFonts.HelveticaBold, FontSize = 10 }
        );

        // Sample data for generating realistic employee records
        string[] firstNames = new[]
        {
            "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "David", "Elizabeth",
            "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen"
        };
        string[] lastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin"
        };
        string[] departments = new[]
        {
            "Engineering", "Marketing", "Sales", "Human Resources", "Finance", "Operations", "Customer Support",
            "Research & Development", "Legal", "IT"
        };
        string[] positions = new[]
        {
            "Manager", "Senior Analyst", "Developer", "Coordinator", "Specialist", "Director", "Associate", "Lead",
            "Consultant", "Administrator"
        };

        // Add 150 employee rows to ensure multiple pages
        for (int i = 1; i <= 150; i++)
        {
            string firstName = firstNames[(i - 1) % firstNames.Length];
            string lastName = lastNames[i * 7 % lastNames.Length];
            string department = departments[(i - 1) % departments.Length];
            string position = positions[i * 3 % positions.Length];
            int startYear = 2015 + i % 10;
            int startMonth = i * 5 % 12 + 1;

            // Alternate row colors for readability
            Color rowBgColor = i % 2 == 0 ? Color.Slate50 : Color.White;

            table.AddRow(
                TableRow.From(
                    new TextBlock
                    {
                        Text = i.ToString("D4"), FontName = StandardFonts.Helvetica, FontSize = 9,
                        TextColor = Color.Slate600
                    },
                    new TextBlock
                    {
                        Text = $"{firstName} {lastName}", FontName = StandardFonts.Helvetica, FontSize = 9,
                        TextColor = Color.Slate800
                    },
                    new TextBlock
                    {
                        Text = department, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate600
                    },
                    new TextBlock
                    {
                        Text = position, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate600
                    },
                    new TextBlock
                    {
                        Text = $"{startMonth:D2}/{startYear}", FontName = StandardFonts.Helvetica, FontSize = 9,
                        TextColor = Color.Slate500
                    }
                ).WithBackground(rowBgColor)
            );
        }

        doc.Add(table);

        // === SECTION 3: Long Text Content After Table ===
        doc.Add(new TextBlock
        {
            Text = "Part 2: Company Overview & History",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 30, 0, 15)
        });

        // Generate extensive text content that will flow across multiple pages
        (string, string)[] sections = new[]
        {
            ("About Our Company",
                "Founded in 2005, our company has grown from a small startup to a global enterprise with operations in over 40 countries. Our commitment to innovation and excellence has driven our success, and we continue to push the boundaries of what's possible in our industry. With a workforce of dedicated professionals, we deliver solutions that make a real difference in people's lives."),
            ("Our Mission",
                "We are dedicated to creating sustainable value for our stakeholders through innovation, operational excellence, and responsible business practices. Our mission guides every decision we make, from product development to customer service. We believe that by staying true to our values, we can achieve lasting success while making a positive impact on society."),
            ("Corporate Values",
                "Integrity, innovation, and inclusivity form the foundation of our corporate culture. We believe in doing the right thing, even when no one is watching. We encourage creative thinking and embrace new ideas. We celebrate diversity and create an environment where everyone can thrive and contribute their unique perspectives."),
            ("Strategic Vision",
                "Looking ahead to 2030, we envision a future where technology seamlessly integrates with everyday life to enhance human potential. Our strategic initiatives focus on emerging markets, sustainable technologies, and digital transformation. We are investing heavily in research and development to stay ahead of industry trends and meet evolving customer needs."),
            ("Innovation & Research",
                "Our R&D team comprises world-class scientists, engineers, and designers who are passionate about solving complex problems. We allocate significant resources to research initiatives that explore new frontiers in technology. Our innovation labs around the world collaborate on breakthrough projects that will shape the future of our industry."),
            ("Sustainability Commitment",
                "Environmental responsibility is not just a corporate policy—it's a core value that influences everything we do. We have committed to achieving carbon neutrality by 2030 and have implemented comprehensive sustainability programs across all our facilities. From renewable energy adoption to waste reduction initiatives, we are taking concrete steps to protect our planet."),
            ("Community Engagement",
                "We believe in giving back to the communities where we operate. Through our foundation and employee volunteer programs, we support education, healthcare, and environmental initiatives around the world. Our employees donate thousands of hours each year to causes they care about, and we match their contributions through our corporate giving program."),
            ("Financial Performance",
                "Despite challenging market conditions, our company has demonstrated remarkable resilience and adaptability. We have maintained strong revenue growth while improving operational efficiency. Our diversified portfolio and disciplined approach to capital allocation have positioned us well for continued success in the years ahead."),
            ("Employee Development",
                "Our people are our greatest asset. We invest significantly in training and development programs to help our employees reach their full potential. From leadership academies to technical certification programs, we provide numerous opportunities for career growth. Our commitment to employee well-being extends to comprehensive benefits, flexible work arrangements, and wellness initiatives."),
            ("Customer Focus",
                "Customer satisfaction is at the heart of everything we do. We continuously gather feedback and use it to improve our products and services. Our customer success teams work closely with clients to understand their needs and deliver solutions that exceed expectations. We measure our success not just by financial metrics, but by the value we create for our customers.")
        };

        foreach ((string title, string content) in sections)
        {
            doc.Add(new TextBlock
            {
                Text = title,
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                TextColor = Color.Slate700,
                Margin = new Thickness(0, 20, 0, 8)
            });

            // Add the main content paragraph
            doc.Add(new TextBlock
            {
                Text = content,
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
                TextColor = Color.Slate600,
                Margin = new Thickness(0, 0, 0, 8)
            });

            // Add additional detail paragraph to ensure more content
            doc.Add(new TextBlock
            {
                Text = $"This section highlights the importance of {title.ToLower()} in our organization. " +
                       "We continuously evaluate our practices and seek opportunities for improvement. " +
                       "Through regular assessments and stakeholder engagement, we ensure that our strategies " +
                       "remain relevant and effective in an ever-changing business environment. " +
                       "The leadership team reviews progress quarterly and adjusts our approach as needed " +
                       "to maintain alignment with our long-term objectives.",
                FontName = StandardFonts.Helvetica,
                FontSize = 11,
                TextColor = Color.Slate600,
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        // === SECTION 4: Closing Remarks ===
        doc.Add(new TextBlock
        {
            Text = "Conclusion",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 18,
            TextColor = Color.Slate700,
            Margin = new Thickness(0, 30, 0, 15)
        });

        doc.Add(new TextBlock
        {
            Text = "This document demonstrates the powerful pagination capabilities of BonniePDF. " +
                   "The employee table automatically spans multiple pages with headers repeating on each page, " +
                   "making it easy to reference column names regardless of where you are in the document. " +
                   "The text sections that follow flow naturally across page boundaries, maintaining readability " +
                   "and professional formatting throughout. This unified approach to content flow enables the " +
                   "creation of complex, multipage documents with consistent quality and appearance.",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600,
            Margin = new Thickness(0, 0, 0, 20)
        });

        doc.Add(new TextBlock
        {
            Text = "Generated with BonniePDF - Flow Architecture Demo",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 10,
            TextColor = Color.Slate400,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 20, 0, 0)
        });

        doc.Save(outputPath);
        return outputPath;
    }
}
