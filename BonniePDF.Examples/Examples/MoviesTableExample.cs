using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Configuration;
using BonniePDF.Examples.ExamplesData;
using BonniePDF.Extensions;
using BonniePDF.Layout;
using BonniePDF.Layout.Elements;
using BonniePDF.Models;

namespace BonniePDF.Examples.Examples;

/// <summary>
/// Generates a PDF showing a list of movies with ratings
/// </summary>
public class MoviesTableExample
{
    public static string Generate(string outputPath = "movies_list.pdf")
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Orientation = PageOrientation.Landscape,
            Margin = 40
        });

        doc.Metadata.Title = "Classic Movies Collection";
        doc.Metadata.Author = "Classic Cinema Archive";

        List<Movie> movies = Movies.GetMockMovies();
        // Title Section
        VerticalStack titleStack = new();
        titleStack.AddChild(new TextBlock
        {
            Text = "Classic Movies Collection",
            FontName = StandardFonts.TimesBold,
            FontSize = 26,
            TextColor = Color.Slate900
        });
        titleStack.AddChild(new TextBlock
        {
            Text = "A curated selection of timeless cinema from the golden age of Hollywood",
            FontName = StandardFonts.TimesItalic,
            FontSize = 12,
            TextColor = Color.Slate600
        });
        titleStack.AddChild(new TextBlock
        {
            Text = $"Total Movies: {Movies.GetMockMovies().Count} | Compiled: {DateTime.Now:MMMM yyyy}",
            FontName = StandardFonts.Helvetica,
            FontSize = 10,
            TextColor = Color.Slate500
        });

        // Movies Table
        Table moviesTable = new(1f, 3f, 4f, 2f, 1.5f, 2f)
        {
            CellPadding = 8,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Color.Slate400),
            HeaderBackgroundColor = Color.Indigo800,
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
            Color ratingColor = movie.Rating >= 9.0 ? Color.Green600 :
                movie.Rating >= 7.5 ? Color.Amber600 : Color.Slate600;
            Color ratingBg = movie.Rating >= 9.0 ? Color.Green50 : movie.Rating >= 7.5 ? Color.Amber50 : Color.Slate100;

            // Alternating row background
            Color rowBgColor = rowIndex % 2 == 0 ? Color.White : Color.Slate50;

            TableRow row = TableRow.From(
                new TextBlock
                {
                    Text = movie.Year.ToString(), FontName = StandardFonts.CourierBold, FontSize = 10,
                    TextColor = Color.Indigo700
                },
                new TextBlock
                {
                    Text = movie.Title, FontName = StandardFonts.HelveticaBold, FontSize = 10,
                    TextColor = Color.Slate900
                },
                new TextBlock
                {
                    Text = movie.Director, FontName = StandardFonts.Helvetica, FontSize = 10, TextColor = Color.Slate700
                },
                new TextBlock
                {
                    Text = movie.Genre, FontName = StandardFonts.Helvetica, FontSize = 9, TextColor = Color.Slate600
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
                    .Border(Color.Slate300, 0.5f),
                new TextBlock
                {
                    Text = movie.Duration, FontName = StandardFonts.Courier, FontSize = 9, TextColor = Color.Slate600
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
            TextColor = Color.Slate800
        });

        float avgRating = movies.Average(m => m.Rating);
        Movie topRated = movies.OrderByDescending(m => m.Rating).First();
        Movie earliest = movies.OrderBy(m => m.Year).First();
        Movie latest = movies.OrderByDescending(m => m.Year).First();

        statsStack.AddChild(new TextBlock
        {
            Text = $"- Average Rating: {avgRating:F1}/10", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = Color.Slate700
        });
        statsStack.AddChild(new TextBlock
        {
            Text = $"- Top Rated: {topRated.Title} ({topRated.Rating:F1})", FontName = StandardFonts.Helvetica,
            FontSize = 10, TextColor = Color.Slate700
        });
        statsStack.AddChild(new TextBlock
        {
            Text = $"- Year Range: {earliest.Year} - {latest.Year}", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = Color.Slate700
        });
        statsStack.AddChild(new TextBlock
        {
            Text = "- Most Represented Genre: Drama", FontName = StandardFonts.Helvetica, FontSize = 10,
            TextColor = Color.Slate700
        });

        // Footer
        TextBlock footer = new()
        {
            Text = "(c) 2024 Classic Cinema Archive. All rights reserved. | Films listed for educational purposes.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 8,
            TextColor = Color.Slate400
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
        doc.Save(outputPath);

        return outputPath;
    }
}