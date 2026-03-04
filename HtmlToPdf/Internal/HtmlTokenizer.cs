using System.Net;
using System.Text.RegularExpressions;

namespace HtmlToPdf;

internal static class HtmlTokenizer
{
    private static readonly Regex TokenRegex = new("<[^>]+>|[^<]+", RegexOptions.Compiled);
    private static readonly Regex TagNameRegex = new(@"^</?\s*([a-zA-Z0-9]+)", RegexOptions.Compiled);

    public static IReadOnlyList<TextChunk> Tokenize(string html, float baseFontSize)
    {
        var chunks = new List<TextChunk>();

        var boldDepth = 0;
        var italicDepth = 0;
        var headingSizes = new Stack<float>();
        var listStack = new Stack<ListState>();

        foreach (Match match in TokenRegex.Matches(html))
        {
            var token = match.Value;
            if (token.Length == 0)
            {
                continue;
            }

            if (token[0] == '<')
            {
                ProcessTag(token, chunks, ref boldDepth, ref italicDepth, headingSizes, listStack, baseFontSize);
                continue;
            }

            var text = NormalizeWhitespace(WebUtility.HtmlDecode(token));
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            chunks.Add(new TextChunk(text, ResolveStyle(boldDepth, italicDepth, headingSizes, baseFontSize)));
        }

        return chunks;
    }

    private static void ProcessTag(
        string tagToken,
        List<TextChunk> chunks,
        ref int boldDepth,
        ref int italicDepth,
        Stack<float> headingSizes,
        Stack<ListState> listStack,
        float baseFontSize)
    {
        var tagMatch = TagNameRegex.Match(tagToken);
        if (!tagMatch.Success)
        {
            return;
        }

        var tag = tagMatch.Groups[1].Value.ToLowerInvariant();
        var isClosing = tagToken.StartsWith("</", StringComparison.Ordinal);

        if (isClosing)
        {
            switch (tag)
            {
                case "b":
                case "strong":
                    if (boldDepth > 0)
                    {
                        boldDepth--;
                    }
                    break;
                case "i":
                case "em":
                    if (italicDepth > 0)
                    {
                        italicDepth--;
                    }
                    break;
                case "h1":
                case "h2":
                case "h3":
                case "h4":
                case "h5":
                case "h6":
                    if (headingSizes.Count > 0)
                    {
                        headingSizes.Pop();
                    }
                    AddLineBreak(chunks, 2);
                    break;
                case "p":
                case "div":
                    AddLineBreak(chunks, 2);
                    break;
                case "li":
                    AddLineBreak(chunks, 1);
                    break;
                case "ul":
                case "ol":
                    if (listStack.Count > 0)
                    {
                        listStack.Pop();
                    }
                    AddLineBreak(chunks, 1);
                    break;
            }

            return;
        }

        switch (tag)
        {
            case "br":
                AddLineBreak(chunks, 1);
                break;
            case "p":
            case "div":
                AddLineBreak(chunks, 1);
                break;
            case "b":
            case "strong":
                boldDepth++;
                break;
            case "i":
            case "em":
                italicDepth++;
                break;
            case "h1":
                headingSizes.Push(baseFontSize * 2f);
                AddLineBreak(chunks, 1);
                break;
            case "h2":
                headingSizes.Push(baseFontSize * 1.75f);
                AddLineBreak(chunks, 1);
                break;
            case "h3":
                headingSizes.Push(baseFontSize * 1.5f);
                AddLineBreak(chunks, 1);
                break;
            case "h4":
                headingSizes.Push(baseFontSize * 1.35f);
                AddLineBreak(chunks, 1);
                break;
            case "h5":
                headingSizes.Push(baseFontSize * 1.2f);
                AddLineBreak(chunks, 1);
                break;
            case "h6":
                headingSizes.Push(baseFontSize * 1.1f);
                AddLineBreak(chunks, 1);
                break;
            case "ul":
                listStack.Push(new ListState(false));
                AddLineBreak(chunks, 1);
                break;
            case "ol":
                listStack.Push(new ListState(true));
                AddLineBreak(chunks, 1);
                break;
            case "li":
                AddLineBreak(chunks, 1);
                var prefix = GetListPrefix(listStack);
                chunks.Add(new TextChunk(prefix, ResolveStyle(boldDepth, italicDepth, headingSizes, baseFontSize)));
                break;
        }
    }

    private static string GetListPrefix(Stack<ListState> listStack)
    {
        if (listStack.Count == 0)
        {
            return "- ";
        }

        var state = listStack.Peek();
        if (!state.Ordered)
        {
            return "- ";
        }

        state.Counter++;
        return $"{state.Counter}. ";
    }

    private static TextStyle ResolveStyle(int boldDepth, int italicDepth, Stack<float> headingSizes, float baseFontSize)
    {
        var fontSize = headingSizes.Count > 0 ? headingSizes.Peek() : baseFontSize;
        return new TextStyle(boldDepth > 0, italicDepth > 0, fontSize);
    }

    private static void AddLineBreak(List<TextChunk> chunks, int count)
    {
        for (var i = 0; i < count; i++)
        {
            chunks.Add(new TextChunk(string.Empty, new TextStyle(false, false, 0f), true));
        }
    }

    private static string NormalizeWhitespace(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input, @"\s+", " ");
    }

    private sealed class ListState(bool ordered)
    {
        public bool Ordered { get; } = ordered;
        public int Counter { get; set; }
    }
}
