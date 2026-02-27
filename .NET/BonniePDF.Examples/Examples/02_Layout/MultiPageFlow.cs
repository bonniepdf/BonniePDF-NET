using System.Linq;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._02_Layout;

/// <summary>
/// Demonstrates the Unified Flow & Pagination architecture.
/// Shows TextBlock overflow, VerticalStack overflow, and header/footer templates.
/// </summary>
public static class FlowArchitectureExample
{
    /// <summary>
    /// Generates a document with a long TextBlock that flows across multiple pages.
    /// </summary>
    public static string GenerateTextBlockOverflow(string outputPath = "textblock_overflow.pdf")
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Settings.FooterTemplate = pageNumber => new TextBlock
        {
            Text = $"Page {pageNumber} of {TextElementBase.TotalPagesToken}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Slate500,
            Alignment = TextAlignment.Right
        };

        doc.Metadata.Title = "TextBlock Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates text flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Long Text Document",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Generate a long text that will overflow
        string[] paragraphs = new[]
        {
            "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
            "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. Sed ut perspiciatis unde omnis iste natus error sit voluptatem accusantium doloremque laudantium, totam rem aperiam, eaque ipsa quae ab illo inventore veritatis et quasi architecto beatae vitae dicta sunt explicabo.",
            "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit, sed quia consequuntur magni dolores eos qui ratione voluptatem sequi nesciunt. Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit, sed quia non numquam eius modi tempora incidunt ut labore et dolore magnam aliquam quaerat voluptatem.",
            "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis suscipit laboriosam, nisi ut aliquid ex ea commodi consequatur? Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse quam nihil molestiae consequatur, vel illum qui dolorem eum fugiat quo voluptas nulla pariatur?",
            "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis praesentium voluptatum deleniti atque corrupti quos dolores et quas molestias excepturi sint occaecati cupiditate non provident, similique sunt in culpa qui officia deserunt mollitia animi, id est laborum et dolorum fuga."
        };

        // Repeat paragraphs to create enough content for multiple pages
        string longText = string.Join("\n\n", Enumerable.Range(1, 20).Select(i =>
            $"Section {i}\n\n{paragraphs[i % paragraphs.Length]}\n\n{paragraphs[(i + 1) % paragraphs.Length]}"));

        doc.Add(new TextBlock
        {
            Text = longText,
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate700
        });

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Generates a document with a VerticalStack containing many items that flows across pages.
    /// </summary>
    public static string GenerateVerticalStackOverflow(string outputPath = "verticalstack_overflow.pdf")
    {
        using Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "VerticalStack Overflow Demo";
        doc.Metadata.Author = "BonniePDF";
        doc.Metadata.Subject = "Demonstrates VerticalStack flowing across page boundaries";

        // Add a title
        doc.Add(new TextBlock
        {
            Text = "Item Catalog",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 10)
        });

        doc.Add(new TextBlock
        {
            Text = "A collection of items demonstrating automatic pagination",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.Slate500,
            Margin = new Thickness(0, 0, 0, 20)
        });

        // Create a vertical stack with many items
        VerticalStack stack = new() { Margin = new Thickness(0) };

        for (int i = 1; i <= 75; i++)
        {
            // Each item is a small card-like element
            VerticalStack itemStack = new()
            {
                Margin = new Thickness(0, 0, 0, 15)
            };

            itemStack.AddChild(new TextBlock
            {
                Text = $"Item #{i:D3}",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 12,
                TextColor = Color.Blue600,
                Margin = new Thickness(0, 0, 0, 5)
            });

            itemStack.AddChild(new TextBlock
            {
                Text =
                    $"This is the description for item {i}. It contains some details about what this item represents and why it's important.",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Slate600
            });

            stack.AddChild(itemStack);
        }

        doc.Add(stack);

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Generates a document with header and footer templates on each page.
    /// Note: Header/Footer template rendering requires pipeline integration.
    /// This example shows the settings configuration.
    /// </summary>
    public static string GenerateWithHeadersAndFooters(string outputPath = "headers_footers.pdf")
    {
        // Create document with custom settings
        DocumentSettings settings = new()
        {
            PageSize = PdfSize.A4,
            Margin = 60,
            DefaultZoom = ZoomLevel.FitPage,

            // Header template - shows document title
            HeaderTemplate = pageNumber => new TextBlock
            {
                Text = $"BonniePDF Documentation - Page {pageNumber}",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 10,
                TextColor = Color.Slate600,
                Alignment = TextAlignment.Center
            },

            // Footer template - shows page number
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"Page {pageNumber} - Confidential",
                FontName = StandardFonts.Helvetica,
                FontSize = 10,
                TextColor = Color.Slate500,
                Alignment = TextAlignment.Center
            }
        };

        using Document doc = new(settings);
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "Headers and Footers Demo";
        doc.Metadata.Author = "BonniePDF";

        // Add content that will span multiple pages
        doc.Add(new TextBlock
        {
            Text = "Chapter 1: Introduction",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 15)
        });

        // Generate content for several pages
        for (int section = 1; section <= 8; section++)
        {
            doc.Add(new TextBlock
            {
                Text = $"1.{section} Section Title",
                FontName = StandardFonts.HelveticaBold,
                FontSize = 14,
                TextColor = Color.Slate700,
                Margin = new Thickness(0, 15, 0, 10)
            });

            string paragraphText = section switch
            {
                1 =>
                    "This document demonstrates the header and footer template system in BonniePDF. Headers and footers are automatically rendered on each page after all content has been laid out.",
                2 =>
                    "The header template function receives the page number and returns an IElement that is rendered at the top of each page. This allows for dynamic content like section titles or logos.",
                3 =>
                    "Similarly, the footer template function creates elements for the bottom of each page. Common uses include page numbers, copyright notices, and document dates.",
                4 =>
                    "Templates can contain any valid IElement, including HorizontalStack for multi-column layouts, TextBlocks for simple text, or even Images for logos and branding.",
                5 =>
                    "The page number passed to templates is 1-based, making it easy to display 'Page 1 of N' style pagination. The total page count is available after rendering completes.",
                6 =>
                    "Templates are rendered after all page content is finalized, ensuring accurate page counts and proper positioning regardless of how content flows across pages.",
                7 =>
                    "You can conditionally render headers or footers by returning null from the template function. For example, you might omit the header on the first page.",
                8 =>
                    "This flexible system supports professional document layouts while maintaining the simplicity that makes BonniePDF easy to use.",
                _ => "Additional content section."
            };

            // Add multiple paragraphs per section
            for (int p = 0; p < 3; p++)
                doc.Add(new TextBlock
                {
                    Text = paragraphText + ' ' + paragraphText,
                    FontName = StandardFonts.Helvetica,
                    FontSize = 11,
                    TextColor = Color.Slate600,
                    Margin = new Thickness(0, 0, 0, 12)
                });
        }

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Generates a comprehensive document demonstrating the flow architecture with:
    /// - A long table with repeating headers on each page
    /// - Followed by a long text section that flows across pages
    /// This showcases how different element types can paginate together in a single document.
    /// </summary>
    public static string GenerateComprehensiveFlowDemo(string outputPath = "comprehensive_flow_demo.pdf")
    {
        // IMPORTANT: Set orientation in DocumentSettings BEFORE creating the Document
        // The first page is created in the constructor, so orientation must be set beforehand
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