using System.Text;

namespace HtmlToPdf.Tests;

public sealed class PlaywrightHtmlToPdfConverterTests
{
    [Theory]
    [MemberData(nameof(HtmlToPdfConverterTests.GetHtmlFilePaths), MemberType = typeof(HtmlToPdfConverterTests))]
    public async Task ConvertAsync_FromHtmlFile_ReturnsPdfBytes(string htmlFilePath)
    {
        if (!ShouldRunPlaywrightTests())
        {
            return;
        }

        var converter = new PlaywrightHtmlToPdfConverter();
        var html = File.ReadAllText(htmlFilePath);

        var result = await converter.ConvertAsync(html);

        Assert.NotNull(result);
        Assert.True(result.Length > 8);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(result, 0, 8));
    }

    [Theory]
    [MemberData(nameof(HtmlToPdfConverterTests.GetHtmlFilePaths), MemberType = typeof(HtmlToPdfConverterTests))]
    public async Task ConvertAsync_CanWriteOutput_ForManualInspection(string htmlFilePath)
    {
        if (!ShouldRunPlaywrightTests())
        {
            return;
        }

        var outputDirectory = Environment.GetEnvironmentVariable("HTMLTOPDF_TEST_PDF_OUTPUT_DIR");
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            return;
        }

        var converter = new PlaywrightHtmlToPdfConverter();
        var html = File.ReadAllText(htmlFilePath);
        var pdf = await converter.ConvertAsync(html);

        Directory.CreateDirectory(outputDirectory);
        var fileName = Path.GetFileNameWithoutExtension(htmlFilePath) + ".playwright.pdf";
        var outputPath = Path.Combine(outputDirectory, fileName);
        await File.WriteAllBytesAsync(outputPath, pdf);

        Assert.True(File.Exists(outputPath), $"Expected output PDF was not created: {outputPath}");
        Assert.True(new FileInfo(outputPath).Length > 8, $"Output PDF was unexpectedly small: {outputPath}");
    }

    private static bool ShouldRunPlaywrightTests()
    {
        var enabled = Environment.GetEnvironmentVariable("HTMLTOPDF_RUN_PLAYWRIGHT_TESTS");
        return string.Equals(enabled, "true", StringComparison.OrdinalIgnoreCase)
               || string.Equals(enabled, "1", StringComparison.OrdinalIgnoreCase);
    }
}
