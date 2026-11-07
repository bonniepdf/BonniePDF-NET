using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>One order-confirmation letter in French, Greek, Arabic and Hebrew: the same letterhead and layout, mirrored for right-to-left Arabic and Hebrew. Each letter is set in a font for its script; digits the Hebrew face lacks come from the document's Latin font.</summary>
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

    public static Document Generate()
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
                cached = doc.Fonts.Add(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", letter.Font)));
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
            HorizontalStack letterhead = new() { WidthDistribution = WidthDistribution.Equal };
            letterhead.Add(new TextBlock("ATLAS HOMEWARES") { FontName = StandardFonts.HelveticaBold, FontSize = 16, LetterSpacing = 2, Color = ColorPalette.Teal[700] });
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
