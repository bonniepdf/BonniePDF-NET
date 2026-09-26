using System.Globalization;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using static BonniePDF.Examples.Templates.FieldService.FieldKit;

namespace BonniePDF.Examples.Templates.FieldService;

/// <summary>
/// The check-in document a lettings or property-management platform issues at
/// the start of a tenancy: every room, every item in it, its condition and
/// cleanliness on the day, meter readings, keys handed over, and the signatures
/// of both parties. At check-out the same document is the reference against
/// which deposit deductions are argued - which is why it is written to be
/// unambiguous rather than brief.
///
/// Structurally this is the same register as the site inspection report and it
/// shares that family's kit. The differences are the ones that matter to a
/// letting agent: the register is grouped by room rather than flat, condition
/// and cleanliness are separate gradings, and the document ends with a tenant
/// declaration and a limitation-period notice.
///
/// Sample property, people, and references throughout.
/// </summary>
[System.ComponentModel.DisplayName("Inventory and schedule of condition")]
[System.ComponentModel.Category("Field service")]
[System.ComponentModel.Description("A tenancy check-in schedule: a room-by-room register grading condition and cleanliness separately, meter readings, keys issued, and both parties' declarations. PDF/A-2a + PDF/UA-1.")]
public static class InventoryScheduleOfConditionExample
{
    private const string Reference = "INV-2026-1174";
    private const string Property = "14 Sample Road, Sample Town, ST4 7QN";
    private const string PropertyType = "Two-bedroom first-floor flat, unfurnished with white goods";
    private const string LandlordAgent = "Sample Lettings Ltd";
    private const string Clerk = "Sample Clerk — Inventory Clerk (IC-0092)";
    private const string Tenant = "Sample Tenant";
    private const string InspectionType = "Check-in";
    private const string Weather = "Dry, 16°C — all rooms inspected in daylight";

    private static readonly DateTime Inspected = new(2026, 9, 4, 10, 30, 0);
    private static readonly DateTime TenancyStart = new(2026, 9, 5);

    private sealed record Item(
        string Name,
        string Condition,
        string Cleanliness,
        string Notes,
        string Evidence);

    private sealed record Room(string Name, Item[] Items);

    private static readonly Room[] Rooms =
    [
        new("Entrance hall",
        [
            new("Front door, frame and furniture", "Good", "Professional", "Two keys supplied; lock operates freely", "P-001"),
            new("Walls and ceiling — magnolia emulsion", "Good", "Professional", "Light scuff to the wall beside the meter cupboard, 40mm", "P-002"),
            new("Flooring — engineered oak", "Fair", "Professional", "Two shallow scratches near the threshold, pre-existing", "P-003"),
            new("Smoke alarm — ceiling mounted", "Good", "Professional", "Tested in the presence of the tenant; audible", "P-004"),
        ]),
        new("Living room",
        [
            new("Walls and ceiling", "Good", "Professional", "No marks noted", "P-005"),
            new("Window, frame and handles", "Good", "Professional", "Opens and locks; trickle vents clear", "P-006"),
            new("Carpet — mid grey", "Fair", "Professional", "Slight flattening in the traffic route, no staining", "P-007"),
            new("Radiator and thermostatic valve", "Good", "Domestic", "Light dust to the rear fins", "P-008"),
            new("Curtain pole and fittings", "Worn", "Domestic", "Finial loose at the left-hand end", "P-009"),
        ]),
        new("Kitchen",
        [
            new("Units, doors and handles", "Good", "Professional", "All doors aligned and closing", "P-010"),
            new("Worktop — laminate", "Fair", "Professional", "Two heat marks adjacent to the hob, pre-existing", "P-011"),
            new("Oven and hob", "Good", "Professional", "Cleaned to professional standard; all rings tested", "P-012"),
            new("Fridge freezer", "Good", "Professional", "Running, seals intact, shelves complete", "P-013"),
            new("Sink, taps and waste", "Good", "Professional", "No drips; waste runs freely", "P-014"),
            new("Flooring — vinyl", "Good", "Professional", "Sealed at the perimeter", "P-015"),
        ]),
        new("Bathroom",
        [
            new("Bath, shower and screen", "Good", "Professional", "Screen seal intact; shower tested", "P-016"),
            new("Basin, taps and waste", "Good", "Professional", "No drips noted", "P-017"),
            new("WC and cistern", "Good", "Professional", "Flushes and refills correctly", "P-018"),
            new("Tiling and grout", "Fair", "Professional", "Minor grout discolouration behind the bath taps", "P-019"),
            new("Extractor fan", "Good", "Domestic", "Operates on the light switch; grille lightly dusty", "P-020"),
        ]),
        new("Bedroom 1",
        [
            new("Walls and ceiling", "Good", "Professional", "Two filled and painted picture-hook holes to the chimney breast", "P-021"),
            new("Carpet", "Good", "Professional", "No marks noted", "P-022"),
            new("Built-in wardrobe", "Fair", "Professional", "Right-hand door catches slightly on closing", "P-023"),
            new("Window and blind", "Good", "Professional", "Blind raises and lowers evenly", "P-024"),
        ]),
        new("Bedroom 2",
        [
            new("Walls and ceiling", "Good", "Professional", "No marks noted", "P-025"),
            new("Carpet", "Damaged", "Professional", "Burn mark approx. 25mm diameter near the radiator — recorded at check-in, not the tenant’s liability", "P-026, P-027"),
            new("Radiator and valve", "Good", "Professional", "No leaks noted", "P-028"),
        ]),
    ];

    private sealed record Meter(string Utility, string Serial, string Reading, string Located);

    private static readonly Meter[] Meters =
    [
        new("Electricity", "E-88214-A", "41,208 kWh", "Hall meter cupboard"),
        new("Gas", "G-55190-C", "9,431 m³", "Hall meter cupboard"),
        new("Water", "W-31875-B", "0,772 m³", "External stopcock chamber"),
    ];

    private sealed record KeySet(string Set, int Quantity, string IssuedTo);

    private static readonly KeySet[] Keys =
    [
        new("Front door — communal", 2, "Tenant"),
        new("Flat door", 2, "Tenant"),
        new("Window lock", 1, "Tenant"),
        new("Bin store fob", 1, "Tenant"),
    ];

    private static readonly string[] ConditionOrder = ["Good", "Fair", "Worn", "Damaged"];

    private static Color ConditionColour(string condition) => condition switch
    {
        "Good" => StatusDone,
        "Fair" => StatusMedium,
        "Worn" => StatusHigh,
        _ => StatusCritical,
    };

    public static Document Generate()
    {
        Item[] allItems = [.. Rooms.SelectMany(room => room.Items)];
        Dictionary<string, int> byCondition = new(StringComparer.Ordinal);
        foreach (Item item in allItems)
        {
            byCondition[item.Condition] = byCondition.GetValueOrDefault(item.Condition) + 1;
        }

        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 44 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber => pageNumber == 1
            ? null
            : RunningBand($"{InspectionType} — {Reference}", Property);
        doc.Settings.FooterTemplate = pageNumber => RunningBand(
            "Retain for the tenancy — the check-out is compared against this",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"{InspectionType} inventory and schedule of condition — {Property}";
        doc.Metadata.Author = LandlordAgent;
        doc.Metadata.Subject =
            "Tenancy check-in inventory and schedule of condition: room-by-room register, meter readings, keys issued, and both parties’ declarations (sample data).";
        doc.Metadata.Keywords = "inventory, schedule of condition, check-in, lettings, tenancy, deposit, pdf/ua, pdf/a";

        // ── Title block ───────────────────────────────────────────────────────

        doc.Add(AccentBar());
        doc.Add(Eyebrow($"{InspectionType.ToUpperInvariant()} · INVENTORY AND SCHEDULE OF CONDITION"));
        doc.Add(H1(Property));
        doc.Add(Body(PropertyType, color: Muted, marginBottom: 14f));

        doc.Add(FieldRow(
            [
                Field("Reference", Reference),
                Field("Inspection type", InspectionType),
                Field("Inspected", $"{ShortDate(Inspected)} {Clock(Inspected)}"),
                Field("Tenancy starts", ShortDate(TenancyStart)),
            ],
            marginBottom: 6f));

        doc.Add(FieldRow(
        [
            Field("Managing agent", LandlordAgent),
            Field("Prepared by", Clerk),
            Field("Tenant", Tenant),
            Field("Conditions", Weather),
        ]));

        // ── Condition summary ─────────────────────────────────────────────────

        HorizontalStack tiles = new()
        {
            WidthDistribution = WidthDistribution.Weighted,
            Spacing = 10,
            Margin = new Thickness(0, 0, 0, 6),
        };
        tiles.Add(Tile(allItems.Length.ToString(CultureInfo.InvariantCulture), "Items recorded", Accent), 1);
        foreach (string condition in ConditionOrder)
        {
            tiles.Add(
                Tile(
                    byCondition.GetValueOrDefault(condition).ToString(CultureInfo.InvariantCulture),
                    condition,
                    ConditionColour(condition)),
                1);
        }

        doc.Add(tiles);

        doc.Add(Panel("How to read this document",
        [
            Body(
                "Condition describes wear; cleanliness describes how the item was left. Both are recorded as found "
                    + "on the inspection date and are the reference for the check-out comparison.",
                marginBottom: 3f,
                size: 9f),
            Body(
                "Anything recorded here as Worn or Damaged existed before the tenancy began and is not the "
                    + "tenant’s liability. Fair wear and tear over the term is expected and is not a deduction.",
                marginBottom: 0f,
                size: 9f),
        ]));

        // ── Room-by-room register ─────────────────────────────────────────────

        if (Rooms.Length == 0)
        {
            doc.Add(H2("Room-by-room schedule"));
        }
        else
        {
            bool firstRoom = true;
            foreach (Room room in Rooms)
            {
                VerticalStack roomStack = new() { BreakAllowed = false };
                if (firstRoom)
                {
                    roomStack.Add(H2("Room-by-room schedule"));
                    firstRoom = false;
                }
                roomStack.Add(H3(room.Name));

                Table table = new(3.1f, 1.1f, 1.3f, 4.2f, 0.95f)
                {
                    CellPadding = 5,
                    Border = new Border(0.5f),
                    BorderColors = new BorderColors(Rule),
                    HeaderBackgroundColor = Accent,
                    HeaderTextColor = Color.White,
                };
                table.AddHeader(Th("Item"), Th("Condition"), Th("Cleanliness"), Th("Notes as found"), Th("Plate"));
                foreach (Item item in room.Items)
                {
                    table.AddRow(
                        Td(item.Name),
                        StatusCell(item.Condition, ConditionColour(item.Condition)),
                        Td(item.Cleanliness, color: Muted),
                        Td(item.Notes),
                        Td(item.Evidence, color: Muted));
                }

                roomStack.Add(table);
                doc.Add(roomStack);
            }
        }

        // ── Meters and keys ───────────────────────────────────────────────────

        VerticalStack metersStack = new() { BreakAllowed = false };
        metersStack.Add(H2("Meter readings"));

        Table meters = new(1.4f, 1.6f, 1.6f, 2.4f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        meters.AddHeader(Th("Utility"), Th("Serial number"), Th("Reading at check-in"), Th("Location"));
        foreach (Meter meter in Meters)
        {
            meters.AddRow(
                Td(meter.Utility),
                Td(meter.Serial),
                Td(meter.Reading, bold: true),
                Td(meter.Located, color: Muted));
        }

        metersStack.Add(meters);
        doc.Add(metersStack);

        VerticalStack keysStack = new() { BreakAllowed = false };
        keysStack.Add(H2("Keys issued"));

        Table keys = new(3f, 1f, 1.6f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        keys.AddHeader(Th("Key or fob"), Th("Quantity"), Th("Issued to"));
        foreach (KeySet key in Keys)
        {
            keys.AddRow(
                Td(key.Set),
                Td(key.Quantity.ToString(CultureInfo.InvariantCulture)),
                Td(key.IssuedTo, color: Muted));
        }

        keysStack.Add(keys);
        doc.Add(keysStack);

        // ── Declarations ──────────────────────────────────────────────────────

        doc.Add(H2("Declarations"));
        doc.Add(Body(
            "The clerk confirms that this schedule records the condition of the property as found on the "
                + "inspection date. The tenant confirms that they have received a copy, have had the opportunity to "
                + "inspect the property, and either agree with its contents or have notified the agent of any "
                + "disagreement in writing.",
            color: Muted,
            size: 9f));

        doc.Add(Panel(
            "Period for comment",
            [
                Body(
                    "Any disagreement with this schedule must be notified to the managing agent in writing within "
                        + "seven days of the tenancy start date. After that period the schedule is taken as agreed "
                        + "for the purpose of the check-out comparison.",
                    marginBottom: 0f,
                    size: 9f),
            ],
            StatusHigh));

        doc.Add(SignatureRow(
        [
            SignatureSlot("Inventory clerk", Clerk, ShortDate(Inspected)),
            SignatureSlot("Tenant", Tenant),
            SignatureSlot("Managing agent", $"{LandlordAgent} — authorised signatory"),
        ]));

        doc.Add(Body(
            $"{Reference} · photographic plates P-001 to P-028 are issued as a separate annexe under the same "
                + "references and form part of this schedule.",
            color: Muted,
            marginTop: 10f,
            marginBottom: 0f,
            size: 8f));

        return doc;
    }

    // ── Local helpers ─────────────────────────────────────────────────────────

    private static string Clock(DateTime d) => d.ToString("HH:mm", CultureInfo.InvariantCulture);

    private static HorizontalStack RunningBand(string left, string right)
    {
        HorizontalStack row = new() { WidthDistribution = WidthDistribution.Weighted };
        row.Add(
            new TextBlock { Text = left, FontName = StandardFonts.Helvetica, FontSize = 7.5f, TextColor = Muted },
            2.5f);
        row.Add(
            new TextBlock
            {
                Text = right,
                FontName = StandardFonts.Helvetica,
                FontSize = 7.5f,
                TextColor = Muted,
                Alignment = TextAlignment.Right,
            },
            2);
        return row;
    }

    private static Container Tile(string value, string label, Color accent)
    {
        VerticalStack stack = new();
        stack.Add(new TextBlock
        {
            Text = value,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = accent,
            Margin = new Thickness(0, 0, 0, 2),
        });
        stack.Add(new TextBlock
        {
            Text = label,
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            TextColor = Muted,
        });

        return new Container
        {
            BackgroundColor = ColorPalette.Slate[50],
            Border = new Border(0.75f),
            BorderColors = new BorderColors(Rule),
            CornerRadius = 6,
            Padding = new Thickness(11, 10),
            Child = stack,
        };
    }
}
