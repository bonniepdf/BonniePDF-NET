# Tables

BonniePDF provides a fluent table builder with proportional columns, repeating headers, alternating row backgrounds, and automatic multi-page flow.

## Basic Table

```csharp
column.Item().Table(table =>
{
    table.Columns(1f, 2f, 1f); // 3 columns with 1:2:1 ratio
    table.CellPadding(8);

    // Header row
    table.Header(row =>
    {
        row.Cell().Text("Name").Bold();
        row.Cell().Text("Description").Bold();
        row.Cell().Text("Price").Bold();
    });

    // Data rows
    table.Cell().Text("Widget A");
    table.Cell().Text("A basic widget for everyday use");
    table.Cell().Text("$9.99");

    table.Cell().Text("Widget B");
    table.Cell().Text("Premium widget with extra features");
    table.Cell().Text("$19.99");
});
```

## Column Definition

Columns are defined with proportional weights. The weights determine relative widths:

```csharp
// Equal-width columns
table.Columns(1f, 1f, 1f, 1f);

// ID (narrow), Name (wide), Status (narrow)
table.Columns(0.5f, 2f, 1f);

// 5-column layout
table.Columns(0.5f, 1.5f, 1f, 1f, 1f);
```

## Cell Padding

Set uniform padding for all cells:

```csharp
table.CellPadding(8); // 8pt padding inside every cell
```

## Headers

### Basic Header

```csharp
table.Header(row =>
{
    row.Cell().Text("Column A").Bold();
    row.Cell().Text("Column B").Bold();
    row.Cell().Text("Column C").Bold();
});
```

### Styled Header

```csharp
table.HeaderBackground(Color.Blue600);
table.HeaderTextColor(Color.White);

table.Header(row =>
{
    row.Cell().Text("ID").Bold().FontSize(10);
    row.Cell().Text("Name").Bold().FontSize(10);
    row.Cell().Text("Department").Bold().FontSize(10);
});
```

### Repeating Headers

For tables that span multiple pages, enable repeating headers so column names appear at the top of every page:

```csharp
table.RepeatHeaders(); // Headers repeat on each page
```

## Footers

Add a footer row — useful for totals:

```csharp
table.Footer(row =>
{
    row.Cell().Text("Total").Bold();
    row.Cell().Text("");
    row.Cell().Text("$29.98").Bold();
});
```

## Data Rows

### Simple Rows

Add cells sequentially — they fill left-to-right, wrapping to the next row automatically:

```csharp
// Each group of 3 cells forms one row (matching 3 columns)
table.Cell().Text("Row 1, Col 1");
table.Cell().Text("Row 1, Col 2");
table.Cell().Text("Row 1, Col 3");

table.Cell().Text("Row 2, Col 1");
table.Cell().Text("Row 2, Col 2");
table.Cell().Text("Row 2, Col 3");
```

### Rows with Background Color

Use `table.Row()` to set a background color for the entire row:

```csharp
table.Row(Color.Slate50, row =>
{
    row.Cell().Text("Shaded row");
    row.Cell().Text("With background");
    row.Cell().Text("Color");
});
```

### Alternating Row Colors

```csharp
for (int i = 0; i < data.Length; i++)
{
    Color bgColor = i % 2 == 0 ? Color.White : Color.Slate50;

    table.Row(bgColor, row =>
    {
        row.Cell().Text(data[i].Name);
        row.Cell().Text(data[i].Department);
        row.Cell().Text(data[i].Salary.ToString("C"));
    });
}
```

## Borders

Set uniform borders on all cells:

```csharp
table.Border(1, Color.Slate300); // 1pt border, slate color
table.Border(0.5f);              // 0.5pt border, default black
```

## Complete Example: Employee Directory

```csharp
column.Item().Table(table =>
{
    table.Columns(0.5f, 1.5f, 1f, 1f, 1f);
    table.CellPadding(8);
    table.Border(1, Color.Slate300);
    table.HeaderBackground(Color.Blue600);
    table.HeaderTextColor(Color.White);
    table.RepeatHeaders();

    table.Header(row =>
    {
        row.Cell().Text("ID").Bold().FontSize(10);
        row.Cell().Text("Full Name").Bold().FontSize(10);
        row.Cell().Text("Department").Bold().FontSize(10);
        row.Cell().Text("Position").Bold().FontSize(10);
        row.Cell().Text("Start Date").Bold().FontSize(10);
    });

    for (int i = 0; i < employees.Count; i++)
    {
        var emp = employees[i];
        Color rowBg = i % 2 == 0 ? Color.White : Color.Slate50;

        table.Row(rowBg, row =>
        {
            row.Cell().Text(emp.Id.ToString("D4")).FontSize(9).TextColor(Color.Slate600);
            row.Cell().Text(emp.FullName).FontSize(9).TextColor(Color.Slate800);
            row.Cell().Text(emp.Department).FontSize(9).TextColor(Color.Slate600);
            row.Cell().Text(emp.Position).FontSize(9).TextColor(Color.Slate600);
            row.Cell().Text(emp.StartDate.ToString("MM/yyyy")).FontSize(9).TextColor(Color.Slate500);
        });
    }
});
```

## TableDescriptor Reference

| Method | Description |
|---|---|
| `table.Columns(params float[])` | Define column weights (proportional widths) |
| `table.CellPadding(float)` | Set default cell padding in points |
| `table.RepeatHeaders(bool)` | Enable/disable repeating headers on page breaks |
| `table.HeaderBackground(Color)` | Set header row background color |
| `table.HeaderTextColor(Color)` | Set header row text color |
| `table.Border(float, Color?)` | Set uniform cell borders |
| `table.Header(Action<TableRowDescriptor>)` | Define header row |
| `table.Footer(Action<TableRowDescriptor>)` | Define footer row |
| `table.Row(Action<TableRowDescriptor>)` | Add data row |
| `table.Row(Color, Action<TableRowDescriptor>)` | Add data row with background |
| `table.Cell()` | Add next cell (fills left-to-right) |
