using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Templates.UserDirectory.Data;
using BonniePDF.Examples.Templates.UserDirectory.Data.Models;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.UserDirectory;

/// <summary>
/// Generates a 2-page PDF showing a list of mock users
/// </summary>
[System.ComponentModel.DisplayName("User Directory")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Generates a 2-page PDF showing a list of mock users with ID, name, email, department, role, location, and status in a formatted table.")]
public static class UserDirectoryExample
{

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 40
        });

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "User Directory";
        doc.Metadata.Author = "Company HR";

        // Title
        TextBlock title = new()
        {
            Text = "User Directory",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800]
        };

        TextBlock subtitle = new()
        {
            Text = $"Generated on {DateTime.Now:MMMM dd, yyyy} | Total Users: 50",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0,0,0,5)
        };

        VerticalStack headerStack = new();
        headerStack.AddChild(title);
        headerStack.AddChild(subtitle);

        // Users Table
        Table usersTable = new(1f, 2f, 2.5f, 2f, 1.5f, 2f, 2f)
        {
            CellPadding = 8,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[300]),
            HeaderBackgroundColor = ColorPalette.Slate[800],
            HeaderTextColor = Color.White,
            RepeatHeadersOnNewPage = true
        };

        // Table Header - using dedicated AddHeader method
        usersTable.AddHeader(
            new TextBlock
                { Text = "ID", FontName = StandardFonts.HelveticaBold, FontSize = 9 }.AlignCenter(),
            new TextBlock { Text = "Name", FontName = StandardFonts.HelveticaBold, FontSize = 9 },
            new TextBlock { Text = "Email", FontName = StandardFonts.HelveticaBold, FontSize = 9 },
            new TextBlock { Text = "Department", FontName = StandardFonts.HelveticaBold, FontSize = 9 },
            new TextBlock { Text = "Role", FontName = StandardFonts.HelveticaBold, FontSize = 9 },
            new TextBlock { Text = "Location", FontName = StandardFonts.HelveticaBold, FontSize = 9 },
            new TextBlock
                { Text = "Status", FontName = StandardFonts.HelveticaBold, FontSize = 9 }.AlignCenter()
        );

        // Mock Users Data
        IList<User> users = UsersData.GetMockUsers();

        int rowIndex = 0;
        foreach (User user in users)
        {
            Color statusColor = user.Status == "Active" ? ColorPalette.Green[600] :
                user.Status == "Inactive" ? ColorPalette.Gray[500] : ColorPalette.Amber[600];
            Color statusBgColor = user.Status == "Active" ? ColorPalette.Green[50] :
                user.Status == "Inactive" ? ColorPalette.Gray[100] : ColorPalette.Amber[50];

            // Alternating row background
            Color rowBgColor = rowIndex % 2 == 0 ? Color.White : ColorPalette.Slate[50];

            TableRow row = TableRow.From(
                new TextBlock
                    { Text = user.Id, FontName = StandardFonts.Courier, FontSize = 9, TextColor = ColorPalette.Slate[600] },
                new TextBlock
                    { Text = user.Name, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = ColorPalette.Slate[800] },
                new TextBlock
                    { Text = user.Email, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = ColorPalette.Slate[600] },
                new TextBlock
                {
                    Text = user.Department, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = ColorPalette.Slate[600]
                },
                new TextBlock
                    { Text = user.Role, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = ColorPalette.Slate[600] },
                new TextBlock
                {
                    Text = user.Location, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = ColorPalette.Slate[600]
                },
                // Using TableCell for custom cell styling with background that fills the entire cell
                new TextBlock
                    {
                        Text = user.Status, FontName = StandardFonts.HelveticaBold, FontSize = 8,
                        TextColor = statusColor
                    }.AlignCenter().AsCell()
                    .Background(statusBgColor)
                    .Padding(4)
                    .Border(ColorPalette.Slate[300], 0.5f) // Keep border consistent for this cell
            );

            row.BackgroundColor = rowBgColor;
            usersTable.AddRow(row);
            rowIndex++;
        }

        // Main Layout
        VerticalStack mainStack = new();
        mainStack.AddChild(headerStack);
        mainStack.AddChild(usersTable);

        // Footer note
        TextBlock footer = new()
        {
            Text = "* This is a confidential document. For internal use only.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 8,
            TextColor = ColorPalette.Slate[400]
        };

        mainStack.AddChild(footer);

        // Add to document
        doc.Add(mainStack);

        // Save PDF
        return doc;
    }
}

