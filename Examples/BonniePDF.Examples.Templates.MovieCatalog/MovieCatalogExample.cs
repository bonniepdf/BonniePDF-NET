using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Examples.Templates.MovieCatalog.Data;
using BonniePDF.Examples.Templates.MovieCatalog.Data.Models;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.MovieCatalog;

/// <summary>
/// Generates a PDF showing a list of movies with ratings
/// </summary>
[System.ComponentModel.DisplayName("Movie Catalog")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Generates a PDF showing a list of classic movies with ratings, director, genre, and duration in a formatted table.")]
public static class MovieCatalogExample
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
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(10).WithColor(ColorPalette.Slate[600]);

        doc.Metadata.Title = "Classic Movies Collection";
        doc.Metadata.Author = "Classic Cinema Archive";

        IList<Movie> movies = MoviesData.GetMockMovies();
        // Title Section
        VerticalStack titleStack = new();
        titleStack.AddChild(new TextBlock
        {
            Text = "Classic Movies Collection",
            FontName = StandardFonts.TimesBold,
            FontSize = 26,
            TextColor = ColorPalette.Slate[900]
        });
        titleStack.AddChild(new TextBlock
        {
            Text = "A curated selection of timeless cinema from the golden age of Hollywood",
            FontName = StandardFonts.TimesItalic,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600]
        });
        titleStack.AddChild(new TextBlock
        {
            Text = $"Total Movies: {MoviesData.GetMockMovies().Count} | Compiled: {DateTime.Now:MMMM yyyy}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = ColorPalette.Slate[500]
        });

        // MoviesData Table
        Table moviesTable = new(1f, 3f, 4f, 2f, 1.5f, 2f)
        {
            CellPadding = 8,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(ColorPalette.Slate[400]),
            HeaderBackgroundColor = ColorPalette.Indigo[800],
            HeaderTextColor = Color.White,
            RepeatHeadersOnNewPage = true
        };

        // Table Header - using dedicated AddHeader method
        moviesTable.AddHeader(
            new TextBlock { Text = "Year", FontName = StandardFonts.HelveticaBold, FontSize = 10 }.AlignCenter(),
            new TextBlock { Text = "Title", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Director", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Genre", FontName = StandardFonts.HelveticaBold, FontSize = 10 },
            new TextBlock { Text = "Rating", FontName = StandardFonts.HelveticaBold, FontSize = 10 }.AlignCenter(),
            new TextBlock { Text = "Duration", FontName = StandardFonts.HelveticaBold, FontSize = 10 }.AlignCenter()
        );

        int rowIndex = 0;
        foreach (Movie movie in movies)
        {
            Color ratingColor = movie.Rating >= 9.0 ? ColorPalette.Green[600] :
                movie.Rating >= 7.5 ? ColorPalette.Amber[600] : ColorPalette.Slate[600];
            Color ratingBg = movie.Rating >= 9.0 ? ColorPalette.Green[50] : movie.Rating >= 7.5 ? ColorPalette.Amber[50] : ColorPalette.Slate[100];

            // Alternating row background
            Color rowBgColor = rowIndex % 2 == 0 ? Color.White : ColorPalette.Slate[50];

            TableRow row = TableRow.From(
                new TextBlock
                {
                    Text = movie.Year.ToString(), FontName = StandardFonts.CourierBold, FontSize = 10,
                    TextColor = ColorPalette.Indigo[700],
                    Alignment = TextAlignment.Center
                },
                new TextBlock
                {
                    Text = movie.Title, FontName = StandardFonts.HelveticaBold, FontSize = 10,
                    TextColor = ColorPalette.Slate[900]
                },
                new TextBlock
                {
                    Text = movie.Director, FontName = StandardFonts.Helvetica, FontSize = 10, TextColor = ColorPalette.Slate[600]
                },
                new TextBlock
                {
                    Text = movie.Genre, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = ColorPalette.Slate[600]
                },
                // Using TableCell for custom cell styling with background that fills the entire cell
                new TextBlock
                    {
                        Text = movie.Rating.ToString("F1"), FontName = StandardFonts.HelveticaBold, FontSize = 10,
                        TextColor = ratingColor
                    }.AlignCenter()
                    .AsCell()
                    .Background(ratingBg)
                    .Padding(4)
                    .Border(ColorPalette.Slate[300], 0.5f),
                new TextBlock
                {
                    Text = movie.Duration, FontName = StandardFonts.Courier, FontSize = 9, TextColor = ColorPalette.Slate[600]
                }.AlignCenter()
            );

            row.BackgroundColor = rowBgColor;
            moviesTable.AddRow(row);
            rowIndex++;
        }

        // Statistics Section
        VerticalStack statsStack = new();
        statsStack.AddChild(new TextBlock
        {
            Text = "Collection Statistics", FontName = StandardFonts.HelveticaBold, FontSize = 12,
            TextColor = ColorPalette.Slate[800]
        });

        float avgRating = movies.Average(m => m.Rating);
        Movie topRated = movies.OrderByDescending(m => m.Rating).First();
        Movie earliest = movies.OrderBy(m => m.Year).First();
        Movie latest = movies.OrderByDescending(m => m.Year).First();

        statsStack.AddChild(new TextBlock
        {
            Text = $"- Average Rating: {avgRating:F1}/10", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = ColorPalette.Slate[600]
        });
        statsStack.AddChild(new TextBlock
        {
            Text = $"- Top Rated: {topRated.Title} ({topRated.Rating:F1})", FontName = StandardFonts.Helvetica,
            FontSize = 10, TextColor = ColorPalette.Slate[600]
        });
        statsStack.AddChild(new TextBlock
        {
            Text = $"- Year Range: {earliest.Year} - {latest.Year}", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = ColorPalette.Slate[600]
        });
        statsStack.AddChild(new TextBlock
        {
            Text = "- Most Represented Genre: Drama", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = ColorPalette.Slate[600]
        });

        // Footer
        TextBlock footer = new()
        {
            Text = $"© {DateTime.Now.Year} Classic Cinema Archive. All rights reserved. | Films listed for educational purposes.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 8,
            TextColor = ColorPalette.Slate[400]
        };

        // Main Layout
        VerticalStack mainStack = new();
        mainStack.AddChild(titleStack);
        mainStack.AddChild(moviesTable);
        mainStack.AddChild(statsStack);
        mainStack.AddChild(footer);

        // Add to document
        doc.Add(mainStack);

        // Save PDF
        return doc;
    }
}

