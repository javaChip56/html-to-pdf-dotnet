# HtmlToPdf (.NET)

This folder contains a .NET 8 class library with two renderers:

- `HtmlToPdfConverter`: lightweight, built-in .NET only (limited HTML/CSS support)
- `PlaywrightHtmlToPdfConverter`: Chromium-backed renderer via Playwright (modern HTML/CSS support)

## Project

- Library path: `html-topdf/HtmlToPdf`
- Target framework: `net8.0`
- External package: `Microsoft.Playwright` (for Playwright renderer)

## Public API

Lightweight renderer:

```csharp
var converter = new HtmlToPdfConverter();
byte[] pdfBytes = converter.Convert("<h1>Hello</h1><p>Fast, simple PDF.</p>");
```

Playwright renderer:

```csharp
var converter = new PlaywrightHtmlToPdfConverter();
byte[] pdfBytes = await converter.ConvertAsync("<h1>Hello</h1><div style='display:grid'>Modern CSS</div>");
```

## Linux container usage (Playwright)

Install Playwright browsers in your image:

```bash
pwsh bin/Release/net8.0/playwright.ps1 install --with-deps chromium
```

Typical multi-stage Docker approach:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore HtmlToPdf/HtmlToPdf.csproj
RUN dotnet build HtmlToPdf/HtmlToPdf.csproj -c Release
RUN pwsh /src/HtmlToPdf/bin/Release/net8.0/playwright.ps1 install --with-deps chromium

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /src/HtmlToPdf/bin/Release/net8.0/ ./
```

## Lightweight renderer supported HTML subset

- Headings: `h1` to `h6`
- Paragraph/block flow: `p`, `div`, `br`
- Emphasis: `b`, `strong`, `i`, `em`
- Lists: `ul`, `ol`, `li`
- Text content and HTML entities

## Notes

- Use `HtmlToPdfConverter` when you need zero browser runtime and simple document formatting.
- Use `PlaywrightHtmlToPdfConverter` when you need modern CSS/layout fidelity.

## Tests

Test project:

- `html-topdf/HtmlToPdf.Tests`

Run:

```bash
dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release -p:RestoreIgnoreFailedSources=true
```

Use HTML files from a specific directory:

```bash
HTMLTOPDF_TEST_HTML_DIR=/path/to/html dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release -p:RestoreIgnoreFailedSources=true
```

Use one specific file:

```bash
HTMLTOPDF_TEST_HTML_FILE=/path/to/file.html dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release -p:RestoreIgnoreFailedSources=true
```

Write generated PDFs for manual inspection (opt-in):

```bash
HTMLTOPDF_TEST_HTML_DIR=/path/to/html HTMLTOPDF_TEST_PDF_OUTPUT_DIR=/path/to/output dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release -p:RestoreIgnoreFailedSources=true
```

Run Playwright integration tests (opt-in):

```bash
HTMLTOPDF_RUN_PLAYWRIGHT_TESTS=true dotnet test HtmlToPdf.Tests/HtmlToPdf.Tests.csproj -c Release -p:RestoreIgnoreFailedSources=true
```

## GitHub Actions

The repository includes separate GitHub Actions workflows for build, code review, test, and release:

- `Build`: restores and compiles the library and test projects
- `Code Review`: verifies formatting and fails on compiler warnings
- `Test`: runs the standard test suite and supports an opt-in Playwright integration run via manual dispatch
- `Release`: packs and publishes to NuGet when a Git tag matching `v*` is pushed

For NuGet publishing, configure the repository secret `NUGET_API_KEY` with a NuGet.org API key.
