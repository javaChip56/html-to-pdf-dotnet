# HtmlToPdf (.NET, no external dependencies)

This folder contains a .NET 8 class library that converts HTML to PDF using only built-in .NET APIs.

## Project

- Library path: `html-topdf/HtmlToPdf`
- Target framework: `net8.0`
- External packages: none

## Public API

```csharp
var converter = new HtmlToPdfConverter();
byte[] pdfBytes = converter.Convert("<h1>Hello</h1><p>Container-safe PDF.</p>");
```

Or stream directly:

```csharp
await converter.ConvertToStreamAsync(html, outputStream, cancellationToken: ct);
```

## Linux container usage

No native dependencies are required by this library. You only need the .NET runtime image.

Example Docker base:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
```

## Supported HTML subset

- Headings: `h1` to `h6`
- Paragraph/block flow: `p`, `div`, `br`
- Emphasis: `b`, `strong`, `i`, `em`
- Lists: `ul`, `ol`, `li`
- Text content and HTML entities

## Notes

- The converter is intentionally lightweight and does not implement full browser layout/CSS.
- Good fit for server-side report or document generation where simple formatted text output is enough.

## Tests

Test project:

- `html-topdf/HtmlToPdf.Tests`

Run:

```bash
dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release
```

Use HTML files from a specific directory:

```bash
HTMLTOPDF_TEST_HTML_DIR=/path/to/html dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release
```

Use one specific file:

```bash
HTMLTOPDF_TEST_HTML_FILE=/path/to/file.html dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release
```

Write generated PDFs for manual inspection (opt-in):

```bash
HTMLTOPDF_TEST_HTML_DIR=/path/to/html HTMLTOPDF_TEST_PDF_OUTPUT_DIR=/path/to/output dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release
```
