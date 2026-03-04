namespace HtmlToPdf;

public sealed class PlaywrightHtmlToPdfOptions
{
    public bool PrintBackground { get; init; } = true;
    public bool PreferCssPageSize { get; init; } = true;
    public bool Landscape { get; init; }
    public float Scale { get; init; } = 1.0f;
    public string? Format { get; init; } = "A4";

    public string MarginTop { get; init; } = "16mm";
    public string MarginRight { get; init; } = "12mm";
    public string MarginBottom { get; init; } = "16mm";
    public string MarginLeft { get; init; } = "12mm";

    public string? HeaderTemplateHtml { get; init; }
    public string? FooterTemplateHtml { get; init; }
    public bool DisplayHeaderFooter => !string.IsNullOrWhiteSpace(HeaderTemplateHtml) || !string.IsNullOrWhiteSpace(FooterTemplateHtml);

    public int ViewportWidth { get; init; } = 1366;
    public int ViewportHeight { get; init; } = 900;
    public int NavigationTimeoutMs { get; init; } = 30000;

    public string? ChromiumExecutablePath { get; init; }
    public IReadOnlyList<string>? ChromiumArgs { get; init; }
}
