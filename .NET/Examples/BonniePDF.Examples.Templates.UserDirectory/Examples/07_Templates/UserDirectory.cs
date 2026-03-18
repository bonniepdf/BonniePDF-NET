using System;
using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Data;
using BonniePDF.Examples.Data.Models;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Examples._07_Templates;

/// <summary>
/// Generates a 2-page PDF showing a list of mock users
/// </summary>
public static class UsersTableExample
{

    public static string Generate(string outputPath)
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 40
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;

        doc.Metadata.Title = "User Directory";
        doc.Metadata.Author = "Company HR";

        // Title
        TextBlock title = new()
        {
            Text = "User Directory",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800
        };

        TextBlock subtitle = new()
        {
            Text = $"Generated on {DateTime.Now:MMMM dd, yyyy} | Total Users: 50",
            FontName = StandardFonts.Helvetica,
            FontSize = 11,
            TextColor = Color.Slate600,
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
            BorderColors = new BorderColors(Color.Slate300),
            HeaderBackgroundColor = Color.Slate800,
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
            Color statusColor = user.Status == "Active" ? Color.Green600 :
                user.Status == "Inactive" ? Color.Gray500 : Color.Amber600;
            Color statusBgColor = user.Status == "Active" ? Color.Green50 :
                user.Status == "Inactive" ? Color.Gray100 : Color.Amber50;

            // Alternating row background
            Color rowBgColor = rowIndex % 2 == 0 ? Color.White : Color.Slate50;

            TableRow row = TableRow.From(
                new TextBlock
                    { Text = user.Id, FontName = StandardFonts.Courier, FontSize = 9, TextColor = Color.Slate700 },
                new TextBlock
                    { Text = user.Name, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate800 },
                new TextBlock
                    { Text = user.Email, FontName = StandardFonts.Helvetica, FontSize = 8, TextColor = Color.Slate600 },
                new TextBlock
                {
                    Text = user.Department, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate700
                },
                new TextBlock
                    { Text = user.Role, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate700 },
                new TextBlock
                {
                    Text = user.Location, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate600
                },
                // Using TableCell for custom cell styling with background that fills the entire cell
                new TextBlock
                    {
                        Text = user.Status, FontName = StandardFonts.HelveticaBold, FontSize = 8,
                        TextColor = statusColor
                    }.AlignCenter().AsCell()
                    .Background(statusBgColor)
                    .Padding(4)
                    .Border(Color.Slate300, 0.5f) // Keep border consistent for this cell
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
            TextColor = Color.Slate400
        };

        mainStack.AddChild(footer);

        // Add to document
        doc.Add(mainStack);

        // Save PDF
        doc.Save(outputPath);

        return outputPath;
    }
}
