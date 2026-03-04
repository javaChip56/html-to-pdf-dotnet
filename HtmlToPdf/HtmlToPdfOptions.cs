namespace HtmlToPdf;

public sealed class HtmlToPdfOptions
{
    public float PageWidthPoints { get; init; } = 595f;
    public float PageHeightPoints { get; init; } = 842f;
    public float MarginTopPoints { get; init; } = 56f;
    public float MarginRightPoints { get; init; } = 56f;
    public float MarginBottomPoints { get; init; } = 56f;
    public float MarginLeftPoints { get; init; } = 56f;
    public float BaseFontSizePoints { get; init; } = 12f;
    public float LineHeightMultiplier { get; init; } = 1.35f;
}
