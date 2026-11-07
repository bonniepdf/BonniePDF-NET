using System.Text;
using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>One order-confirmation letter in French, Greek, Arabic and Hebrew: the same letterhead and layout, mirrored for right-to-left Arabic and Hebrew. Each letter is set in a font for its script; digits the Hebrew face lacks come from the document's Latin font.</summary>
[System.ComponentModel.DisplayName("Multilingual Customer Letter")]
[System.ComponentModel.Category("Sales & Supply Chain")]
[System.ComponentModel.Description("One order confirmation in French, Greek, Arabic and Hebrew under the same letterhead, with the Arabic and Hebrew letters and their order tables mirrored right to left.")]
public static class MultilingualCustomerLetterExample
{
    private sealed record Letter(string Font, bool Rtl, string Title, string Address, string Date, string Body,
        string[] Columns, (string Item, string Quantity, string Price)[] Items, (string Label, string Amount) Total, string SignOff);

    private static readonly Letter[] Letters =
    [
        new("NotoSans-Regular.ttf", false,
            Title: "Confirmation de votre commande",
            Address: "Madame Zoë Martin\n18 rue des Écoles\n75005 Paris",
            Date: "25 octobre 2026",
            Body: "Merci pour votre commande. Elle est confirmée et sera livrée à votre adresse sous cinq jours ouvrés. Vous en trouverez le détail ci-dessous ; merci de conserver cette lettre.",
            Columns: ["Article", "Qté", "Prix"],
            Items: [("Lampe de lecture en laiton", "1", "120,00 €"), ("Plaid en lin", "1", "54,00 €")],
            Total: ("Total", "174,00 €"),
            SignOff: "Cordialement,\nLe service client"),
        new("NotoSans-Regular.ttf", false,
            Title: "Επιβεβαίωση παραγγελίας",
            Address: "Κυρία Ελένη Παπαδοπούλου\nΟδός Ερμού 24\n105 63 Αθήνα",
            Date: "25 Οκτωβρίου 2026",
            Body: "Σας ευχαριστούμε για την παραγγελία σας. Επιβεβαιώθηκε και θα παραδοθεί στη διεύθυνσή σας μέσα σε πέντε εργάσιμες ημέρες. Θα βρείτε τις λεπτομέρειες παρακάτω· παρακαλούμε κρατήστε αυτή την επιστολή.",
            Columns: ["Προϊόν", "Ποσ.", "Τιμή"],
            Items: [("Ορειχάλκινο φωτιστικό ανάγνωσης", "1", "120,00 €"), ("Λινό ριχτάρι", "1", "54,00 €")],
            Total: ("Σύνολο", "174,00 €"),
            SignOff: "Με εκτίμηση,\nΕξυπηρέτηση πελατών"),
        new("NotoNaskhArabic-Regular.ttf", true,
            Title: "تأكيد الطلب",
            Address: "السيدة مريم العتيبي\nحي العليا، الرياض 12214",
            Date: "25 أكتوبر 2026",
            Body: "شكراً لطلبكم. تم تأكيد الطلب وسيصل إلى عنوانكم خلال خمسة أيام عمل. يرجى مراجعة تفاصيل الطلب أدناه والاحتفاظ بهذه الرسالة.",
            Columns: ["المنتج", "الكمية", "السعر"],
            Items: [("مصباح قراءة نحاسي", "1", "450.00 ر.س"), ("غطاء من الكتان", "1", "205.00 ر.س")],
            Total: ("المجموع", "655.00 ر.س"),
            SignOff: "مع أطيب التحيات،\nخدمة العملاء"),
        // The Hebrew face has no digits: they fall back to the Latin font added for the French letter.
        new("NotoSerifHebrew-Regular.ttf", true,
            Title: "אישור הזמנה",
            Address: "גב׳ מרים לוי\nרחוב הרצל 12\nתל אביב 6578210",
            Date: "25 באוקטובר 2026",
            Body: "תודה על הזמנתכם. ההזמנה אושרה ותגיע לכתובתכם בתוך חמישה ימי עסקים. פרטי ההזמנה מופיעים להלן; נא לשמור מכתב זה.",
            Columns: ["מוצר", "כמות", "מחיר"],
            Items: [("מנורת קריאה מפליז", "1", "480.00 ₪"), ("שמיכת פשתן", "1", "215.00 ₪")],
            Total: ("סה״כ", "695.00 ₪"),
            SignOff: "בברכה,\nשירות הלקוחות"),
    ];

    private const string LogoAsset = "images/svg/atlas-homewares.svg";

    private const string FallbackLogoSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
          <circle cx="24" cy="24" r="20" fill="#f0fdfa" stroke="#0f766e" stroke-width="3"/>
          <path d="M24 4a26 26 0 0 0 0 40M24 4a26 26 0 0 1 0 40" fill="none" stroke="#0f766e" stroke-width="2.5"/>
          <path d="M6 17c5.5 3 11.5 4.5 18 4.5S36.5 20 42 17" fill="none" stroke="#14b8a6" stroke-width="2.5" stroke-linecap="round"/>
          <path d="M6 31c5.5-3 11.5-4.5 18-4.5S36.5 28 42 31" fill="none" stroke="#14b8a6" stroke-width="2.5" stroke-linecap="round"/>
          <line x1="4" y1="24" x2="44" y2="24" stroke="#0f766e" stroke-width="2.2"/>
        </svg>
        """;

    public static IReadOnlyList<string> RequiredAssets { get; } =
    [
        "fonts/NotoSans-Regular.ttf",
        "fonts/NotoNaskhArabic-Regular.ttf",
        "fonts/NotoSerifHebrew-Regular.ttf",
        LogoAsset,
    ];

    private static string LogoMarkup(IReadOnlyDictionary<string, byte[]>? assets) =>
        assets is not null && assets.TryGetValue(LogoAsset, out byte[]? bytes)
            ? Encoding.UTF8.GetString(bytes)
            : FallbackLogoSvg;

    private static Dictionary<string, byte[]> LoadDefaultAssets()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate))
            {
                return RequiredAssets.ToDictionary(p => p, p => File.ReadAllBytes(Path.Combine(candidate, p)), StringComparer.Ordinal);
            }

            string direct = Path.Combine(dir, "assets");
            if (Directory.Exists(direct))
            {
                return RequiredAssets.ToDictionary(p => p, p => File.ReadAllBytes(Path.Combine(direct, p)), StringComparer.Ordinal);
            }

            dir = Directory.GetParent(dir)?.FullName;
        }

        Dictionary<string, byte[]> dict = new(StringComparer.Ordinal);
        foreach (string p in RequiredAssets)
        {
            string full = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, p);
            if (File.Exists(full))
            {
                dict[p] = File.ReadAllBytes(full);
            }
        }

        return dict;
    }

    public static Document Generate() => Generate(LoadDefaultAssets());

    public static Document Generate(IReadOnlyDictionary<string, byte[]> assets)
    {
        var doc = new Document(new DocumentSettings
        {
            Language = "en-GB",
            Margin = 48,
            FooterTemplate = PageTemplate.FromElements(@default: new TextBlock("Atlas Homewares Ltd  ·  12 Exmouth Market, London EC1R 4QE  ·  atlas.example")
            {
                FontSize = 8,
                Color = ColorPalette.Slate[500],
                Alignment = TextAlignment.Center
            })
        })
        { Metadata = new DocumentMetadata { Title = "Multilingual customer letter" } };

        // Each font is added once, however many letters use it.
        var faces = new Dictionary<string, DocumentFontFace>();
        for (int index = 0; index < Letters.Length; index++)
        {
            Letter letter = Letters[index];
            if (index != 0) doc.Add(new PageBreak());
            if (!faces.TryGetValue(letter.Font, out DocumentFontFace? cached))
            {
                byte[] fontBytes = assets.TryGetValue($"fonts/{letter.Font}", out byte[]? b)
                    ? b
                    : File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", letter.Font));
                cached = doc.Fonts.Add(fontBytes);
                faces[letter.Font] = cached;
            }
            DocumentFontFace face = cached;
            TextBlock Body(string text, float size = 11, Color? color = null, TextAlignment align = TextAlignment.Start) => new(text)
            {
                FontName = face.FontName,
                FontSize = size,
                Color = color ?? ColorPalette.Slate[700],
                Alignment = align,
                LineHeight = 1.5f
            };

            // The letterhead is the same on every page, in the brand's own (Latin) lettering.
            HorizontalStack brand = new() { WidthDistribution = WidthDistribution.Auto, Spacing = 8 };
            brand.Add(new Svg { SvgData = LogoMarkup(assets), Width = 22, Height = 22, IsDecorative = true });
            brand.Add(new TextBlock("ATLAS HOMEWARES") { FontName = StandardFonts.HelveticaBold, FontSize = 16, LetterSpacing = 2, Color = ColorPalette.Teal[700] });
            HorizontalStack letterhead = new() { WidthDistribution = WidthDistribution.Equal };
            letterhead.Add(brand);
            letterhead.Add(new TextBlock("London  ·  Paris  ·  Athens  ·  Riyadh") { FontSize = 9, Color = ColorPalette.Slate[500], Alignment = TextAlignment.Right });
            doc.Add(new Container
            {
                Border = new Border(0, 0, 0, 2),
                BorderColors = new BorderColors(ColorPalette.Teal[600]),
                Padding = new Thickness(0, 0, 0, 10),
                Margin = new Thickness(0, 0, 0, 28),
                Child = letterhead
            });

            // Everything below follows the letter's direction: start is the right edge in Arabic and Hebrew, and
            // the order table's columns mirror.
            Table table = new(4f, 1f, 1.6f)
            {
                CellPadding = 7,
                HeaderBackgroundColor = ColorPalette.Teal[700],
                HeaderTextColor = Color.White,
                Border = new Border(0.5f),
                BorderColors = new BorderColors(ColorPalette.Slate[200])
            };
            table.AddHeader(letter.Columns.Select((c, i) => (IElement)Body(c, 10, Color.White, i == 2 ? TextAlignment.End : TextAlignment.Start)).ToArray());
            foreach (var (item, quantity, price) in letter.Items)
                table.AddRow(Body(item), Body(quantity), Body(price, align: TextAlignment.End));
            table.AddRow(Body(letter.Total.Label, color: ColorPalette.Slate[900]), Body(""), Body(letter.Total.Amount, color: ColorPalette.Slate[900], align: TextAlignment.End));

            HorizontalStack heading = new() { WidthDistribution = WidthDistribution.Equal };
            heading.Add(Body(letter.Address));
            heading.Add(Body(letter.Date, color: ColorPalette.Slate[500], align: TextAlignment.End));

            VerticalStack content = new()
            {
                ContentDirection = letter.Rtl ? ContentDirection.RightToLeft : ContentDirection.LeftToRight,
                Spacing = 16
            };
            content.Add(heading);
            content.Add(new Heading(letter.Title)
            {
                Level = index == 0 ? 1 : 2,
                FontName = face.FontName,
                FontSize = 22,
                Color = ColorPalette.Slate[900],
                Alignment = TextAlignment.Start,
                Margin = new Thickness(0, 12, 0, 0)
            });
            content.Add(Body(letter.Body));
            content.Add(table);
            content.Add(Body(letter.SignOff, color: ColorPalette.Slate[900]));
            doc.Add(content);
        }
        return doc;
    }
}
