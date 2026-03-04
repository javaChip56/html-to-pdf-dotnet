namespace HtmlToPdf;

internal sealed record TextChunk(string Text, TextStyle Style, bool IsLineBreak = false);

internal sealed record TextStyle(bool Bold, bool Italic, float FontSizePoints);

internal sealed record PdfRun(string Text, string FontResourceName, float FontSizePoints, float WidthPoints);

internal sealed record PdfLine(IReadOnlyList<PdfRun> Runs, float HeightPoints);

internal sealed record PdfPage(IReadOnlyList<PdfLine> Lines);
