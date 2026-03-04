using Microsoft.Playwright;

namespace HtmlToPdf;

public sealed class PlaywrightHtmlToPdfConverter
{
    public async Task<byte[]> ConvertAsync(string html, PlaywrightHtmlToPdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML input cannot be null or whitespace.", nameof(html));
        }

        var effectiveOptions = options ?? new PlaywrightHtmlToPdfOptions();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (effectiveOptions.NavigationTimeoutMs > 0)
        {
            cts.CancelAfter(effectiveOptions.NavigationTimeoutMs);
        }

        using var playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        await using var browser = await LaunchBrowserAsync(playwright, effectiveOptions).ConfigureAwait(false);
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize
            {
                Width = effectiveOptions.ViewportWidth,
                Height = effectiveOptions.ViewportHeight
            }
        }).ConfigureAwait(false);

        var page = await context.NewPageAsync().ConfigureAwait(false);
        await page.EmulateMediaAsync(new PageEmulateMediaOptions
        {
            Media = Media.Print
        }).ConfigureAwait(false);

        await page.SetContentAsync(html, new PageSetContentOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = effectiveOptions.NavigationTimeoutMs
        }).ConfigureAwait(false);

        cts.Token.ThrowIfCancellationRequested();

        return await page.PdfAsync(BuildPdfOptions(effectiveOptions)).ConfigureAwait(false);
    }

    public async Task ConvertToStreamAsync(string html, Stream output, PlaywrightHtmlToPdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", nameof(output));
        }

        var bytes = await ConvertAsync(html, options, cancellationToken).ConfigureAwait(false);
        await output.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IBrowser> LaunchBrowserAsync(IPlaywright playwright, PlaywrightHtmlToPdfOptions options)
    {
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = true
        };

        if (!string.IsNullOrWhiteSpace(options.ChromiumExecutablePath))
        {
            launchOptions.ExecutablePath = options.ChromiumExecutablePath;
        }

        if (options.ChromiumArgs is { Count: > 0 })
        {
            launchOptions.Args = options.ChromiumArgs;
        }

        return await playwright.Chromium.LaunchAsync(launchOptions).ConfigureAwait(false);
    }

    private static PagePdfOptions BuildPdfOptions(PlaywrightHtmlToPdfOptions options)
    {
        return new PagePdfOptions
        {
            Format = options.Format,
            Landscape = options.Landscape,
            Scale = options.Scale,
            PrintBackground = options.PrintBackground,
            PreferCSSPageSize = options.PreferCssPageSize,
            DisplayHeaderFooter = options.DisplayHeaderFooter,
            HeaderTemplate = options.HeaderTemplateHtml,
            FooterTemplate = options.FooterTemplateHtml,
            Margin = new Margin
            {
                Top = options.MarginTop,
                Right = options.MarginRight,
                Bottom = options.MarginBottom,
                Left = options.MarginLeft
            }
        };
    }
}
