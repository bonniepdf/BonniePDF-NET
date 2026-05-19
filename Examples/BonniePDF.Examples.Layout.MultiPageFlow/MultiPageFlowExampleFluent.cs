using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.MultiPageFlow;

/// <summary>
/// Generates a comprehensive document demonstrating the flow architecture using the Fluent API:
/// a long table with repeating headers on each page, followed by a long text section.
/// </summary>
[System.ComponentModel.DisplayName("Multi-Page Flow (Fluent API)")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Shows how elements seamlessly flow across multiple pages using the Fluent API.")]
public static class MultiPageFlowExampleFluent
{
    private static readonly string[] FirstNames =
    {
        "James", "Mary", "Robert", "Patricia", "John", "Jennifer", "Michael", "Linda", "David", "Elizabeth",
        "William", "Barbara", "Richard", "Susan", "Joseph", "Jessica", "Thomas", "Sarah", "Charles", "Karen"
    };

    private static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
        "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin"
    };

    private static readonly string[] Departments =
    {
        "Engineering", "Marketing", "Sales", "Human Resources", "Finance", "Operations", "Customer Support",
        "Research & Development", "Legal", "IT"
    };

    private static readonly string[] Positions =
    {
        "Manager", "Senior Analyst", "Developer", "Coordinator", "Specialist", "Director", "Associate", "Lead",
        "Consultant", "Administrator"
    };

    /// <summary>
    /// Generates the multi-page flow example PDF and saves it to <paramref name="outputPath"/>
    /// using the Fluent API. The document illustrates a 150-row table with repeating headers
    /// and flowing text sections across multiple landscape pages.
    /// </summary>
    /// <param name="outputPath">The full file path where the PDF will be written.</param>
    /// <returns>The path of the generated PDF file.</returns>
    public static Document Generate()
    {
        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Flow Architecture Demo";
                metadata.Author = "BonniePDF";
                metadata.Subject = "Demonstrates tables with repeating headers and text overflow";
            });

            doc.WithSettings(settings =>
            {
                // IMPORTANT: Orientation must be set in DocumentSettings before the document renders
                settings.PageSize = PdfSize.A4;
                settings.Orientation = PageOrientation.Landscape;
                settings.Margin = 50;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // === SECTION 1: Document Title ===
                    column.Item()
                        .Text("Employee Directory & Company Overview")
                        .Bold()
                        .FontSize(28)
                        .TextColor(Color.Slate800)
                        .AlignCenter()
                        .Margin(0, 0, 0, 10);

                    column.Item()
                        .Text($"Annual Report {DateTime.Now.Year}")
                        .FontSize(14)
                        .TextColor(Color.Slate500)
                        .AlignCenter()
                        .Margin(0, 0, 0, 30);

                    // === SECTION 2: Large Table with Employee Data ===
                    column.Item()
                        .Text("Part 1: Complete Employee Directory")
                        .Bold()
                        .FontSize(18)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 15);

                    column.Item()
                        .Text("The following table lists all employees across all departments. Headers repeat on each page for easy reference.")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 15);

                    column.Item()
                        .Table(table =>
                        {
                            // 5-column layout matching original float widths
                            table.Columns(0.5f, 1.5f, 1f, 1f, 1f);
                            table.CellPadding(8);
                            table.HeaderBackground(Color.Blue600);
                            table.HeaderTextColor(Color.White);
                            table.RepeatHeaders();

                            table.Header(row =>
                            {
                                row.Cell().Text("ID").Bold().FontSize(10);
                                row.Cell().Text("Full Name").Bold().FontSize(10);
                                row.Cell().Text("Department").Bold().FontSize(10);
                                row.Cell().Text("Position").Bold().FontSize(10);
                                row.Cell().Text("Start Date").Bold().FontSize(10);
                            });

                            // Add 150 employee rows with alternating row background
                            for (int i = 1; i <= 150; i++)
                            {
                                string firstName = FirstNames[(i - 1) % FirstNames.Length];
                                string lastName = LastNames[i * 7 % LastNames.Length];
                                string department = Departments[(i - 1) % Departments.Length];
                                string position = Positions[i * 3 % Positions.Length];
                                int startYear = 2015 + i % 10;
                                int startMonth = i * 5 % 12 + 1;

                                Color rowBgColor = i % 2 == 0 ? Color.Slate50 : Color.White;

                                table.Row(rowBgColor, row =>
                                {
                                    row.Cell().Text(i.ToString("D4")).FontSize(9).TextColor(Color.Slate600);
                                    row.Cell().Text($"{firstName} {lastName}").FontSize(9).TextColor(Color.Slate800);
                                    row.Cell().Text(department).FontSize(9).TextColor(Color.Slate600);
                                    row.Cell().Text(position).FontSize(9).TextColor(Color.Slate600);
                                    row.Cell().Text($"{startMonth:D2}/{startYear}").FontSize(9).TextColor(Color.Slate500);
                                });
                            }
                        });

                    // === SECTION 3: Long Text Content After Table ===
                    column.Item().Spacer(30);

                    column.Item()
                        .Text("Part 2: Company Overview & History")
                        .Bold()
                        .FontSize(18)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 15);

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
                        column.Item()
                            .Text(title)
                            .Bold()
                            .FontSize(14)
                            .TextColor(Color.Slate700)
                            .Margin(0, 20, 0, 8);

                        column.Item()
                            .Text(content)
                            .TextColor(Color.Slate600)
                            .Margin(0, 0, 0, 8);

                        column.Item()
                            .Text($"This section highlights the importance of {title.ToLower()} in our organization. " +
                                  "We continuously evaluate our practices and seek opportunities for improvement. " +
                                  "Through regular assessments and stakeholder engagement, we ensure that our strategies " +
                                  "remain relevant and effective in an ever-changing business environment. " +
                                  "The leadership team reviews progress quarterly and adjusts our approach as needed " +
                                  "to maintain alignment with our long-term objectives.")
                            .TextColor(Color.Slate600)
                            .Margin(0, 0, 0, 12);
                    }

                    // === SECTION 4: Closing Remarks ===
                    column.Item().Spacer(30);

                    column.Item()
                        .Text("Conclusion")
                        .Bold()
                        .FontSize(18)
                        .TextColor(Color.Slate700)
                        .Margin(0, 0, 0, 15);

                    column.Item()
                        .Text("This document demonstrates the powerful pagination capabilities of BonniePDF. " +
                              "The employee table automatically spans multiple pages with headers repeating on each page, " +
                              "making it easy to reference column names regardless of where you are in the document. " +
                              "The text sections that follow flow naturally across page boundaries, maintaining readability " +
                              "and professional formatting throughout. This unified approach to content flow enables the " +
                              "creation of complex, multipage documents with consistent quality and appearance.")
                        .TextColor(Color.Slate600)
                        .Margin(0, 0, 0, 20);

                    column.Item()
                        .Text("Generated with BonniePDF - Flow Architecture Demo")
                        .Italic()
                        .FontSize(10)
                        .TextColor(Color.Slate400)
                        .AlignCenter()
                        .Margin(0, 20, 0, 0);
                });
            });
        }).Build();

    }
}

