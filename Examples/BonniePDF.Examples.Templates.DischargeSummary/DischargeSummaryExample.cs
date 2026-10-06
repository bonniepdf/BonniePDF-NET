using BonniePDF;
using static BonniePDF.Examples.Shared.TradeKit;

namespace BonniePDF.Examples.Templates.DischargeSummary;

/// <summary>
/// The document healthcare software is actually built around, and the one the
/// existing healthcare showcases were missing: the summary that goes to the
/// patient's GP when they leave hospital. Every section of it is read by someone
/// in a hurry, so the ordering is fixed by convention rather than by design
/// preference - diagnosis, then what was done, then medication changes, then
/// what the GP must do next.
///
/// It is also where the accessibility argument is easiest to make concretely.
/// A medication table read out of order by a screen reader is a clinical risk,
/// not a usability complaint: the dose has to stay with the drug it belongs to,
/// and a change of dose has to be announced as a change. That is what the tagged
/// structure here is for.
///
/// Every patient, clinician, identifier, and clinical value is a sample. Nothing
/// here is clinical guidance.
/// </summary>
[System.ComponentModel.DisplayName("Discharge summary")]
[System.ComponentModel.Category("Healthcare")]
[System.ComponentModel.Description("A hospital discharge summary: diagnoses, the admission narrative, key investigations, a medication list marking every change from the pre-admission regimen, and numbered actions for the GP. PDF/A-2a + PDF/UA-1.")]
public static class DischargeSummaryExample
{
    private const string Reference = "DS-2026-44127";
    private const string Patient = "Sample Patient";
    private const string PatientId = "NHS 000 000 0000 (sample)";
    private const string DateOfBirth = "14 Mar 1958";
    private const string Address = "Sample Address, Sample Town, ST2 6PL";
    private const string Ward = "Sample Ward 6 — Cardiology";
    private const string Consultant = "Dr S. Sample — Consultant Cardiologist";
    private const string PreparedBy = "Dr A. Sample — Registrar";
    private const string GpPractice = "Sample Medical Practice, Sample Town";
    private const string Method = "Emergency admission via the emergency department";
    private const string Destination = "Discharged home, independent";

    private static readonly DateTime Admitted = new(2026, 9, 3);
    private static readonly DateTime Discharged = new(2026, 9, 9);

    private const string PrimaryDiagnosis = "Non-ST-elevation myocardial infarction";

    private static readonly string[] SecondaryDiagnoses =
    [
        "Type 2 diabetes mellitus, diet controlled",
        "Essential hypertension",
        "Stage 2 chronic kidney disease",
    ];

    private sealed record Medication(
        string Drug,
        string Dose,
        string Route,
        string Frequency,
        string Change,
        string Note);

    private static readonly Medication[] Medications =
    [
        new("Aspirin", "75 mg", "Oral", "Once daily", "New", "Started on admission; continue indefinitely"),
        new("Ticagrelor", "90 mg", "Oral", "Twice daily", "New", "Continue for 12 months, then review"),
        new("Atorvastatin", "80 mg", "Oral", "Once daily at night", "Dose changed", "Increased from 20 mg; repeat lipids at 3 months"),
        new("Bisoprolol", "2.5 mg", "Oral", "Once daily", "New", "Titrate to heart rate and blood pressure in primary care"),
        new("Ramipril", "2.5 mg", "Oral", "Once daily", "New", "Check renal function and potassium in 1–2 weeks"),
        new("Metformin", "1 g", "Oral", "Twice daily", "Unchanged", "Continue at the pre-admission dose"),
        new("Ibuprofen", "400 mg", "Oral", "As required", "Stopped", "Stopped — avoid NSAIDs with the new antiplatelet regimen"),
    ];

    private static Color ChangeColour(string change) => change switch
    {
        "New" => Positive,
        "Dose changed" => Accent,
        "Stopped" => Negative,
        _ => Muted,
    };

    private sealed record Investigation(string Test, string Result, string Reference, string Flag);

    private static readonly Investigation[] Investigations =
    [
        new("High-sensitivity troponin T", "148 ng/L, peak 412 ng/L", "< 14 ng/L", "Raised"),
        new("ECG on admission", "Sinus rhythm, lateral T-wave inversion", "—", "Abnormal"),
        new("Echocardiogram", "LV ejection fraction 48%, mild hypokinesia", "> 55%", "Abnormal"),
        new("eGFR", "68 mL/min/1.73m²", "> 90", "Low"),
        new("HbA1c", "52 mmol/mol", "< 48", "Raised"),
        new("Haemoglobin", "134 g/L", "130–180", "Normal"),
    ];

    private static readonly string[] GpActions =
    [
        "Check urea, electrolytes and creatinine 1–2 weeks after discharge, following the ramipril start.",
        "Titrate bisoprolol and ramipril to target as tolerated, reviewing blood pressure and heart rate at each step.",
        "Repeat the lipid profile at three months and review the atorvastatin dose against target.",
        "Review diabetes control in light of the raised HbA1c, and refer to the practice diabetes clinic.",
        "Ensure the patient is added to the cardiac rehabilitation pathway if they have not been contacted within two weeks.",
    ];

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 46 });
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9.5f).WithColor(Ink);
        doc.Settings.HeaderTemplate = pageNumber => pageNumber == 1
            ? null
            : Band($"Discharge summary {Reference}", $"{Patient} · {PatientId}");
        doc.Settings.FooterTemplate = pageNumber => Band(
            "Confidential patient information",
            $"Page {pageNumber} of {TextElementBase.TotalPagesToken}");

        doc.Metadata.Title = $"Discharge summary {Reference} — {Patient}";
        doc.Metadata.Author = "Sample Hospital Trust";
        doc.Metadata.Subject =
            "Hospital discharge summary: diagnoses, admission narrative, investigations, medication changes on discharge, and actions for the general practitioner (sample data).";
        doc.Metadata.Keywords =
            "discharge summary, hospital, medication reconciliation, primary care, handover, pdf/ua, pdf/a";

        doc.Add(AccentBar());
        doc.Add(Cell(
            "DISCHARGE SUMMARY · FOR THE ATTENTION OF THE GENERAL PRACTITIONER",
            size: 8.5f,
            bold: true,
            color: Accent,
            marginBottom: 4f));
        doc.Add(H1("Inpatient discharge summary"));
        doc.Add(Cell($"{Ward} · {Consultant}", color: Muted, marginBottom: 14f));

        doc.Add(WeightedRow(
            [
                Field("Patient", Patient),
                Field("Identifier", PatientId),
                Field("Date of birth", DateOfBirth),
                Field("Summary reference", Reference),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
            [
                Field("Admitted", GbDate(Admitted)),
                Field("Discharged", GbDate(Discharged)),
                Field("Admission method", Method),
                Field("Discharge destination", Destination),
            ],
            marginBottom: 6f));
        doc.Add(WeightedRow(
        [
            Field("Registered practice", GpPractice),
            Field("Responsible consultant", Consultant),
            Field("Summary prepared by", PreparedBy),
            Field("Patient address", Address),
        ]));

        // ── Diagnoses ─────────────────────────────────────────────────────────

        doc.Add(H2("Diagnoses"));
        doc.Add(Panel("Primary diagnosis this admission", [Cell(PrimaryDiagnosis, size: 11f, bold: true)]));
        doc.Add(Cell("Other active problems", size: 9.5f, bold: true, marginBottom: 4f));
        foreach (string diagnosis in SecondaryDiagnoses)
        {
            doc.Add(Cell($"· {diagnosis}", size: 9.5f, marginBottom: 2f));
        }

        // ── Narrative ─────────────────────────────────────────────────────────

        doc.Add(H2("Summary of admission"));
        doc.Add(Cell(
            "The patient presented to the emergency department with two hours of central chest discomfort "
                + "radiating to the left arm, with associated sweating. Troponin was raised on serial sampling and "
                + "the ECG showed lateral T-wave inversion, in keeping with a non-ST-elevation myocardial "
                + "infarction.",
            size: 9.5f,
            marginBottom: 6f));
        doc.Add(Cell(
            "Dual antiplatelet therapy and treatment-dose anticoagulation were started on admission. Coronary "
                + "angiography on day two demonstrated significant disease in the circumflex territory, treated "
                + "with a single drug-eluting stent. Recovery was uncomplicated. The patient mobilised "
                + "independently, remained in sinus rhythm on telemetry, and was reviewed by the cardiac "
                + "rehabilitation team before discharge.",
            size: 9.5f,
            marginBottom: 6f));
        doc.Add(Cell(
            "Secondary prevention was started before discharge and is set out below. The patient and their family "
                + "have been counselled on the diagnosis, on the importance of not missing antiplatelet doses, and "
                + "on when to seek urgent help.",
            size: 9.5f,
            marginBottom: 4f));

        // ── Investigations ────────────────────────────────────────────────────

        doc.Add(H2("Key investigations"));

        Table investigations = new(2.4f, 3.4f, 1.7f, 1.1f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        investigations.AddHeader(Th("Investigation"), Th("Result"), Th("Reference range"), Th("Flag"));
        foreach (Investigation row in Investigations)
        {
            bool normal = string.Equals(row.Flag, "Normal", StringComparison.Ordinal);
            investigations.AddRow(
                Td(row.Test),
                Td(row.Result),
                Td(row.Reference, color: Muted),
                Td(row.Flag, bold: !normal, color: normal ? Positive : Negative));
        }

        doc.Add(investigations);

        // ── Medication ────────────────────────────────────────────────────────

        doc.Add(H2("Medication on discharge"));
        doc.Add(Cell(
            "This list replaces the pre-admission list in full. Every change from the pre-admission regimen is "
                + "marked, with the reason and the follow-up it requires.",
            size: 9f,
            color: Muted,
            marginBottom: 8f));

        Table meds = new(1.7f, 0.9f, 0.8f, 1.6f, 1.2f, 3.5f)
        {
            CellPadding = 6,
            Border = new Border(0.5f),
            BorderColors = new BorderColors(Rule),
            HeaderBackgroundColor = Accent,
            HeaderTextColor = Color.White,
        };
        meds.AddHeader(Th("Medicine"), Th("Dose"), Th("Route"), Th("Frequency"), Th("Change"), Th("Note"));
        foreach (Medication med in Medications)
        {
            meds.AddRow(
                Td(med.Drug, bold: true),
                Td(med.Dose),
                Td(med.Route, color: Muted),
                Td(med.Frequency),
                Td(
                    med.Change,
                    bold: !string.Equals(med.Change, "Unchanged", StringComparison.Ordinal),
                    color: ChangeColour(med.Change)),
                Td(med.Note, color: Muted));
        }

        doc.Add(meds);

        doc.Add(Panel(
            "Allergies and cautions",
            [
                Cell("No known drug allergies recorded.", size: 9f, marginBottom: 3f),
                Cell(
                    "Avoid non-steroidal anti-inflammatory drugs while on dual antiplatelet therapy. The patient "
                        + "has been told this and the ibuprofen previously taken as required has been stopped.",
                    size: 9f),
            ],
            Negative));

        // ── Actions for the GP ────────────────────────────────────────────────

        doc.Add(H2("Actions for the general practitioner"));
        for (int index = 0; index < GpActions.Length; index++)
        {
            doc.Add(Cell($"{index + 1}. {GpActions[index]}", size: 9.5f, marginBottom: 4f));
        }

        doc.Add(H2("Follow-up arranged"));
        doc.Add(Cell(
            "Cardiology outpatient review in six weeks — appointment to be sent to the patient by post. Cardiac "
                + "rehabilitation will contact the patient directly within two weeks. No further hospital "
                + "investigations are outstanding.",
            size: 9.5f,
            marginBottom: 6f));

        doc.Add(Panel("Advice given to the patient",
        [
            Cell(
                "Seek urgent medical help by calling the emergency services if chest pain returns at rest, lasts "
                    + "more than fifteen minutes, or is accompanied by breathlessness, sweating, or faintness. Do "
                    + "not stop the antiplatelet medicines without discussing it with a clinician, including before "
                    + "any dental or surgical procedure.",
                size: 9f),
        ]));

        doc.Add(Cell(
            $"Prepared by {PreparedBy} on {GbDate(Discharged)} on behalf of {Consultant}. "
                + $"Queries about this summary: quote {Reference}.",
            size: 8.5f,
            color: Muted,
            marginTop: 10f));

        return doc;
    }
}
