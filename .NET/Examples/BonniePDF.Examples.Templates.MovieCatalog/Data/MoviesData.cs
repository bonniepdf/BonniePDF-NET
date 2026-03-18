using System.Collections.Generic;
using BonniePDF.Examples.Data.Models;

namespace BonniePDF.Examples.Data;

public static class MoviesData
{
    public static IList<Movie> GetMockMovies()
    {
        return new List<Movie>
        {
            new()
            {
                Year = 1941, Title = "Citizen Kane", Director = "Orson Welles", Genre = "Drama", Rating = 8.3f,
                Duration = "119 min"
            },
            new()
            {
                Year = 1942, Title = "Casablanca", Director = "Michael Curtiz", Genre = "Drama/Romance", Rating = 8.5f,
                Duration = "102 min"
            },
            new()
            {
                Year = 1946, Title = "It's a Wonderful Life", Director = "Frank Capra", Genre = "Drama/Fantasy",
                Rating = 8.6f, Duration = "130 min"
            },
            new()
            {
                Year = 1950, Title = "Sunset Boulevard", Director = "Billy Wilder", Genre = "Drama/Noir", Rating = 8.4f,
                Duration = "110 min"
            },
            new()
            {
                Year = 1952, Title = "Singin' in the Rain", Director = "Stanley Donen, Gene Kelly",
                Genre = "Musical/Comedy", Rating = 8.3f, Duration = "103 min"
            },
            new()
            {
                Year = 1954, Title = "Rear Window", Director = "Alfred Hitchcock", Genre = "Thriller/Mystery",
                Rating = 8.5f, Duration = "112 min"
            },
            new()
            {
                Year = 1954, Title = "Seven Samurai", Director = "Akira Kurosawa", Genre = "Action/Drama",
                Rating = 8.6f, Duration = "207 min"
            },
            new()
            {
                Year = 1957, Title = "12 Angry Men", Director = "Sidney Lumet", Genre = "Drama", Rating = 9.0f,
                Duration = "96 min"
            },
            new()
            {
                Year = 1957, Title = "The Bridge on the River Kwai", Director = "David Lean", Genre = "Adventure/Drama",
                Rating = 8.1f, Duration = "161 min"
            },
            new()
            {
                Year = 1958, Title = "Vertigo", Director = "Alfred Hitchcock", Genre = "Mystery/Romance", Rating = 8.3f,
                Duration = "128 min"
            },
            new()
            {
                Year = 1959, Title = "North by Northwest", Director = "Alfred Hitchcock", Genre = "Action/Thriller",
                Rating = 8.4f, Duration = "136 min"
            },
            new()
            {
                Year = 1960, Title = "Psycho", Director = "Alfred Hitchcock", Genre = "Horror/Thriller", Rating = 8.5f,
                Duration = "109 min"
            },
            new()
            {
                Year = 1960, Title = "Spartacus", Director = "Stanley Kubrick", Genre = "Adventure/Drama",
                Rating = 7.9f, Duration = "197 min"
            },
            new()
            {
                Year = 1962, Title = "To Kill a Mockingbird", Director = "Robert Mulligan", Genre = "Drama",
                Rating = 8.5f, Duration = "129 min"
            },
            new()
            {
                Year = 1964, Title = "Dr. Strangelove", Director = "Stanley Kubrick", Genre = "Comedy/Sci-Fi",
                Rating = 8.4f, Duration = "95 min"
            },
            new()
            {
                Year = 1966, Title = "The Good, the Bad and the Ugly", Director = "Sergio Leone", Genre = "Western",
                Rating = 8.8f, Duration = "178 min"
            },
            new()
            {
                Year = 1967, Title = "The Graduate", Director = "Mike Nichols", Genre = "Drama/Romance", Rating = 7.9f,
                Duration = "106 min"
            },
            new()
            {
                Year = 1968, Title = "2001: A Space Odyssey", Director = "Stanley Kubrick", Genre = "Sci-Fi/Adventure",
                Rating = 8.3f, Duration = "149 min"
            },
            new()
            {
                Year = 1969, Title = "Butch Cassidy and the Sundance Kid", Director = "George Roy Hill",
                Genre = "Biography/Drama", Rating = 8.0f, Duration = "110 min"
            },
            new()
            {
                Year = 1971, Title = "The French Connection", Director = "William Friedkin", Genre = "Action/Crime",
                Rating = 7.8f, Duration = "104 min"
            },
            new()
            {
                Year = 1972, Title = "The Godfather", Director = "Francis Ford Coppola", Genre = "Crime/Drama",
                Rating = 9.2f, Duration = "175 min"
            },
            new()
            {
                Year = 1974, Title = "The Godfather Part II", Director = "Francis Ford Coppola", Genre = "Crime/Drama",
                Rating = 9.0f, Duration = "202 min"
            },
            new()
            {
                Year = 1975, Title = "One Flew Over the Cuckoo's Nest", Director = "Milos Forman", Genre = "Drama",
                Rating = 8.7f, Duration = "133 min"
            },
            new()
            {
                Year = 1976, Title = "Taxi Driver", Director = "Martin Scorsese", Genre = "Drama/Thriller",
                Rating = 8.2f, Duration = "114 min"
            },
            new()
            {
                Year = 1977, Title = "Star Wars: Episode IV", Director = "George Lucas", Genre = "Action/Sci-Fi",
                Rating = 8.6f, Duration = "121 min"
            },
            new()
            {
                Year = 1979, Title = "Apocalypse Now", Director = "Francis Ford Coppola", Genre = "Drama/War",
                Rating = 8.5f, Duration = "147 min"
            },
            new()
            {
                Year = 1980, Title = "Raging Bull", Director = "Martin Scorsese", Genre = "Biography/Drama",
                Rating = 8.2f, Duration = "129 min"
            },
            new()
            {
                Year = 1980, Title = "The Empire Strikes Back", Director = "Irvin Kershner", Genre = "Action/Sci-Fi",
                Rating = 8.7f, Duration = "124 min"
            },
            new()
            {
                Year = 1981, Title = "Raiders of the Lost Ark", Director = "Steven Spielberg",
                Genre = "Action/Adventure", Rating = 8.4f, Duration = "115 min"
            },
            new()
            {
                Year = 1984, Title = "Amadeus", Director = "Milos Forman", Genre = "Biography/Drama", Rating = 8.4f,
                Duration = "160 min"
            },
            new()
            {
                Year = 1984, Title = "This Is Spinal Tap", Director = "Rob Reiner", Genre = "Comedy/Music",
                Rating = 7.9f, Duration = "82 min"
            },
            new()
            {
                Year = 1985, Title = "Back to the Future", Director = "Robert Zemeckis", Genre = "Adventure/Comedy",
                Rating = 8.5f, Duration = "116 min"
            },
            new()
            {
                Year = 1986, Title = "Aliens", Director = "James Cameron", Genre = "Action/Sci-Fi", Rating = 8.5f,
                Duration = "137 min"
            },
            new()
            {
                Year = 1987, Title = "Princess Bride", Director = "Rob Reiner", Genre = "Adventure/Comedy",
                Rating = 8.0f, Duration = "98 min"
            },
            new()
            {
                Year = 1988, Title = "Die Hard", Director = "John McTiernan", Genre = "Action/Thriller", Rating = 8.2f,
                Duration = "132 min"
            },
            new()
            {
                Year = 1989, Title = "Do the Right Thing", Director = "Spike Lee", Genre = "Drama", Rating = 7.9f,
                Duration = "120 min"
            },
            new()
            {
                Year = 1989, Title = "When Harry Met Sally...", Director = "Rob Reiner", Genre = "Comedy/Romance",
                Rating = 7.7f, Duration = "96 min"
            },
            new()
            {
                Year = 1991, Title = "The Silence of the Lambs", Director = "Jonathan Demme", Genre = "Crime/Drama",
                Rating = 8.6f, Duration = "118 min"
            },
            new()
            {
                Year = 1992, Title = "Reservoir Dogs", Director = "Quentin Tarantino", Genre = "Crime/Drama",
                Rating = 8.3f, Duration = "99 min"
            },
            new()
            {
                Year = 1993, Title = "Schindler's List", Director = "Steven Spielberg", Genre = "Biography/Drama",
                Rating = 9.0f, Duration = "195 min"
            },
            new()
            {
                Year = 1994, Title = "Pulp Fiction", Director = "Quentin Tarantino", Genre = "Crime/Drama",
                Rating = 8.9f, Duration = "154 min"
            },
            new()
            {
                Year = 1994, Title = "The Shawshank Redemption", Director = "Frank Darabont", Genre = "Drama",
                Rating = 9.3f, Duration = "142 min"
            },
            new()
            {
                Year = 1995, Title = "Se7en", Director = "David Fincher", Genre = "Crime/Drama", Rating = 8.6f,
                Duration = "127 min"
            },
            new()
            {
                Year = 1997, Title = "Titanic", Director = "James Cameron", Genre = "Drama/Romance", Rating = 7.9f,
                Duration = "194 min"
            },
            new()
            {
                Year = 1998, Title = "The Big Lebowski", Director = "Joel Coen", Genre = "Comedy/Crime", Rating = 8.1f,
                Duration = "117 min"
            },
            new()
            {
                Year = 1999, Title = "Fight Club", Director = "David Fincher", Genre = "Drama", Rating = 8.8f,
                Duration = "139 min"
            },
            new()
            {
                Year = 1999, Title = "The Matrix", Director = "The Wachowskis", Genre = "Action/Sci-Fi", Rating = 8.7f,
                Duration = "136 min"
            }
        };
    }
}
