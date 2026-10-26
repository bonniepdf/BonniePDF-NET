using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace BonniePDF;

/// <summary>
/// Deterministic placeholder text for prototyping layouts before real content
/// exists. The sequence is seeded, so repeated renders produce the same document.
/// </summary>
public static class Placeholders
{
    private static PlaceholderSource _global = new(42);

    /// <summary>A single lorem-ipsum word.</summary>
    public static string Word() => _global.Word();
    /// <summary>A two-word capitalised label.</summary>
    public static string Label() => _global.Label();
    /// <summary>One lorem-ipsum sentence.</summary>
    public static string Sentence() => _global.Sentence();
    /// <summary>A paragraph of lorem-ipsum text.</summary>
    public static string Paragraph() => _global.Paragraph();
    /// <summary>Several paragraphs of lorem-ipsum text, separated by blank lines (3 by default).</summary>
    public static string Paragraphs(int count = 3) => _global.Paragraphs(count);

    /// <summary>A full name.</summary>
    public static string Name() => _global.Name();
    /// <summary>A given name.</summary>
    public static string FirstName() => _global.FirstName();
    /// <summary>A family name.</summary>
    public static string LastName() => _global.LastName();
    /// <summary>An email address.</summary>
    public static string Email() => _global.Email();
    /// <summary>A phone number.</summary>
    public static string PhoneNumber() => _global.PhoneNumber();
    /// <summary>A company name.</summary>
    public static string CompanyName() => _global.CompanyName();
    /// <summary>A department name.</summary>
    public static string Department() => _global.Department();
    /// <summary>A job title.</summary>
    public static string Role() => _global.Role();
    /// <summary>A place name.</summary>
    public static string Location() => _global.Location();

    /// <summary>
    /// A whole number between <paramref name="min"/> and <paramref name="max"/> inclusive (0 to 1000 by
    /// default).
    /// </summary>
    public static int Integer(int min = 0, int max = 1000) => _global.Integer(min, max);
    /// <summary>
    /// A number between <paramref name="min"/> and <paramref name="max"/>, rounded to
    /// <paramref name="decimals"/> places (0 to 1000 with 2 places by default).
    /// </summary>
    public static decimal Decimal(decimal min = 0, decimal max = 1000, int decimals = 2) => _global.Decimal(min, max, decimals);
    /// <summary>A whole-number percentage such as <c>42%</c> (0 to 100 by default).</summary>
    public static string Percent(int min = 0, int max = 100) => _global.Percent(min, max);
    /// <summary>
    /// A currency amount such as <c>$1,234.50</c>, between <paramref name="min"/> and <paramref name="max"/> (1
    /// to 10,000, with <c>$</c>, by default).
    /// </summary>
    public static string Currency(decimal min = 1, decimal max = 10000, string symbol = "$") => _global.Currency(min, max, symbol);

    /// <summary>A date as <c>MM/DD/YYYY</c>.</summary>
    public static string ShortDate() => _global.ShortDate();
    /// <summary>A date written out in full.</summary>
    public static string LongDate() => _global.LongDate();
    /// <summary>A month name.</summary>
    public static string MonthLabel() => _global.MonthLabel();

    /// <summary>A light colour that suits a background fill.</summary>
    public static Color BackgroundColor() => _global.BackgroundColor();
    /// <summary>An accent colour.</summary>
    public static Color Color() => _global.Color();

    /// <summary>Invoice line items, ready for a table (5 by default).</summary>
    public static IList<InvoiceLineItem> InvoiceLines(int count = 5) => _global.InvoiceLines(count);
    /// <summary>
    /// Rows of table cells (name, department, location, percentage), as arrays of text (5 rows of 4 columns by
    /// default).
    /// </summary>
    public static IList<string[]> TableRows(int rows = 5, int columns = 4) => _global.TableRows(rows, columns);
    /// <summary>A series of whole numbers for a chart, one per month (12 between 50 and 300 by default).</summary>
    public static IList<int> MonthlyValues(int count = 12, int min = 50, int max = 300) => _global.MonthlyValues(count, min, max);

    /// <summary>A PNG placeholder image of the requested size, as bytes.</summary>
    public static byte[] Image(int width, int height) => _global.Image(width, height);

    /// <summary>A PNG placeholder avatar icon of the requested square size (48 by default), as bytes.</summary>
    public static byte[] Avatar(int size = 48) => _global.Avatar(size);

    /// <summary>Several employee records for directory tables and team prototypes (10 by default).</summary>
    public static IList<EmployeeItem> Employees(int count = 10) => _global.Employees(count);

    /// <summary>Returns an independent source that always produces the same sequence for the same seed.</summary>
    public static PlaceholderSource WithSeed(int seed) => new(seed);

    /// <summary>Rewinds the shared source to its starting seed (42).</summary>
    public static void Reset() => _global = new PlaceholderSource(42);
}

/// <summary>
/// One priced line of placeholder invoice content.
/// </summary>
/// <param name="Service">The service name.</param>
/// <param name="Description">A short description of the work.</param>
/// <param name="Quantity">The number of units.</param>
/// <param name="UnitPrice">The price of one unit.</param>
/// <param name="Total">The line total: quantity times unit price.</param>
public record InvoiceLineItem(string Service, string Description, int Quantity, decimal UnitPrice, decimal Total);

/// <summary>
/// One employee record for prototyping directory and team layouts.
/// </summary>
/// <param name="Id">The 1-based index or employee id.</param>
/// <param name="Name">The full name.</param>
/// <param name="Role">The job title.</param>
/// <param name="Department">The department name.</param>
/// <param name="Avatar">A circular avatar placeholder PNG, as bytes.</param>
public sealed record EmployeeItem(int Id, string Name, string Role, string Department, byte[] Avatar);

/// <summary>
/// A single seeded stream of placeholder values. Use this directly when one
/// document needs its own sequence, independent of the shared
/// <see cref="Placeholders"/> stream.
/// </summary>
public class PlaceholderSource(int seed)
{
    private int _index = Math.Abs(seed) % 1000;

    private int Next(int max)
    {
        _index = (_index * 1103515245 + 12345) & 0x7FFFFFFF;
        return _index % max;
    }

    private static readonly string[] FirstNames =
    [
        "Alice", "Bob", "Carol", "Dan", "Eve", "Frank", "Grace", "Hank", "Iris", "Jack",
        "Kate", "Leo", "Mia", "Nate", "Olivia", "Pete", "Quinn", "Rose", "Sam", "Tina",
        "Uma", "Victor", "Wendy", "Xander", "Yara", "Zach", "Amy", "Brian", "Chloe", "David",
        "Emma", "Felix", "Gina", "Hugo", "Isabel", "James", "Karen", "Liam", "Mona", "Neil",
        "Ophelia", "Paul", "Rachel", "Steven", "Theresa", "Ulrich", "Violet", "Will", "Xena", "Yvonne",
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Jones", "Brown", "Taylor", "Wilson", "Davis", "Moore", "Anderson", "Thomas", "Jackson",
        "White", "Harris", "Martin", "Garcia", "Martinez", "Lee", "Perez", "Thompson", "Walker", "Hall",
        "Allen", "Young", "Hernandez", "King", "Wright", "Lopez", "Hill", "Scott", "Green", "Adams",
        "Baker", "Nelson", "Carter", "Mitchell", "Roberts", "Turner", "Phillips", "Campbell", "Parker", "Evans",
        "Edwards", "Collins", "Stewart", "Sanchez", "Morris", "Rogers", "Reed", "Cook", "Morgan", "Bell",
    ];

    private static readonly string[] Departments = ["Engineering", "Marketing", "Sales", "HR", "Finance", "Design", "Operations"];
    private static readonly string[] Roles =
    [
        "Senior Developer",
        "Brand Manager",
        "HR Specialist",
        "Financial Analyst",
        "QA Lead",
        "Account Executive",
        "Project Manager",
        "Research Scientist",
        "Support Lead",
        "DevOps Engineer",
        "Compliance Officer",
        "Systems Administrator",
        "Frontend Developer",
        "Budget Analyst",
        "Content Strategist",
        "Sales Manager",
        "Backend Developer",
        "Supply Chain Lead",
        "Data Scientist",
        "CX Analyst",
        "Recruiter",
        "Controller",
        "Platform Architect",
        "IP Counsel",
        "Security Engineer",
        "Digital Strategist",
        "Business Developer",
        "Logistics Manager",
        "UX Researcher",
        "Mobile Developer",
        "Events Manager",
        "Cloud Architect",
        "Treasury Analyst",
        "AI Researcher",
        "Contract Specialist",
        "Sales Engineer",
        "Process Analyst",
        "Network Engineer",
        "Escalations Lead",
        "Site Reliability Engineer",
        "L&D Manager",
        "Risk Analyst",
        "SEO Specialist",
        "Data Engineer",
        "Fleet Coordinator",
    ];
    private static readonly string[] Locations = ["New York", "London", "Berlin", "Tokyo", "Sydney", "Toronto", "Paris"];

    private static readonly string[] CompanyNames =
    [
        "Sample Company", "Sample Customer", "Sample Supplier", "Sample Software",
        "Sample Industries", "Sample Solutions", "Sample Systems", "Sample Services",
        "Sample Retail", "Sample Group",
    ];

    private static readonly string[] LoremSentences =
    [
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit.",
        "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
        "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.",
        "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore.",
        "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia.",
        "Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit aut fugit.",
        "Neque porro quisquam est, qui dolorem ipsum quia dolor sit amet.",
        "Ut enim ad minima veniam, quis nostrum exercitationem ullam corporis.",
        "Quis autem vel eum iure reprehenderit qui in ea voluptate velit esse.",
        "At vero eos et accusamus et iusto odio dignissimos ducimus qui blanditiis.",
        "Nam libero tempore, cum soluta nobis est eligendi optio cumque nihil impedit.",
        "Temporibus autem quibusdam et aut officiis debitis aut rerum necessitatibus.",
        "Itaque earum rerum hic tenetur a sapiente delectus, ut aut reiciendis.",
        "Nulla facilisi cras fermentum odio eu feugiat pretium nibh ipsum consequat.",
        "Viverra justo nec ultrices dui sapien eget mi proin sed libero enim.",
    ];

    private static readonly string[] MonthLabels = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    private static readonly string[] ServiceNames =
    [
        "Strategy Workshop", "Identity Design", "Website Design", "Development",
        "Motion Graphics", "Content Strategy", "SEO Audit", "Brand Guidelines",
        "UX Research", "Copywriting",
    ];

    private static readonly string[] ServiceDescriptions =
    [
        "2-day brand strategy intensive", "Logo, colors, typography system",
        "Responsive website with animations", "Next.js with CMS integration",
        "Hero animations, micro-interactions", "Content calendar and editorial plan",
        "Technical SEO and keyword analysis", "Comprehensive style guide document",
        "User interviews and usability testing", "Website and marketing copy",
    ];

    private static readonly Color[] BackgroundColors =
    [
        ColorPalette.Blue[50], ColorPalette.Green[50], ColorPalette.Amber[50], ColorPalette.Rose[50],
        ColorPalette.Indigo[50], ColorPalette.Slate[50], ColorPalette.Purple[50], ColorPalette.Cyan[50],
    ];

    private static readonly Color[] AccentColors =
    [
        ColorPalette.Indigo[600], ColorPalette.Blue[600], ColorPalette.Green[600], ColorPalette.Amber[600],
        ColorPalette.Rose[600], ColorPalette.Purple[600], ColorPalette.Cyan[600], ColorPalette.Slate[600],
    ];

    /// <summary>A single lorem-ipsum word.</summary>
    public string Word()
    {
        var words = LoremSentences[Next(LoremSentences.Length)].Split(' ');
        return words[Next(words.Length)].Trim('.', ',');
    }

    /// <summary>A two-word capitalised label.</summary>
    public string Label()
    {
        var w = $"{Word()} {Word()}";
        return char.ToUpper(w[0]) + w[1..];
    }

    /// <summary>One lorem-ipsum sentence.</summary>
    public string Sentence() => LoremSentences[Next(LoremSentences.Length)];

    /// <summary>A paragraph of lorem-ipsum text.</summary>
    public string Paragraph()
    {
        int count = 3 + Next(4);
        var sentences = new List<string>(count);
        for (int i = 0; i < count; i++) sentences.Add(Sentence());
        return string.Join(" ", sentences);
    }

    /// <summary>Several paragraphs of lorem-ipsum text, separated by blank lines (3 by default).</summary>
    public string Paragraphs(int count = 3)
    {
        var paras = new List<string>(count);
        for (int i = 0; i < count; i++) paras.Add(Paragraph());
        return string.Join("\n\n", paras);
    }

    /// <summary>A full name.</summary>
    public string Name() => $"{FirstNames[Next(FirstNames.Length)]} {LastNames[Next(LastNames.Length)]}";
    /// <summary>A given name.</summary>
    public string FirstName() => FirstNames[Next(FirstNames.Length)];
    /// <summary>A family name.</summary>
    public string LastName() => LastNames[Next(LastNames.Length)];

    /// <summary>An email address.</summary>
    public string Email()
    {
        var first = FirstNames[Next(FirstNames.Length)].ToLowerInvariant();
        var last = LastNames[Next(LastNames.Length)].ToLowerInvariant();
        string[] domains = ["company.com", "example.com", "mail.org", "work.net"];
        return $"{first}.{last}@{domains[Next(domains.Length)]}";
    }

    /// <summary>A phone number.</summary>
    public string PhoneNumber()
    {
        int area = 200 + Next(800);
        int mid = 100 + Next(900);
        int end = 1000 + Next(9000);
        return string.Create(CultureInfo.InvariantCulture, $"({area}) {mid}-{end}");
    }

    /// <summary>A company name.</summary>
    public string CompanyName() => CompanyNames[Next(CompanyNames.Length)];
    /// <summary>A department name.</summary>
    public string Department() => Departments[Next(Departments.Length)];
    /// <summary>A job title.</summary>
    public string Role() => Roles[Next(Roles.Length)];
    /// <summary>A place name.</summary>
    public string Location() => Locations[Next(Locations.Length)];

    /// <summary>
    /// A whole number between <paramref name="min"/> and <paramref name="max"/> inclusive (0 to 1000 by
    /// default).
    /// </summary>
    public int Integer(int min = 0, int max = 1000) => min + Next(max - min + 1);

    /// <summary>
    /// A number between <paramref name="min"/> and <paramref name="max"/>, rounded to
    /// <paramref name="decimals"/> places (0 to 1000 with 2 places by default).
    /// </summary>
    public decimal Decimal(decimal min = 0, decimal max = 1000, int decimals = 2)
    {
        decimal raw = min + (Next(10000) / 10000m) * (max - min);
        return Math.Round(raw, decimals);
    }

    /// <summary>A whole-number percentage such as <c>42%</c> (0 to 100 by default).</summary>
    public string Percent(int min = 0, int max = 100) => string.Create(CultureInfo.InvariantCulture, $"{Integer(min, max)}%");

    /// <summary>
    /// A currency amount such as <c>$1,234.50</c>, between <paramref name="min"/> and <paramref name="max"/> (1
    /// to 10,000, with <c>$</c>, by default).
    /// </summary>
    public string Currency(decimal min = 1, decimal max = 10000, string symbol = "$")
        => string.Create(CultureInfo.InvariantCulture, $"{symbol}{Decimal(min, max, 2):N2}");

    /// <summary>A date as <c>MM/DD/YYYY</c>.</summary>
    public string ShortDate()
    {
        int m = 1 + Next(12);
        int d = 1 + Next(28);
        int y = 2020 + Next(7);
        return string.Create(CultureInfo.InvariantCulture, $"{m:D2}/{d:D2}/{y}");
    }

    /// <summary>A date written out in full.</summary>
    public string LongDate()
    {
        string[] months = ["January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December",];
        int m = Next(12);
        int d = 1 + Next(28);
        int y = 2020 + Next(7);
        return string.Create(CultureInfo.InvariantCulture, $"{d} {months[m]} {y}");
    }

    /// <summary>A month name.</summary>
    public string MonthLabel() => MonthLabels[Next(MonthLabels.Length)];

    /// <summary>A light colour that suits a background fill.</summary>
    public Color BackgroundColor() => BackgroundColors[Next(BackgroundColors.Length)];
    /// <summary>An accent colour.</summary>
    public Color Color() => AccentColors[Next(AccentColors.Length)];

    /// <summary>Invoice line items, ready for a table (5 by default).</summary>
    public IList<InvoiceLineItem> InvoiceLines(int count = 5)
    {
        var items = new List<InvoiceLineItem>(count);
        for (int i = 0; i < count; i++)
        {
            int idx = Next(ServiceNames.Length);
            int qty = 1 + Next(5);
            decimal price = 500 + Next(12) * 500;
            items.Add(new InvoiceLineItem(ServiceNames[idx], ServiceDescriptions[idx], qty, price, qty * price));
        }
        return items;
    }

    /// <summary>Several employee records for directory tables and team prototypes (10 by default).</summary>
    public IList<EmployeeItem> Employees(int count = 10)
    {
        var list = new List<EmployeeItem>(count);
        for (int i = 1; i <= count; i++)
        {
            list.Add(new EmployeeItem(
                Id: i,
                Name: Name(),
                Role: Role(),
                Department: Department(),
                Avatar: Avatar(48)));
        }
        return list;
    }

    /// <summary>
    /// Rows of table cells (name, department, location, percentage), as arrays of text (5 rows of 4 columns by
    /// default).
    /// </summary>
    public IList<string[]> TableRows(int rows = 5, int columns = 4)
    {
        var result = new List<string[]>(rows);
        for (int r = 0; r < rows; r++)
        {
            var row = new string[columns];
            for (int c = 0; c < columns; c++)
            {
                row[c] = c switch
                {
                    0 => Name(),
                    1 => Department(),
                    2 => Location(),
                    _ => Percent(),
                };
            }
            result.Add(row);
        }
        return result;
    }

    /// <summary>A series of whole numbers for a chart, one per month (12 between 50 and 300 by default).</summary>
    public IList<int> MonthlyValues(int count = 12, int min = 50, int max = 300)
    {
        var values = new List<int>(count);
        for (int i = 0; i < count; i++) values.Add(Integer(min, max));
        return values;
    }

    /// <summary>A PNG placeholder image of the requested size, as bytes.</summary>
    public byte[] Image(int width, int height) => BuildPlaceholderPng(width, height, Next(360));

    private static readonly (byte R, byte G, byte B)[] AvatarPalettes =
    [
        (59, 130, 246),  // Blue
        (16, 185, 129),  // Emerald
        (139, 92, 246),  // Purple
        (245, 158, 11),  // Amber
        (239, 68, 68),   // Red
        (20, 184, 166),  // Teal
        (236, 72, 153),  // Pink
        (99, 102, 241),  // Indigo
    ];

    /// <summary>
    /// Generates a circular avatar placeholder PNG with a silhouette of a person
    /// and a deterministic background color from a friendly palette.
    /// </summary>
    /// <param name="size">The width and height of the square PNG in pixels (48 by default).</param>
    public byte[] Avatar(int size = 48)
    {
        int clampedSize = Math.Max(8, Math.Min(size, 512));
        int paletteIdx = Next(AvatarPalettes.Length);
        return BuildPlaceholderAvatarPng(clampedSize, paletteIdx);
    }

    private static byte[] BuildPlaceholderAvatarPng(int size, int paletteIdx)
    {
        (byte bgR, byte bgG, byte bgB) = AvatarPalettes[paletteIdx % AvatarPalettes.Length];
        byte[] raw = new byte[size * (1 + size * 4)];

        double cx = (size - 1) / 2.0;
        double cy = (size - 1) / 2.0;
        double outerR = size / 2.0;

        double headCy = 0.38 * size;
        double headR = 0.20 * size;

        double bodyCy = 1.05 * size;
        double bodyR = 0.55 * size;

        for (int y = 0; y < size; y++)
        {
            int rowStart = y * (1 + size * 4);
            raw[rowStart] = 0; // filter: none
            double dy = y - cy;
            double dyHead = y - headCy;
            double dyBody = y - bodyCy;

            for (int x = 0; x < size; x++)
            {
                double dx = x - cx;
                double distOuter = Math.Sqrt(dx * dx + dy * dy);

                double deltaOuter = outerR - distOuter;
                double alphaOuter = 0;
                if (deltaOuter >= 0.5)
                    alphaOuter = 1;
                else if (deltaOuter > -0.5)
                    alphaOuter = deltaOuter + 0.5;

                int off = rowStart + 1 + x * 4;

                if (alphaOuter <= 0)
                {
                    raw[off] = 0;
                    raw[off + 1] = 0;
                    raw[off + 2] = 0;
                    raw[off + 3] = 0;
                    continue;
                }

                double distHead = Math.Sqrt(dx * dx + dyHead * dyHead);
                double distBody = Math.Sqrt(dx * dx + dyBody * dyBody);

                double dHead = headR - distHead;
                double dBody = bodyR - distBody;
                double dSil = Math.Max(dHead, dBody);

                double wSil = 0;
                if (dSil >= 0.5)
                    wSil = 1;
                else if (dSil > -0.5)
                    wSil = dSil + 0.5;

                byte r = (byte)Math.Clamp((int)Math.Round(bgR * (1 - wSil) + 255 * wSil), 0, 255);
                byte g = (byte)Math.Clamp((int)Math.Round(bgG * (1 - wSil) + 255 * wSil), 0, 255);
                byte b = (byte)Math.Clamp((int)Math.Round(bgB * (1 - wSil) + 255 * wSil), 0, 255);
                byte a = (byte)Math.Clamp((int)Math.Round(255 * alphaOuter), 0, 255);

                raw[off] = r;
                raw[off + 1] = g;
                raw[off + 2] = b;
                raw[off + 3] = a;
            }
        }

        using MemoryStream ms = new();
        ms.Write([137, 80, 78, 71, 13, 10, 26, 10]);

        byte[] ihdr = new byte[13];
        WriteU32(ihdr, 0, (uint)size);
        WriteU32(ihdr, 4, (uint)size);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 6; // RGBA
        WritePngChunk(ms, "IHDR", ihdr);

        byte[] deflated = DeflateRawSimple(raw);
        WritePngChunk(ms, "IDAT", deflated);

        WritePngChunk(ms, "IEND", []);

        return ms.ToArray();
    }

    private static byte[] BuildPlaceholderPng(int width, int height, int hueOffset)
    {
        byte[] raw = new byte[height * (1 + width * 3)];
        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (1 + width * 3);
            raw[rowStart] = 0; // filter: none
            for (int x = 0; x < width; x++)
            {
                byte r = (byte)(200 + (int)(40 * Math.Sin((x + hueOffset) * 0.05)));
                byte g = (byte)(200 + (int)(40 * Math.Sin((y + hueOffset) * 0.05)));
                byte b = 210;
                int off = rowStart + 1 + x * 3;
                raw[off] = r;
                raw[off + 1] = g;
                raw[off + 2] = b;
            }
        }

        using var ms = new MemoryStream();
        // PNG signature
        ms.Write([137, 80, 78, 71, 13, 10, 26, 10]);

        // IHDR
        byte[] ihdr = new byte[13];
        WriteU32(ihdr, 0, (uint)width);
        WriteU32(ihdr, 4, (uint)height);
        ihdr[8] = 8; // bit depth
        ihdr[9] = 2; // color type RGB
        WritePngChunk(ms, "IHDR", ihdr);

        // IDAT - uncompressed deflate wrapped in zlib
        byte[] deflated = DeflateRawSimple(raw);
        WritePngChunk(ms, "IDAT", deflated);

        // IEND
        WritePngChunk(ms, "IEND", []);

        return ms.ToArray();
    }

    private static void WritePngChunk(Stream stream, string type, byte[] data)
    {
        byte[] len = new byte[4];
        WriteU32(len, 0, (uint)data.Length);
        stream.Write(len);

        byte[] typeBytes = new byte[4];
        for (int i = 0; i < 4; i++) typeBytes[i] = (byte)type[i];
        stream.Write(typeBytes);
        stream.Write(data);

        byte[] crcInput = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, crcInput, 0, 4);
        Array.Copy(data, 0, crcInput, 4, data.Length);
        byte[] crc = new byte[4];
        WriteU32(crc, 0, Crc32(crcInput));
        stream.Write(crc);
    }

    private static void WriteU32(byte[] buf, int offset, uint value)
    {
        buf[offset] = (byte)(value >> 24);
        buf[offset + 1] = (byte)(value >> 16);
        buf[offset + 2] = (byte)(value >> 8);
        buf[offset + 3] = (byte)value;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int j = 0; j < 8; j++)
                crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xEDB88320u : 0);
        }
        return crc ^ 0xFFFFFFFF;
    }

    private static byte[] DeflateRawSimple(byte[] data)
    {
        const int blockSize = 65535;
        using var ms = new MemoryStream();
        ms.WriteByte(0x78); // CMF
        ms.WriteByte(0x01); // FLG

        for (int i = 0; i < data.Length; i += blockSize)
        {
            int end = Math.Min(i + blockSize, data.Length);
            int len = end - i;
            bool isFinal = end == data.Length;
            ms.WriteByte((byte)(isFinal ? 0x01 : 0x00));
            ms.WriteByte((byte)(len & 0xFF));
            ms.WriteByte((byte)((len >> 8) & 0xFF));
            ms.WriteByte((byte)(~len & 0xFF));
            ms.WriteByte((byte)((~len >> 8) & 0xFF));
            ms.Write(data, i, len);
        }

        byte[] adler = new byte[4];
        WriteU32(adler, 0, Adler32(data));
        ms.Write(adler);

        return ms.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }
}
