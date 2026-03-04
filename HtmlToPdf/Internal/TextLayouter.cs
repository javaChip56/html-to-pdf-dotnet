using System.Text.RegularExpressions;

namespace HtmlToPdf;

internal static class TextLayouter
{
    private static readonly Regex TextPartRegex = new(@"\S+\s*|\s+", RegexOptions.Compiled);

    public static IReadOnlyList<PdfPage> Layout(IReadOnlyList<TextChunk> chunks, HtmlToPdfOptions options)
    {
        var lines = BuildLines(chunks, options);
        return Paginate(lines, options);
    }

    private static List<PdfLine> BuildLines(IReadOnlyList<TextChunk> chunks, HtmlToPdfOptions options)
    {
        var availableWidth = options.PageWidthPoints - options.MarginLeftPoints - options.MarginRightPoints;
        var output = new List<PdfLine>();
        var currentRuns = new List<PdfRun>();
        var currentWidth = 0f;
        var currentLineHeight = options.BaseFontSizePoints * options.LineHeightMultiplier;

        foreach (var chunk in chunks)
        {
            if (chunk.IsLineBreak)
            {
                FinalizeLine(output, currentRuns, currentLineHeight, options);
                currentWidth = 0f;
                currentLineHeight = options.BaseFontSizePoints * options.LineHeightMultiplier;
                continue;
            }

            foreach (Match partMatch in TextPartRegex.Matches(chunk.Text))
            {
                var part = partMatch.Value;
                if (part.Length == 0)
                {
                    continue;
                }

                var style = chunk.Style;
                var partWidth = EstimateWidth(part, style.FontSizePoints);
                var partLineHeight = style.FontSizePoints * options.LineHeightMultiplier;
                currentLineHeight = Math.Max(currentLineHeight, partLineHeight);

                if (currentWidth + partWidth <= availableWidth || currentWidth <= 0.01f)
                {
                    AppendRun(currentRuns, part, style);
                    currentWidth += partWidth;
                    continue;
                }

                FinalizeLine(output, currentRuns, currentLineHeight, options);
                currentWidth = 0f;
                currentLineHeight = partLineHeight;

                if (partWidth <= availableWidth)
                {
                    AppendRun(currentRuns, part, style);
                    currentWidth += partWidth;
                    continue;
                }

                BreakLongWord(output, currentRuns, ref currentWidth, ref currentLineHeight, part, style, availableWidth, options);
            }
        }

        if (currentRuns.Count > 0)
        {
            FinalizeLine(output, currentRuns, currentLineHeight, options);
        }

        return output;
    }

    private static List<PdfPage> Paginate(IReadOnlyList<PdfLine> lines, HtmlToPdfOptions options)
    {
        var pages = new List<PdfPage>();
        var currentPageLines = new List<PdfLine>();
        var availableHeight = options.PageHeightPoints - options.MarginTopPoints - options.MarginBottomPoints;
        var usedHeight = 0f;

        foreach (var line in lines)
        {
            if (usedHeight + line.HeightPoints > availableHeight && currentPageLines.Count > 0)
            {
                pages.Add(new PdfPage(currentPageLines.ToArray()));
                currentPageLines = new List<PdfLine>();
                usedHeight = 0f;
            }

            currentPageLines.Add(line);
            usedHeight += line.HeightPoints;
        }

        if (currentPageLines.Count == 0)
        {
            currentPageLines.Add(new PdfLine(Array.Empty<PdfRun>(), options.BaseFontSizePoints * options.LineHeightMultiplier));
        }

        pages.Add(new PdfPage(currentPageLines.ToArray()));
        return pages;
    }

    private static void BreakLongWord(
        List<PdfLine> output,
        List<PdfRun> currentRuns,
        ref float currentWidth,
        ref float currentLineHeight,
        string part,
        TextStyle style,
        float availableWidth,
        HtmlToPdfOptions options)
    {
        var index = 0;
        while (index < part.Length)
        {
            var maxChars = Math.Max(1, (int)Math.Floor((availableWidth - currentWidth) / (style.FontSizePoints * 0.6f)));
            var take = Math.Min(maxChars, part.Length - index);
            var fragment = part.Substring(index, take);
            AppendRun(currentRuns, fragment, style);
            currentWidth += EstimateWidth(fragment, style.FontSizePoints);
            index += take;

            if (index < part.Length)
            {
                FinalizeLine(output, currentRuns, currentLineHeight, options);
                currentWidth = 0f;
                currentLineHeight = style.FontSizePoints * options.LineHeightMultiplier;
            }
        }
    }

    private static void AppendRun(List<PdfRun> currentRuns, string text, TextStyle style)
    {
        var fontName = ResolveFont(style);
        var width = EstimateWidth(text, style.FontSizePoints);

        if (currentRuns.Count > 0)
        {
            var last = currentRuns[^1];
            if (last.FontResourceName == fontName && Math.Abs(last.FontSizePoints - style.FontSizePoints) < 0.01f)
            {
                currentRuns[^1] = last with
                {
                    Text = last.Text + text,
                    WidthPoints = last.WidthPoints + width
                };
                return;
            }
        }

        currentRuns.Add(new PdfRun(text, fontName, style.FontSizePoints, width));
    }

    private static string ResolveFont(TextStyle style)
    {
        if (style.Bold && style.Italic)
        {
            return "F4";
        }

        if (style.Bold)
        {
            return "F2";
        }

        if (style.Italic)
        {
            return "F3";
        }

        return "F1";
    }

    private static float EstimateWidth(string text, float fontSizePoints) => text.Length * fontSizePoints * 0.6f;

    private static void FinalizeLine(List<PdfLine> output, List<PdfRun> currentRuns, float lineHeight, HtmlToPdfOptions options)
    {
        if (currentRuns.Count == 0)
        {
            output.Add(new PdfLine(Array.Empty<PdfRun>(), Math.Max(lineHeight, options.BaseFontSizePoints * options.LineHeightMultiplier)));
            return;
        }

        output.Add(new PdfLine(currentRuns.ToArray(), Math.Max(lineHeight, options.BaseFontSizePoints * options.LineHeightMultiplier)));
        currentRuns.Clear();
    }
}
