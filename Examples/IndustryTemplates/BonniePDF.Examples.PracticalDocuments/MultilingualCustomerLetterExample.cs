using BonniePDF;

namespace BonniePDF.Examples.PracticalDocuments;

/// <summary>A customer letter localized into French, Arabic and Hebrew with explicit font bytes.</summary>
public static class MultilingualCustomerLetterExample
{
    public static Document Generate()
    {
        var doc = new Document(new DocumentSettings { Language = "en-GB", Margin = 48 }) { Metadata = new DocumentMetadata { Title = "Multilingual customer letter" } };
        var sections = new[] {
            (Font: "NotoSans-Regular.ttf", Title: "Confirmation de votre commande", Address: "Madame Zoë Martin\n18 rue des Écoles\n75005 Paris", Body: "Votre commande est confirmée. La livraison est prévue à votre adresse. Merci de vérifier les coordonnées et de conserver cette lettre.", Date: "25 octobre 2026", Amount: "1 234,50 EUR", Rtl: false),
            (Font: "NotoNaskhArabic-Regular.ttf", Title: "تأكيد الطلب", Address: "السيدة مريم\nالرياض", Body: "تم تأكيد طلبكم. يرجى مراجعة العنوان والاحتفاظ بهذه الرسالة. شكراً لكم.", Date: "25 أكتوبر 2026", Amount: "1,234.50 SAR", Rtl: true),
            (Font: "NotoSerifHebrew-Regular.ttf", Title: "אישור הזמנה", Address: "גב׳ מרים לוי\nתל אביב", Body: "ההזמנה שלכם אושרה. נא לבדוק את הכתובת ולשמור את המכתב. תודה רבה.", Date: "25 באוקטובר 2026", Amount: "1,234.50 ILS", Rtl: true)
        };
        for (int i = 0; i < sections.Length; i++)
        {
            if (i != 0) doc.Add(new PageBreak());
            var section = sections[i];
            var face = doc.Fonts.Add(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fonts", section.Font)));
            doc.Add(new Heading(section.Title) { FontName = face.FontName, FontSize = 26, Level = i == 0 ? 1 : 2, Alignment = section.Rtl ? TextAlignment.Right : TextAlignment.Left, ContentDirection = section.Rtl ? ContentDirection.RightToLeft : ContentDirection.LeftToRight, Margin = new Thickness(0, 0, 0, 20) });
            foreach (string text in new[] { section.Address, section.Date, section.Body }) doc.Add(new TextBlock(text) {
                FontName = face.FontName, FontSize = 14, Alignment = section.Rtl ? TextAlignment.Right : TextAlignment.Left,
                ContentDirection = section.Rtl ? ContentDirection.RightToLeft : ContentDirection.LeftToRight, Margin = new Thickness(0, 0, 0, 20) });
            doc.Add(new TextBlock(section.Amount) { FontName = face.FontName, FontSize = 14, Alignment = section.Rtl ? TextAlignment.Right : TextAlignment.Left,
                ContentDirection = ContentDirection.LeftToRight, Margin = new Thickness(0, 0, 0, 20) });
        }
        return doc;
    }
}
