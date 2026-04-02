# BonniePDF

BonniePDF is a high-performance PDF generation library for .NET 8+. It is designed for ease of use and performance with no friction or fuss around deployments.

## Features

- Zero dependancies.
- Fluent API syntax for declarative document construction
- Built-in charts (bar, line, area, pie, lollipop, dot plot, grouped bar)
- PDF/A-1b (support for archival compliance)
- AES-256 encryption with configurable permissions
- TrueType font embedding & subsetting
- Automatic pagination, page numbers, and overflow handling for tables and text
- SVG export
- Full async support

## Overview

BonniePDF enables developers to create complex PDF documents in managed code. With an intuitive syntax, it efficiently manages text, images, tables, charts, and standard document structures, with no need for native libraries or third-party tools.

## Installation

```
dotnet add package BonniePDF
```

## Compatability

Requires .NET 8.0 or later. Works on Windows, Linux, and macOS.

## Basic usage

```csharp
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;
using BonniePDF.Core;

Pdf.Create(doc =>
{
    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            column.Item()
                .Text("Hello, World!")
                .Bold()
                .FontSize(24)
                .TextColor(Color.Slate800);
        });
    });
}).Save("hello.pdf");
```

## Documentation

- [Getting Started](docs/getting-started.md)
- [Page Layout](docs/layout.md)
- [Text and Typography](docs/typography.md)
- [Tables](docs/tables.md)
- [Charts](docs/charts.md)
- [Images and Shapes](docs/media.md)
- [Headers and Footers](docs/templates.md)
- [Encryption and PDF/A](docs/advanced.md)
- [API Reference](docs/api.md)

## Examples

The `Examples/` directory contains lot of working samples that cover common use cases, including text, books, invoices, reports, tables, charts.

## Licence

BonniePDF Community License 1.0 or Commercial - please see https://bonniepdf.dev/pricing for details
