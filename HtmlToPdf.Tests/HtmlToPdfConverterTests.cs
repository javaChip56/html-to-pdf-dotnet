using System.Text;

namespace HtmlToPdf.Tests;

public sealed class HtmlToPdfConverterTests
{
    private static readonly HtmlToPdfConverter Converter = new();

    [Fact]
    public void Convert_WithInlineHtml_ReturnsPdfBytes()
    {
        var html = "<h1>Smoke Test</h1><p>Hello from test.</p>";

        var result = Converter.Convert(html);

        Assert.NotNull(result);
        Assert.True(result.Length > 8);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(result, 0, 8));
    }

    [Theory]
    [MemberData(nameof(GetHtmlFilePaths))]
    public void Convert_FromHtmlFile_ReturnsPdfBytes(string htmlFilePath)
    {
        Assert.True(File.Exists(htmlFilePath), $"HTML file not found: {htmlFilePath}");
        var html = File.ReadAllText(htmlFilePath);

        var result = Converter.Convert(html);

        Assert.NotNull(result);
        Assert.True(result.Length > 8, $"PDF bytes were unexpectedly small for '{htmlFilePath}'.");
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(result, 0, 8));
    }

    [Theory]
    [MemberData(nameof(GetHtmlFilePaths))]
    public void Convert_FromHtmlFile_WritesPdfToOutputDirectory_ForManualInspection(string htmlFilePath)
    {
        var outputDirectory = ResolvePdfOutputDirectory();
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        Assert.True(File.Exists(htmlFilePath), $"HTML file not found: {htmlFilePath}");
        var html = File.ReadAllText(htmlFilePath);
        var pdf = Converter.Convert(html);

        Directory.CreateDirectory(outputDirectory);
        var fileName = Path.GetFileNameWithoutExtension(htmlFilePath) + ".pdf";
        var outputPath = Path.Combine(outputDirectory, fileName);
        File.WriteAllBytes(outputPath, pdf);

        Assert.True(File.Exists(outputPath), $"Expected output PDF was not created: {outputPath}");
        Assert.True(new FileInfo(outputPath).Length > 8, $"Output PDF was unexpectedly small: {outputPath}");
    }

    public static IEnumerable<object[]> GetHtmlFilePaths()
    {
        var explicitFile = Environment.GetEnvironmentVariable("HTMLTOPDF_TEST_HTML_FILE");
        if (!string.IsNullOrWhiteSpace(explicitFile))
        {
            yield return [Path.GetFullPath(explicitFile)];
            yield break;
        }

        var directory = ResolveInputDirectory();
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"HTML input directory does not exist: {directory}");
        }

        var files = Directory.GetFiles(directory, "*.html", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
        {
            throw new InvalidOperationException(
                $"No .html files found in '{directory}'. Set HTMLTOPDF_TEST_HTML_DIR or add test files.");
        }

        foreach (var file in files)
        {
            yield return [Path.GetFullPath(file)];
        }
    }

    private static string ResolveInputDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("HTMLTOPDF_TEST_HTML_DIR");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory);
        }

        var assemblyDirectory = AppContext.BaseDirectory;
        return Path.GetFullPath(Path.Combine(assemblyDirectory, "TestData", "InputHtml"));
    }

    private static string? ResolvePdfOutputDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("HTMLTOPDF_TEST_PDF_OUTPUT_DIR");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return Path.GetFullPath(configuredDirectory);
        }

        return null;
    }
}
