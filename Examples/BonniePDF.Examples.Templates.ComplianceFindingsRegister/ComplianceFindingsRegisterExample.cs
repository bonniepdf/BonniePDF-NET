using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.ComplianceFindingsRegister;

/// <summary>
/// A compliance / audit findings register - the kind of report a GRC, audit, or
/// security-assessment platform produces. Each finding is a severity-coloured
/// Container card built from a HorizontalStack (severity badge +
/// heading/description), and the whole register is an accessible, archival
/// PDF/A-2a + PDF/UA-1 document.
/// </summary>
[System.ComponentModel.DisplayName("Compliance Findings Register")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("A GRC / audit findings register - severity-coloured finding cards paginating across pages, PDF/A-2a + PDF/UA-1.")]
public static class ComplianceFindingsRegisterExample
{
    private enum Severity { Critical, High, Medium, Low, Resolved }

    private sealed record SeverityStyle(string Label, Color Badge, Color Tint, Color Border);

    private static readonly Dictionary<Severity, SeverityStyle> SeverityStyles = new()
    {
        [Severity.Critical] = new("CRITICAL", ColorPalette.Red[600], ColorPalette.Red[50], ColorPalette.Red[200]),
        [Severity.High]     = new("HIGH",     ColorPalette.Orange[600], ColorPalette.Orange[50], ColorPalette.Orange[200]),
        [Severity.Medium]   = new("MEDIUM",   ColorPalette.Amber[600], ColorPalette.Amber[50], ColorPalette.Amber[200]),
        [Severity.Low]      = new("LOW",      ColorPalette.Slate[500], ColorPalette.Slate[50], ColorPalette.Slate[200]),
        [Severity.Resolved] = new("RESOLVED", ColorPalette.Green[600], ColorPalette.Green[50], ColorPalette.Green[200]),
    };

    private sealed record Finding(string Id, string Category, Severity Severity, string Title, string Description);

    private static readonly Finding[] Findings =
    [
        new("F-001", "Access Control", Severity.Critical, "Shared administrator credentials",
            "A single administrator account is shared across three regional teams with no individual accountability. Recommend provisioning named accounts and enforcing MFA before the next audit cycle."),

        new("F-002", "Data Encryption", Severity.High, "Unencrypted database backups",
            "Nightly database backups are written to object storage without server-side encryption. Recommend enabling encryption at rest and rotating the affected snapshots."),

        new("F-003", "Password Policy", Severity.Medium, "Password rotation exceeds policy window",
            "Twelve service accounts have not rotated credentials within the 90-day policy window. Recommend automating rotation via the secrets manager already in use elsewhere."),

        new("F-004", "Documentation", Severity.Low, "Runbook references a retired endpoint",
            "The incident-response runbook still references a monitoring endpoint decommissioned in the last infrastructure migration. Recommend a documentation pass at the next sprint review."),

        new("F-005", "Incident Response", Severity.Resolved, "On-call escalation gap closed",
            "The previously reported gap between the paging tool and the secondary on-call rota has been closed. Verified against two live incidents since remediation."),

        new("F-006", "Vendor Management", Severity.Critical, "Sub-processor missing a signed DPA",
            "A newly onboarded analytics sub-processor is receiving customer data without an executed data-processing agreement. Recommend suspending the data feed until the DPA is signed."),

        new("F-007", "Network Segmentation", Severity.High, "Flat network between staging and production",
            "Staging hosts can reach production database ports directly. Recommend segmenting the two environments with dedicated security groups and removing the shared routing table."),

        new("F-008", "Change Management", Severity.Medium, "Emergency changes lack retrospective sign-off",
            "Four of the last twenty emergency changes were merged without a documented post-hoc review. Recommend adding a mandatory 48-hour retrospective step to the change template."),

        new("F-009", "Physical Security", Severity.Low, "Visitor log retained past policy",
            "The reception visitor log for the secondary office is retained for eighteen months against a twelve-month policy. Recommend aligning the retention job with the documented schedule."),

        new("F-010", "Employee Training", Severity.Resolved, "Security-awareness completion below target",
            "Completion of the annual security-awareness course has risen from 74% to 98% following the reminder campaign. No further action required this cycle."),

        new("F-011", "Data Retention", Severity.Critical, "Deletion requests not propagating to backups",
            "Right-to-erasure requests are honoured in the primary database but not in long-term backup archives. Recommend extending the deletion workflow to cover archived snapshots."),

        new("F-012", "Access Reviews", Severity.High, "Quarterly access review overdue",
            "The Q2 access review for the billing system was not completed; twelve accounts with elevated privileges have not been re-certified. Recommend closing the backlog before Q3 begins."),

        new("F-013", "Business Continuity", Severity.Medium, "Failover runbook untested this year",
            "The documented regional failover procedure has not been exercised since last year’s audit. Recommend scheduling a tabletop or live failover drill this quarter."),

        new("F-014", "Asset Inventory", Severity.Low, "Decommissioned laptops pending wipe confirmation",
            "Nine decommissioned laptops are awaiting certificate-of-destruction confirmation from the disposal vendor. Recommend following up before the next inventory reconciliation."),

        new("F-015", "Authentication", Severity.Resolved, "Multi-factor authentication now enforced org-wide",
            "MFA is now enforced for all workforce accounts, including the previously exempted contractor group. Verified via the identity provider’s enforcement report."),

        new("F-016", "Third-Party Risk", Severity.High, "Vendor risk assessments twelve months stale",
            "Risk assessments for three critical vendors have not been refreshed in over a year. Recommend prioritising re-assessment for vendors with direct production access."),

        new("F-017", "Audit Logging", Severity.Medium, "Administrative actions not centrally logged",
            "Administrative actions taken directly in the cloud console are not forwarded to the central log store. Recommend enabling the provider’s management-event log export."),

        new("F-018", "Endpoint Protection", Severity.Critical, "Endpoint agent disabled on finance workstations",
            "The endpoint-protection agent was found disabled on two finance-team workstations for eleven days. Recommend re-enabling immediately and reviewing the tamper-protection policy."),
    ];

    private static Container FindingCard(Finding finding)
    {
        SeverityStyle style = SeverityStyles[finding.Severity];

        Container badge = new()
        {
            Width           = 78,
            Padding         = new Thickness(6, 4, 6, 4),
            BackgroundColor = style.Badge,
            CornerRadius    = 4,
            Child = new TextBlock
            {
                Text      = style.Label,
                FontName  = StandardFonts.HelveticaBold,
                FontSize  = 9,
                TextColor = Color.White,
                Alignment = TextAlignment.Center,
            },
        };

        VerticalStack body = new() { Spacing = 3 };

        body.AddChild(new Heading($"{finding.Id} - {finding.Title}")
        {
            TocIdentifier = finding.Id.ToLowerInvariant(),
            Level         = 2,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 12,
            TextColor     = ColorPalette.Slate[800],
        });

        body.AddChild(new TextBlock
        {
            Text      = finding.Category,
            FontName  = StandardFonts.HelveticaBold,
            FontSize  = 9,
            TextColor = ColorPalette.Slate[500],
        });

        body.AddChild(new TextBlock
        {
            Text      = finding.Description,
            FontSize  = 10.5f,
            TextColor = ColorPalette.Slate[700],
        });

        HorizontalStack row = new() { Spacing = 12, WidthDistribution = WidthDistribution.Weighted };
        row.AddChild(badge);
        row.AddChild(body, 1);

        return new Container
        {
            Child           = row,
            Padding         = new Thickness(12),
            BackgroundColor = style.Tint,
            CornerRadius    = 8,
            Border          = new Border(1),
            BorderColors    = new BorderColors(style.Border),
            Margin          = new Thickness(0, 0, 0, 10),
        };
    }

    public static Document Generate()
    {
        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Compliance Findings Register",
                Author   = "BonniePDF",
                Subject  = "An accessible PDF/A-2a + PDF/UA-1 compliance findings register.",
                Keywords = "pdf, accessibility, compliance, audit, findings register, GRC, BonniePDF",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle()
                    .WithFamily(StandardFonts.Helvetica)
                    .WithSize(11)
                    .WithColor(ColorPalette.Slate[700]),

                FooterTemplate = pageNum => new TextBlock
                {
                    Text      = $"Page {pageNum} of {TextElementBase.TotalPagesToken}",
                    FontName  = StandardFonts.Helvetica,
                    FontSize  = 9,
                    TextColor = ColorPalette.Slate[400],
                    Alignment = TextAlignment.Right,
                },
            },
        };

        doc.Add(new Heading("Compliance Findings Register")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 22,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 10),
        });

        doc.Add(new TextBlock
        {
            Text   = "Findings from the quarterly security and compliance review, ordered by identifier and colour-coded by severity. Each entry records the affected control category, a summary, and the recommended remediation.",
            Margin = new Thickness(0, 0, 0, 16),
        });

        VerticalStack register = new();
        foreach (Finding finding in Findings)
        {
            register.AddChild(FindingCard(finding));
        }
        doc.Add(register);

        return doc;
    }
}
