using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Lists;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.OutpatientAppointmentLetter;

/// <summary>
/// Accessible fictional outpatient appointment letter: a masthead, a prominent
/// sample-data banner, a bordered appointment-details card, plain-language
/// actions, an "if you feel unwell" notice, an accessibility contact, and a
/// clear demonstration disclaimer. Bookmarked, accessibility-first PDF/A-2a +
/// PDF/UA-1. It borrows broad UK public-health information-design principles
/// without copying any specific provider's branding, names, or contact details.
/// </summary>
[System.ComponentModel.DisplayName("Outpatient Appointment")]
[System.ComponentModel.Category("Healthcare")]
[System.ComponentModel.Description("Accessible fictional outpatient appointment letter with clear appointment details, plain-language actions, an accessibility contact, and prominent sample-data disclaimers. PDF/A-2a + PDF/UA-1.")]
public static class OutpatientAppointmentLetterExample
{
    private const float Margin = 48;

    private static readonly Color Ink = Color.FromHex("#17324d");
    private static readonly Color BodyText = Color.FromHex("#2f4858");
    private static readonly Color Muted = Color.FromHex("#5d6f7c");
    private static readonly Color Blue = Color.FromHex("#176b87");
    private static readonly Color BlueDark = Color.FromHex("#11566d");
    private static readonly Color BluePale = Color.FromHex("#eaf4f8");
    private static readonly Color BlueBorder = Color.FromHex("#b8d8e3");
    private static readonly Color Notice = Color.FromHex("#fff4ce");
    private static readonly Color NoticeBorder = Color.FromHex("#d69e00");

    private static readonly Detail[] Appointment =
    [
        new("Date", "Tuesday 15 September 2026"),
        new("Time", "10:30 am"),
        new("Clinic", "Cardiology outpatient clinic"),
        new("Clinician", "Dr Sample Clinician"),
        new("Location", "Sample Healthcare Centre\nLevel 2, Blue reception"),
        new("Patient number", "SAMPLE-2026-0148"),
    ];

    private static readonly string[] Actions =
    [
        "Arrive 15 minutes before your appointment and check in at Blue reception.",
        "Bring an up-to-date list of any medicines you take.",
        "Tell us as soon as possible if you cannot attend so the appointment can be rearranged.",
        "Contact us before your visit if you need an interpreter, step-free access, or information in another format.",
    ];

    public static Document Generate()
    {
        DocumentSettings settings = new(PdfSize.A4, Margin)
        {
            Conformance = PdfAConformance.PdfA2a,
            Accessibility = AccessibilityMode.PdfUa1,
            Language = "en-GB",
            DisplayDocTitle = true,
            FooterTemplate = pageNumber => new TextBlock
            {
                Text = $"SAMPLE DOCUMENT - FICTIONAL DATA  |  Not issued by any health service provider  |  Page {pageNumber} of {TextElementBase.TotalPagesToken}",
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                Color = Muted,
                Alignment = TextAlignment.Center,
            },
        };
        settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(11).WithColor(BodyText);

        Document doc = new(settings);
        doc.Metadata.Title = "Fictional Outpatient Appointment";
        doc.Metadata.Author = "Sample Healthcare";
        doc.Metadata.Subject = "Accessible Outpatient Appointment using entirely fictional data.";
        doc.Metadata.Keywords = "sample, healthcare, appointment, accessibility, fictional data";

        doc.Add(Masthead());
        doc.Add(SampleBanner());

        doc.Add(new Heading("Your outpatient appointment")
        {
            TocIdentifier = "appointment",
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 25,
            Color = Ink,
            Margin = new Thickness(0, 0, 0, 14),
        });

        doc.Add(Body("Dear Sample Patient,", bold: true, mb: 8));
        doc.Add(Body(
            "We have arranged an outpatient appointment for you. The main details are shown below. " +
            "Please read the information on this page before you attend.", mb: 18));

        doc.Add(AppointmentCard());
        AddActionsAndContact(doc);

        return doc;
    }

    private static void AddActionsAndContact(Document doc)
    {
        doc.Add(SectionHeading("What you need to do", "what-to-do"));

        UnorderedList actions = new()
        {
            FontName = StandardFonts.Helvetica,
            FontSize = 10.5f,
            Color = BodyText,
            BulletColor = Blue,
            ItemSpacing = 4,
            Margin = new Thickness(0, 0, 0, 18),
        };
        foreach (string action in Actions)
        {
            actions.AddItem(action);
        }

        doc.Add(actions);
        doc.Add(UnwellNotice());

        doc.Add(SectionHeading("Contact and accessibility", "contact"));
        doc.Add(Body(
            "For this fictional example, contact appointments@example.com. Tell the service if you " +
            "need the letter in large print, audio, easy read, Braille, or another language.", mb: 18));

        doc.Add(Disclaimer());
    }

    private static Grid Masthead()
    {
        Grid masthead = new(1.5f, 1) { CellPadding = 0, Margin = new Thickness(0, 0, 0, 8) };

        VerticalStack brand = new() { Spacing = 3 };
        brand.Add(new TextBlock
        {
            Text = "Sample Healthcare",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 21,
            Color = BlueDark,
        });
        brand.Add(new TextBlock { Text = "Outpatient services", FontSize = 10, Color = Muted });

        VerticalStack reference = new() { Spacing = 3 };
        reference.Add(new TextBlock { Text = "Issued 1 August 2026", FontSize = 9.5f, Color = Muted, Alignment = TextAlignment.Right });
        reference.Add(new TextBlock { Text = "Reference SAMPLE-OP-1042", FontSize = 9.5f, Color = Muted, Alignment = TextAlignment.Right });

        masthead.AddRow(brand, reference);
        return masthead;
    }

    private static Container SampleBanner() => new()
    {
        BackgroundColor = Blue,
        Padding = new Thickness(12, 7, 12, 7),
        Margin = new Thickness(0, 0, 0, 20),
        Child = new TextBlock
        {
            Text = "SAMPLE DOCUMENT - FICTIONAL DATA",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            Color = Color.White,
            Alignment = TextAlignment.Center,
        },
    };

    private static Container AppointmentCard()
    {
        Grid details = new(1, 1) { CellPadding = 5 };
        details.AddRow(DetailStack(Appointment[0]), DetailStack(Appointment[1]));
        details.AddRow(DetailStack(Appointment[2]), DetailStack(Appointment[3]));
        details.AddRow(DetailStack(Appointment[4]), DetailStack(Appointment[5]));

        VerticalStack stack = new() { Spacing = 8 };
        stack.Add(new Heading("Appointment details")
        {
            TocIdentifier = "appointment-details",
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            Color = BlueDark,
        });
        stack.Add(details);

        return new Container
        {
            BackgroundColor = BluePale,
            Border = new Border(1),
            BorderColors = new BorderColors(BlueBorder),
            Padding = new Thickness(15),
            Margin = new Thickness(0, 0, 0, 20),
            Child = stack,
        };
    }

    private static Container UnwellNotice()
    {
        VerticalStack stack = new() { Spacing = 5 };
        stack.Add(new TextBlock
        {
            Text = "If you feel unwell before the appointment",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 11,
            Color = Ink,
        });
        stack.Add(new TextBlock
        {
            Text = "Use the appropriate local healthcare service for advice. This sample letter does not " +
                   "provide medical or emergency guidance.",
            FontSize = 10,
            Color = BodyText,
        });

        return new Container
        {
            BackgroundColor = Notice,
            Border = new Border(1),
            BorderColors = new BorderColors(NoticeBorder),
            Padding = new Thickness(13),
            Margin = new Thickness(0, 0, 0, 18),
            Child = stack,
        };
    }

    private static Container Disclaimer() => new()
    {
        BackgroundColor = Color.FromHex("#f4f7f9"),
        Padding = new Thickness(11),
        Child = new TextBlock
        {
            Text = "Demonstration document generated by BonniePDF. All people, identifiers, organisations, " +
                   "locations and contact details are fictional. Not issued by any health service provider.",
            FontName = StandardFonts.HelveticaOblique,
            FontSize = 8.5f,
            Color = Muted,
            Alignment = TextAlignment.Center,
        },
    };

    private static Heading SectionHeading(string text, string tocIdentifier) => new(text)
    {
        TocIdentifier = tocIdentifier,
        Level = 2,
        FontName = StandardFonts.HelveticaBold,
        FontSize = 16,
        Color = Ink,
        Margin = new Thickness(0, 0, 0, 8),
    };

    private static TextBlock Body(string text, bool bold = false, float mb = 12) => new()
    {
        Text = text,
        FontName = bold ? StandardFonts.HelveticaBold : StandardFonts.Helvetica,
        FontSize = 11,
        Color = BodyText,
        Margin = new Thickness(0, 0, 0, mb),
    };

    private static VerticalStack DetailStack(Detail item)
    {
        VerticalStack stack = new() { Spacing = 3 };
        stack.Add(new TextBlock
        {
            Text = item.Label.ToUpperInvariant(),
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            Color = BlueDark,
        });
        stack.Add(new TextBlock
        {
            Text = item.Value,
            FontName = StandardFonts.Helvetica,
            FontSize = 10.5f,
            Color = Ink,
        });
        return stack;
    }

    private sealed record Detail(string Label, string Value);
}
