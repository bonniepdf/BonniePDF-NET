# BonniePDF Validation Tool

A console application that validates generated PDFs against the PDF/A standard using a local VeraPDF API instance.

## Features

- **Batch Validation**: Validate all PDFs in a directory at once
- **VeraPDF Integration**: Uses your local VeraPDF API running at `http://localhost:8080/`
- **Detailed Error Reporting**: Shows specific PDF/A compliance failures with:
  - ISO specification clause and test numbers
  - Clear descriptions of what failed
  - Exact locations in the PDF structure
- **JSON Report Generation**: Creates a detailed `validation-report.json` file
- **Easy to Use**: Simple command-line interface with automatic directory detection

## Prerequisites

1. **VeraPDF API Service**: You must have the VeraPDF validation service running locally on port 8080.

   To start the VeraPDF service, you can use Docker:
   ```bash
   docker run -d -p 8080:8080 -p 8081:8081 verapdf/rest:latest
   ```

   Or run the VeraPDF REST service directly if you have it installed.

2. **API Endpoints**: The tool uses the VeraPDF REST API endpoints:
   - `/api/info` - Health check endpoint
   - `/api/validate/{profileId}` - Validation endpoint (default: `1b` for PDF/A-1B)
   
   Available validation profiles:
   - `1a` - PDF/A-1A (full compliance, accessibility)
   - `1b` - PDF/A-1B (basic compliance)
   - `2b` - PDF/A-2B
   - `3b` - PDF/A-3B

2. **Generated PDFs**: Run the BonniePDF Examples project first to generate sample PDFs.

## Usage

### Basic Usage

The tool will automatically look for PDFs in the default examples output directory:

```bash
dotnet run --project BonniePDF.Validation
```

### Specify Custom Directory

Provide a custom directory path as an argument:

```bash
dotnet run --project BonniePDF.Validation -- "C:\path\to\your\pdfs"
```

### Build and Run

```bash
# Build the project
dotnet build BonniePDF.Validation/BonniePDF.Validation.csproj

# Run the validator
dotnet run --project BonniePDF.Validation
```

## Output

The tool provides:

1. **Console Output**: Real-time validation progress with:
   - ✓ PASSED for compliant PDFs
   - ✗ FAILED for non-compliant PDFs (with error details)
   - ⚠ ERROR for validation failures

2. **JSON Report**: A detailed `validation-report.json` file in the PDF directory containing:
   - Total PDFs validated
   - Pass/Fail/Error counts
   - Individual results for each PDF
   - Full error details

## Example Output

```
╔══════════════════════════════════════════════════════════════╗
║           BonniePDF - PDF/A Validation Tool                  ║
╚══════════════════════════════════════════════════════════════╝

Validating PDFs in: C:\path\to\GeneratedPdfs
VeraPDF API: http://localhost:8080/api
Validation Profile: PDF/A-1B

✓ VeraPDF API is accessible

Found 26 PDF file(s) to validate

[1/26] Validating: all_page_sizes.pdf
   ✗ FAILED - Not compliant
   Errors:
     - [ISO 19005-1:2005 6.7.2.1] The document catalog dictionary of a conforming file shall contain the Metadata key. The metadata stream dictionary shall contain entry Type with value /Metadata and entry Subtype with value /XML
       └─ Location: root/document[0]
     - [ISO 19005-1:2005 6.2.3.3.1] DeviceRGB may be used only if the file has a PDF/A-1 OutputIntent that uses an RGB colour space
       └─ Location: root/document[0]/pages[0](4 0 obj PDPage)/contentStream[0](5 0 obj PDContentStream)/operators[3]/colorSpace[0]
     - [ISO 19005-1:2005 6.3.4.1] The font programs for all fonts used within a conforming file shall be embedded within that file, as defined in PDF Reference 5.8, except when the fonts are used exclusively with text rendering mode 3
       └─ Location: root/document[0]/pages[0](4 0 obj PDPage)/contentStream[0](5 0 obj PDContentStream)/operators[5]/font[0](Helvetica)

[19/26] Validating: simple_document.pdf
   ✓ PASSED - PDF/A-1B validation profile

...

╔══════════════════════════════════════════════════════════════╗
║                      Validation Summary                      ║
╚══════════════════════════════════════════════════════════════╝

Total PDFs validated: 26
  ✓ Passed:  1
  ✗ Failed:  25
  ⚠ Errors:  0

Validation report saved to: C:\path\to\GeneratedPdfs\validation-report.json
```

**Note:** Most BonniePDF example PDFs are not PDF/A compliant by default. PDF/A compliance requires specific metadata, color profiles, and other constraints. To generate PDF/A compliant documents, you'll need to configure BonniePDF with appropriate settings.

## Configuration

### Changing the Validation Profile

By default, the tool validates against PDF/A-1B. To change the validation profile, edit the `DefaultValidationProfile` constant in [`Program.cs`](Program.cs:14):

```csharp
private const string DefaultValidationProfile = "1b"; // Options: 1a, 1b, 2b, 3b
```

### Changing the VeraPDF API URL

Edit the `VeraPdfBaseUrl` constant in [`Program.cs`](Program.cs:13):

```csharp
private const string VeraPdfBaseUrl = "http://localhost:8080/api";
```

### Adjusting Timeout

The validation timeout is set to 5 minutes by default. To change it, modify the timeout in [`ValidatePdfAsync`](Program.cs:177):

```csharp
client.Timeout = TimeSpan.FromMinutes(5);
```

## Troubleshooting

### "Cannot connect to VeraPDF API"

- Ensure the VeraPDF service is running: `docker ps` (if using Docker)
- Check that the service is accessible at `http://localhost:8080/api/info`
- Verify the API endpoint URL in the code matches your VeraPDF service
- Try accessing http://localhost:8080/swagger in your browser to see the API documentation

### "No PDF files found"

- Ensure you've run the BonniePDF Examples project to generate PDFs
- Check that the directory path is correct
- Verify the directory contains `.pdf` files

### Validation Errors

Common PDF/A compliance issues:
- Missing ICC color profiles
- Invalid metadata
- Unsupported fonts
- Incorrect PDF structure

Review the detailed error messages in the console output or JSON report for specific issues.

## Integration with CI/CD

This tool can be integrated into your CI/CD pipeline to automatically validate generated PDFs:

```bash
# Generate PDFs
dotnet run --project BonniePDF.Examples

# Validate PDFs
dotnet run --project BonniePDF.Validation

# Check exit code for CI/CD
if ($LASTEXITCODE -ne 0) { exit 1 }
```

## License

This tool is part of the BonniePDF project.
