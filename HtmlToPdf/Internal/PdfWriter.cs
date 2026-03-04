using System.Globalization;
using System.Text;

namespace HtmlToPdf;

internal static class PdfWriter
{
    public static byte[] Write(IReadOnlyList<PdfPage> pages, HtmlToPdfOptions options)
    {
        var objects = new List<string>();
        objects.Add("<< /Type /Catalog /Pages 2 0 R >>"); // 1
        objects.Add(string.Empty); // 2 placeholder for /Pages
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"); // 3
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>"); // 4
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Oblique >>"); // 5
        objects.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-BoldOblique >>"); // 6

        var pageObjectIds = new List<int>(pages.Count);
        foreach (var page in pages)
        {
            var content = BuildContentStream(page, options);
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}\nendstream");
            var contentObjectId = objects.Count;

            objects.Add(BuildPageObject(contentObjectId, options));
            pageObjectIds.Add(objects.Count);
        }

        objects[1] = BuildPagesObject(pageObjectIds);
        return BuildPdfBinary(objects);
    }

    private static string BuildContentStream(PdfPage page, HtmlToPdfOptions options)
    {
        var sb = new StringBuilder();
        var y = options.PageHeightPoints - options.MarginTopPoints;

        foreach (var line in page.Lines)
        {
            y -= line.HeightPoints;
            var x = options.MarginLeftPoints;

            foreach (var run in line.Runs)
            {
                if (string.IsNullOrEmpty(run.Text))
                {
                    continue;
                }

                sb.Append("BT\n");
                sb.Append('/').Append(run.FontResourceName).Append(' ');
                sb.Append(FormatNumber(run.FontSizePoints)).Append(" Tf\n");
                sb.Append("1 0 0 1 ").Append(FormatNumber(x)).Append(' ').Append(FormatNumber(y)).Append(" Tm\n");
                sb.Append('(').Append(EscapePdfString(run.Text)).Append(") Tj\n");
                sb.Append("ET\n");

                x += run.WidthPoints;
            }
        }

        return sb.ToString();
    }

    private static string BuildPageObject(int contentObjectId, HtmlToPdfOptions options)
    {
        var width = FormatNumber(options.PageWidthPoints);
        var height = FormatNumber(options.PageHeightPoints);
        return
            $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {width} {height}] " +
            "/Resources << /Font << /F1 3 0 R /F2 4 0 R /F3 5 0 R /F4 6 0 R >> >> " +
            $"/Contents {contentObjectId} 0 R >>";
    }

    private static string BuildPagesObject(IReadOnlyList<int> pageObjectIds)
    {
        var kids = string.Join(' ', pageObjectIds.Select(id => $"{id} 0 R"));
        return $"<< /Type /Pages /Count {pageObjectIds.Count} /Kids [{kids}] >>";
    }

    private static byte[] BuildPdfBinary(IReadOnlyList<string> objects)
    {
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n");

        var offsets = new List<int> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append(i + 1).Append(" 0 obj\n");
            sb.Append(objects[i]).Append("\n");
            sb.Append("endobj\n");
        }

        var xrefOffset = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append("xref\n");
        sb.Append("0 ").Append(objects.Count + 1).Append('\n');
        sb.Append("0000000000 65535 f \n");
        for (var i = 1; i < offsets.Count; i++)
        {
            sb.Append(offsets[i].ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        sb.Append("trailer\n");
        sb.Append("<< /Size ").Append(objects.Count + 1).Append(" /Root 1 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append(xrefOffset.ToString(CultureInfo.InvariantCulture)).Append('\n');
        sb.Append("%%EOF\n");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string FormatNumber(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string EscapePdfString(string text)
    {
        return text
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
    }
}
