using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Templates.BusinessInvoice;

/// <summary>
/// Generates a modern QuickBooks-style invoice that fits on a single page
/// </summary>
[System.ComponentModel.DisplayName("Business Invoice")]
[System.ComponentModel.Category("Templates")]
[System.ComponentModel.Description("Generates a modern QuickBooks-style invoice with company branding, line items table, payment details, and professional styling.")]
public static class BusinessInvoiceExample
{
    // Brand accent color - a professional green similar to QuickBooks
    private static readonly Color AccentGreen = Color.FromHex("#0B9444");
    private static readonly Color AccentGreenLight = Color.FromHex("#E8F5EC");
    private static readonly Color HeaderGray = Color.FromHex("#F5F5F5");
    private static readonly Color BorderGray = Color.FromHex("#D9D9D9");
    private static readonly Color TextDark = Color.FromHex("#333333");
    private static readonly Color TextMedium = Color.FromHex("#555555");
    private static readonly Color TextLight = Color.FromHex("#888888");

    public static Document Generate()
    {
        Document doc = new(new DocumentSettings
        {
            PageSize = PdfSize.A4,
            Margin = 36
        });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9).WithColor(TextMedium);
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-US";

        doc.Metadata.Title = $"Invoice INV-{DateTime.Now.Year}-001";
        doc.Metadata.Author = "Studio Eleven";
        doc.Metadata.Subject = "Invoice for design and development services.";

        VerticalStack mainStack = new();

        // ============================================
        // GREEN ACCENT BAR - QuickBooks-style top strip
        // ============================================
        mainStack.AddChild(new Container
        {
            BackgroundColor = AccentGreen,
            Padding = new Thickness(0, 4, 0, 4),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        // ============================================
        // HEADER - Company info left, "Invoice" title right
        // ============================================
        Grid headerTable = new(1.2f, 1f)
        {
            CellPadding = 0,
            Border = Border.None,
            Margin = new Thickness(0, 16, 0, 16)
        };

        // Left: Company branding
        VerticalStack companyStack = new();
        companyStack.AddChild(new TextBlock
        {
            Text = "STUDIO ELEVEN",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 20,
            TextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        companyStack.AddChild(new TextBlock { Text = "42 Design District" });
        companyStack.AddChild(new TextBlock { Text = "Brooklyn, NY 11201" });
        companyStack.AddChild(new TextBlock { Text = "Phone  (555) 123-4567" });
        companyStack.AddChild(new TextBlock { Text = "Email  hello@studioeleven.co" });

        // Right: Invoice title - Heading (H1) instead of plain TextBlock so it
        // carries the semantic role for screen readers (PDF/UA-1 Matterhorn §14-1).
        // Visually identical to the prior styled TextBlock.
        VerticalStack titleStack = new();
        titleStack.AddChild(new Heading("Invoice")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 28,
            TextColor = AccentGreen,
            Alignment = TextAlignment.Right
        });

        headerTable.AddRow(
            companyStack,
            titleStack
        );
        mainStack.AddChild(headerTable);

        // ============================================
        // BILL TO / SHIP TO / DETAILS - 3-column bordered section
        // ============================================
        Grid addressTable = new(1f, 1f, 1.1f)
        {
            CellPadding = 10,
            Border = new Border(BorderStyle.Thin),
            BorderColors = new BorderColors(BorderGray),
            Margin = new Thickness(0, 0, 0, 16)
        };

        // Bill To column
        VerticalStack billToStack = new();
        billToStack.AddChild(new TextBlock
        {
            Text = "Bill to",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            TextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        billToStack.AddChild(new TextBlock { Text = "Meridian Labs" });
        billToStack.AddChild(new TextBlock { Text = "Sarah Chen" });
        billToStack.AddChild(new TextBlock { Text = "88 Innovation Way" });
        billToStack.AddChild(new TextBlock { Text = "Austin, TX 78701" });

        // Ship To column
        VerticalStack shipToStack = new();
        shipToStack.AddChild(new TextBlock
        {
            Text = "Ship to",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            TextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        shipToStack.AddChild(new TextBlock { Text = "Meridian Labs" });
        shipToStack.AddChild(new TextBlock { Text = "Sarah Chen" });
        shipToStack.AddChild(new TextBlock { Text = "88 Innovation Way" });
        shipToStack.AddChild(new TextBlock { Text = "Austin, TX 78701" });

        // Details column
        VerticalStack detailsStack = new();
        detailsStack.AddChild(new TextBlock
        {
            Text = "Details",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            TextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        AddDetailRow(detailsStack, "Invoice #", $"{DateTime.Now.Year}-001");
        AddDetailRow(detailsStack, "Invoice date", DateTime.Now.ToString("MM/dd/yyyy"));
        AddDetailRow(detailsStack, "Terms", "Net 14");
        AddDetailRow(detailsStack, "Due date", DateTime.Now.AddDays(14).ToString("MM/dd/yyyy"));

        addressTable.AddRow(
            billToStack,
            shipToStack,
            detailsStack
        );
        mainStack.AddChild(addressTable);

        // ============================================
        // LINE ITEMS TABLE - Clean professional table
        // ============================================
        Table lineItemsTable = new(2.2f, 2.5f, 0.8f, 0.8f, 1f)
        {
            CellPadding = 8,
            Border = Border.None,
            HeaderBackgroundColor = HeaderGray,
            HeaderTextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 0)
        };

        // Header row
        lineItemsTable.AddHeader(
            new TextBlock
            {
                Text = "Product / Service", FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignLeft(),
            new TextBlock
            {
                Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignLeft(),
            new TextBlock
            {
                Text = "Qty / Hrs", FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignCenter(),
            new TextBlock
            {
                Text = "Rate", FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignRight(),
            new TextBlock
            {
                Text = "Amount", FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignRight()
        );

        // Line items data - generated via Placeholders for prototyping
        var invoiceSource = Placeholders.WithSeed(42);
        var lineItems = invoiceSource.InvoiceLines(5);
        var items = lineItems.Select(li => new { Service = li.Service, Desc = li.Description, Qty = li.Quantity, Price = li.UnitPrice }).ToArray();

        decimal subtotal = 0;
        foreach (var item in items)
        {
            decimal lineTotal = item.Qty * item.Price;
            subtotal += lineTotal;

            lineItemsTable.AddRow(
                new TextBlock
                {
                    Text = item.Service, FontName = StandardFonts.Helvetica, FontSize = 9,
                    TextColor = TextMedium
                }.AlignLeft(),
                new TextBlock
                {
                    Text = item.Desc, FontName = StandardFonts.Helvetica, FontSize = 9,
                    TextColor = TextLight
                }.AlignLeft(),
                new TextBlock
                {
                    Text = item.Qty.ToString(), FontName = StandardFonts.Helvetica, FontSize = 9,
                    TextColor = TextMedium
                }.AlignCenter(),
                new TextBlock
                {
                    Text = $"${item.Price:N2}", FontName = StandardFonts.Helvetica, FontSize = 9,
                    TextColor = TextMedium
                }.AlignRight(),
                new TextBlock
                {
                    Text = $"${lineTotal:N2}", FontName = StandardFonts.Helvetica, FontSize = 9,
                    TextColor = TextDark
                }.AlignRight()
            );
        }

        mainStack.AddChild(lineItemsTable);

        // Divider line under line items
        mainStack.AddChild(new Container
        {
            BackgroundColor = AccentGreen,
            Padding = new Thickness(0, 0.75f, 0, 0),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        // ============================================
        // FOOTER SECTION - Message left, Totals right
        // ============================================
        decimal tax = subtotal * 0.0825m; // 8.25% sales tax
        decimal total = subtotal + tax;

        Grid footerTable = new(1.3f, 1f)
        {
            CellPadding = 0,
            Border = Border.None,
            Margin = new Thickness(0, 0, 0, 0)
        };

        // Left: Customer message
        VerticalStack messageStack = new();
        messageStack.AddChild(new TextBlock
        {
            Text = "Customer message",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 9,
            TextColor = TextDark,
            Margin = new Thickness(0, 0, 0, 4)
        });
        messageStack.AddChild(new TextBlock { Text = "Hello!" });
        messageStack.AddChild(new TextBlock { Text = "Thank you for choosing Studio Eleven." });
        messageStack.AddChild(new TextBlock { Text = "Please return this invoice with payment." });
        messageStack.AddChild(new TextBlock { Text = "Thanks!", Margin = new Thickness(0, 4, 0, 0) });

        // Right: Totals
        VerticalStack totalsStack = new();

        // Subtotal
        Grid subtotalRow = new(1f, 1f)
        {
            CellPadding = 6,
            Border = Border.None
        };
        subtotalRow.AddRow(
            new TextBlock
            {
                Text = "Subtotal", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = TextMedium
            }.AlignLeft(),
            new TextBlock
            {
                Text = $"${subtotal:N2}", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = TextDark
            }.AlignRight()
        );
        totalsStack.AddChild(subtotalRow);

        // Sales Tax
        Grid taxRow = new(1f, 1f)
        {
            CellPadding = 6,
            Border = Border.None
        };
        taxRow.AddRow(
            new TextBlock
            {
                Text = "Sales tax (8.25%)", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = TextMedium
            }.AlignLeft(),
            new TextBlock
            {
                Text = $"${tax:N2}", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = TextDark
            }.AlignRight()
        );
        totalsStack.AddChild(taxRow);

        // Early payment discount
        Grid discountRow = new(1f, 1f)
        {
            CellPadding = 6,
            Border = Border.None
        };
        discountRow.AddRow(
            new TextBlock
            {
                Text = "Discount (early payment)", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = AccentGreen
            }.AlignLeft(),
            new TextBlock
            {
                Text = "-$1,400.00", FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = AccentGreen
            }.AlignRight()
        );
        totalsStack.AddChild(discountRow);

        // Divider before total
        totalsStack.AddChild(new Container
        {
            BackgroundColor = TextDark,
            Padding = new Thickness(0, 0.5f, 0, 0),
            Margin = new Thickness(0, 4, 0, 4),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        // Total
        decimal totalDue = total - 1400m;
        Grid totalRow = new(1f, 1f)
        {
            CellPadding = 6,
            Border = Border.None
        };
        totalRow.AddRow(
            new TextBlock
            {
                Text = "Total", FontName = StandardFonts.HelveticaBold, FontSize = 14,
                TextColor = TextDark
            }.AlignLeft(),
            new TextBlock
            {
                Text = $"${totalDue:N2}", FontName = StandardFonts.HelveticaBold, FontSize = 14,
                TextColor = TextDark
            }.AlignRight()
        );
        totalsStack.AddChild(totalRow);

        footerTable.AddRow(
            new Container { Child = messageStack, Padding = new Thickness(0, 0, 40, 0) },
            totalsStack
        );
        mainStack.AddChild(footerTable);

        // ============================================
        // PAYMENT DETAILS - Compact bank info section
        // ============================================
        Container paymentSection = new()
        {
            BackgroundColor = AccentGreenLight,
            Padding = new Thickness(16, 12, 16, 12),
            Margin = new Thickness(0, 20, 0, 0),
            Border = Border.LeftOnly(BorderStyle.Thick),
            BorderColors = BorderColors.LeftOnly(AccentGreen),
            Child = CreatePaymentDetails()
        };
        mainStack.AddChild(paymentSection);

        // ============================================
        // FOOTER NOTE
        // ============================================
        mainStack.AddChild(new Container
        {
            BackgroundColor = AccentGreen,
            Padding = new Thickness(0, 0.5f, 0, 0),
            Margin = new Thickness(0, 20, 0, 0),
            Child = new TextBlock { Text = " ", FontSize = 1 }
        });

        mainStack.AddChild(new TextBlock
        {
            Text = "Thank you for your business  |  hello@studioeleven.co  |  www.studioeleven.co",
            FontName = StandardFonts.Helvetica,
            FontSize = 8,
            TextColor = TextLight,
            Alignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });

        // ============================================
        // BUILD DOCUMENT
        // ============================================
        doc.Add(mainStack);
        return doc;
    }

    private static void AddDetailRow(VerticalStack stack, string label, string value)
    {
        Grid row = new(0.9f, 1f)
        {
            CellPadding = 0,
            Border = Border.None
        };
        row.AddRow(
            new TextBlock
            {
                Text = label, FontName = StandardFonts.HelveticaBold, FontSize = 9,
                TextColor = TextDark
            }.AlignLeft(),
            new TextBlock
            {
                Text = value, FontName = StandardFonts.Helvetica, FontSize = 9,
                TextColor = TextMedium
            }.AlignLeft()
        );
        stack.AddChild(row);
    }

    private static Grid CreatePaymentDetails()
    {
        Grid paymentTable = new(1f, 1f, 1f)
        {
            CellPadding = 0,
            Border = Border.None
        };

        // Bank details
        VerticalStack bankStack = new();
        bankStack.AddChild(new TextBlock
        {
            Text = "Bank Transfer",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            TextColor = AccentGreen,
            Margin = new Thickness(0, 0, 0, 4)
        });
        bankStack.AddChild(new TextBlock { Text = "Chase Bank", FontSize = 8, TextColor = TextMedium });
        bankStack.AddChild(new TextBlock { Text = "Routing: 021000021", FontSize = 8, TextColor = TextMedium });
        bankStack.AddChild(new TextBlock { Text = "Account: ****7842", FontSize = 8, TextColor = TextMedium });

        // Other payment
        VerticalStack otherStack = new();
        otherStack.AddChild(new TextBlock
        {
            Text = "Other Methods",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            TextColor = AccentGreen,
            Margin = new Thickness(0, 0, 0, 4)
        });
        otherStack.AddChild(new TextBlock { Text = "PayPal: pay@studioeleven.co", FontSize = 8, TextColor = TextMedium });
        otherStack.AddChild(new TextBlock { Text = "Wise: @studioeleven", FontSize = 8, TextColor = TextMedium });

        // Reference
        VerticalStack refStack = new();
        refStack.AddChild(new TextBlock
        {
            Text = "Reference",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 8,
            TextColor = AccentGreen,
            Margin = new Thickness(0, 0, 0, 4)
        });
        refStack.AddChild(new TextBlock { Text = "Please include invoice", FontSize = 8, TextColor = TextMedium });
        refStack.AddChild(new TextBlock { Text = "number with payment", FontSize = 8, TextColor = TextMedium });

        paymentTable.AddRow(
            bankStack,
            otherStack,
            refStack
        );

        return paymentTable;
    }
}

