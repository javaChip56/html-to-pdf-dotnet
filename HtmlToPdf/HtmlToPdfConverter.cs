using System.Text;

namespace HtmlToPdf;

public sealed class HtmlToPdfConverter
{
    public byte[] Convert(string html, HtmlToPdfOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            throw new ArgumentException("HTML input cannot be null or whitespace.", nameof(html));
        }

        var effectiveOptions = options ?? new HtmlToPdfOptions();
        var chunks = HtmlTokenizer.Tokenize(html, effectiveOptions.BaseFontSizePoints);
        var layout = TextLayouter.Layout(chunks, effectiveOptions);
        return PdfWriter.Write(layout, effectiveOptions);
    }

    public void ConvertToStream(string html, Stream output, HtmlToPdfOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", nameof(output));
        }

        var bytes = Convert(html, options);
        output.Write(bytes, 0, bytes.Length);
        output.Flush();
    }

    public async Task ConvertToStreamAsync(string html, Stream output, HtmlToPdfOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (!output.CanWrite)
        {
            throw new ArgumentException("Output stream must be writable.", nameof(output));
        }

        var bytes = Convert(html, options);
        await output.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
