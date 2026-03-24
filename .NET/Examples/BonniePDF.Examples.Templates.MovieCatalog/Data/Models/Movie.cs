namespace BonniePDF.Examples.Templates.MovieCatalog.Data.Models;

public class Movie
{
    public int Year { get; set; }
    public required string Title { get; set; }
    public required string Director { get; set; }
    public required string Genre { get; set; }
    public float Rating { get; set; }
    public required string Duration { get; set; }
}

