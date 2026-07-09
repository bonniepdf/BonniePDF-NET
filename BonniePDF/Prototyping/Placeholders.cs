using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BonniePDF.Core;

namespace BonniePDF.Prototyping;

public static class Placeholders
{
    private static PlaceholderSource _global = new(42);

    public static string Word() => _global.Word();
    public static string Label() => _global.Label();
    public static string Sentence() => _global.Sentence();
    public static string Paragraph() => _global.Paragraph();
    public static string Paragraphs(int count = 3) => _global.Paragraphs(count);

    public static string Name() => _global.Name();
    public static string FirstName() => _global.FirstName();
    public static string LastName() => _global.LastName();
    public static string Email() => _global.Email();
    public static string PhoneNumber() => _global.PhoneNumber();
    public static string CompanyName() => _global.CompanyName();
    public static string Department() => _global.Department();
    public static string Role() => _global.Role();
    public static string Location() => _global.Location();

    public static int Integer(int min = 0, int max = 1000) => _global.Integer(min, max);
    public static decimal Decimal(decimal min = 0, decimal max = 1000, int decimals = 2) => _global.Decimal(min, max, decimals);
    public static string Percent(int min = 0, int max = 100) => _global.Percent(min, max);
    public static string Currency(decimal min = 1, decimal max = 10000, string symbol = "$") => _global.Currency(min, max, symbol);

    public static string ShortDate() => _global.ShortDate();
    public static string LongDate() => _global.LongDate();
    public static string MonthLabel() => _global.MonthLabel();

    public static Color BackgroundColor() => _global.BackgroundColor();
    public static Color Color() => _global.Color();

    public static IList<InvoiceLineItem> InvoiceLines(int count = 5) => _global.InvoiceLines(count);
    public static IList<string[]> TableRows(int rows = 5, int columns = 4) => _global.TableRows(rows, columns);
    public static IList<int> MonthlyValues(int count = 12, int min = 50, int max = 300) => _global.MonthlyValues(count, min, max);

    public static byte[] Image(int width, int height) => _global.Image(width, height);

    public static PlaceholderSource WithSeed(int seed) => new(seed);

    public static void Reset() => _global = new PlaceholderSource(42);
}

public record InvoiceLineItem(string Service, string Description, int Quantity, decimal UnitPrice, decimal Total);

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
    private static readonly string[] Roles = ["Manager", "Lead", "Senior", "Junior", "Intern", "Director", "VP"];
    private static readonly string[] Locations = ["New York", "London", "Berlin", "Tokyo", "Sydney", "Toronto", "Paris"];

    private static readonly string[] CompanyNames =
    [
        "Meridian Labs", "Studio Eleven", "Acme Corporation", "Northwind Traders",
        "Contoso Ltd", "Alpine Software", "Lunar Industries", "Verdant Solutions",
        "Pinnacle Systems", "Cobalt Dynamics",
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
        Core.ColorPalette.Blue[50], Core.ColorPalette.Green[50], Core.ColorPalette.Amber[50], Core.ColorPalette.Rose[50],
        Core.ColorPalette.Indigo[50], Core.ColorPalette.Slate[50], Core.ColorPalette.Purple[50], Core.ColorPalette.Cyan[50],
    ];

    private static readonly Color[] AccentColors =
    [
        Core.ColorPalette.Indigo[600], Core.ColorPalette.Blue[600], Core.ColorPalette.Green[600], Core.ColorPalette.Amber[600],
        Core.ColorPalette.Rose[600], Core.ColorPalette.Purple[600], Core.ColorPalette.Cyan[600], Core.ColorPalette.Slate[600],
    ];

    public string Word()
    {
        var words = LoremSentences[Next(LoremSentences.Length)].Split(' ');
        return words[Next(words.Length)].Trim('.', ',');
    }

    public string Label()
    {
        var w = $"{Word()} {Word()}";
        return char.ToUpper(w[0]) + w[1..];
    }

    public string Sentence() => LoremSentences[Next(LoremSentences.Length)];

    public string Paragraph()
    {
        int count = 3 + Next(4);
        var sentences = new List<string>(count);
        for (int i = 0; i < count; i++) sentences.Add(Sentence());
        return string.Join(" ", sentences);
    }

    public string Paragraphs(int count = 3)
    {
        var paras = new List<string>(count);
        for (int i = 0; i < count; i++) paras.Add(Paragraph());
        return string.Join("\n\n", paras);
    }

    public string Name() => $"{FirstNames[Next(FirstNames.Length)]} {LastNames[Next(LastNames.Length)]}";
    public string FirstName() => FirstNames[Next(FirstNames.Length)];
    public string LastName() => LastNames[Next(LastNames.Length)];

    public string Email()
    {
        var first = FirstNames[Next(FirstNames.Length)].ToLowerInvariant();
        var last = LastNames[Next(LastNames.Length)].ToLowerInvariant();
        string[] domains = ["company.com", "example.com", "mail.org", "work.net"];
        return $"{first}.{last}@{domains[Next(domains.Length)]}";
    }

    public string PhoneNumber()
    {
        int area = 200 + Next(800);
        int mid = 100 + Next(900);
        int end = 1000 + Next(9000);
        return string.Create(CultureInfo.InvariantCulture, $"({area}) {mid}-{end}");
    }

    public string CompanyName() => CompanyNames[Next(CompanyNames.Length)];
    public string Department() => Departments[Next(Departments.Length)];
    public string Role() => Roles[Next(Roles.Length)];
    public string Location() => Locations[Next(Locations.Length)];

    public int Integer(int min = 0, int max = 1000) => min + Next(max - min + 1);

    public decimal Decimal(decimal min = 0, decimal max = 1000, int decimals = 2)
    {
        decimal raw = min + (Next(10000) / 10000m) * (max - min);
        return Math.Round(raw, decimals);
    }

    public string Percent(int min = 0, int max = 100) => string.Create(CultureInfo.InvariantCulture, $"{Integer(min, max)}%");

    public string Currency(decimal min = 1, decimal max = 10000, string symbol = "$")
        => string.Create(CultureInfo.InvariantCulture, $"{symbol}{Decimal(min, max, 2):N2}");

    public string ShortDate()
    {
        int m = 1 + Next(12);
        int d = 1 + Next(28);
        int y = 2020 + Next(7);
        return string.Create(CultureInfo.InvariantCulture, $"{m:D2}/{d:D2}/{y}");
    }

    public string LongDate()
    {
        string[] months = ["January", "February", "March", "April", "May", "June",
            "July", "August", "September", "October", "November", "December",];
        int m = Next(12);
        int d = 1 + Next(28);
        int y = 2020 + Next(7);
        return string.Create(CultureInfo.InvariantCulture, $"{d} {months[m]} {y}");
    }

    public string MonthLabel() => MonthLabels[Next(MonthLabels.Length)];

    public Color BackgroundColor() => BackgroundColors[Next(BackgroundColors.Length)];
    public Color Color() => AccentColors[Next(AccentColors.Length)];

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

    public IList<int> MonthlyValues(int count = 12, int min = 50, int max = 300)
    {
        var values = new List<int>(count);
        for (int i = 0; i < count; i++) values.Add(Integer(min, max));
        return values;
    }

    public byte[] Image(int width, int height) => BuildPlaceholderPng(width, height, Next(360));

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
