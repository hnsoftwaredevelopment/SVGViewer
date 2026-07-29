using System.Text;
using System.Text.RegularExpressions;

namespace SVGViewer.Services;

/// <summary>
/// A small, self-contained Markdown-to-HTML converter, scoped to what the user
/// guides need: headings, paragraphs, bold/italic, inline &amp; fenced code,
/// bullet/number lists, blockquotes, horizontal rules, links and images. It is
/// intentionally not a full CommonMark implementation; it is easy to test and
/// has no external dependencies.
/// </summary>
public static class MarkdownToHtml
{
    /// <summary>
    /// Converts <paramref name="markdown"/> to a complete, styled HTML document.
    /// Relative image sources are prefixed with <paramref name="imageBaseUri"/>
    /// (e.g. a <c>file:///</c> path to the shipped images folder) when supplied.
    /// </summary>
    public static string Convert(string markdown, string? title = null, string? imageBaseUri = null)
    {
        var body = ConvertBody(markdown ?? string.Empty, imageBaseUri);
        var head = WebUtilityEscape(title ?? "SVG Viewer");

        return Template
            .Replace("@@TITLE@@", head)
            .Replace("@@BODY@@", body);
    }

    private const string Template = """
        <!DOCTYPE html>
        <html lang="nl">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
        <title>@@TITLE@@</title>
        <style>
        :root { color-scheme: light dark; }
        body { font-family: Segoe UI, system-ui, sans-serif; line-height: 1.6;
               max-width: 820px; margin: 2rem auto; padding: 0 1.25rem; }
        h1, h2, h3 { line-height: 1.25; }
        h1 { border-bottom: 2px solid #2b6cb0; padding-bottom: .3rem; }
        h2 { border-bottom: 1px solid #cbd5e0; padding-bottom: .2rem; margin-top: 2rem; }
        code { background: rgba(127,127,127,.15); padding: .1rem .3rem; border-radius: 4px;
               font-family: Consolas, monospace; font-size: .9em; }
        pre { background: rgba(127,127,127,.12); padding: 1rem; border-radius: 8px; overflow: auto; }
        pre code { background: none; padding: 0; }
        blockquote { border-left: 4px solid #2b6cb0; margin: 1rem 0; padding: .25rem 1rem; opacity: .85; }
        img { max-width: 100%; height: auto; border-radius: 6px; box-shadow: 0 1px 6px rgba(0,0,0,.15); }
        a { color: #2b6cb0; }
        hr { border: none; border-top: 1px solid #cbd5e0; margin: 2rem 0; }
        </style>
        </head>
        <body>
        @@BODY@@
        </body>
        </html>
        """;

    private static string ConvertBody(string markdown, string? imageBaseUri)
    {
        var lines = markdown.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var html = new StringBuilder();

        var paragraph = new List<string>();
        var listItems = new List<string>();
        var quoteLines = new List<string>();
        var listOrdered = false;
        var inFence = false;
        var fence = new StringBuilder();

        void FlushParagraph()
        {
            if (paragraph.Count == 0) return;
            html.Append("<p>").Append(Inline(string.Join(" ", paragraph), imageBaseUri)).Append("</p>\n");
            paragraph.Clear();
        }

        void FlushList()
        {
            if (listItems.Count == 0) return;
            var tag = listOrdered ? "ol" : "ul";
            html.Append('<').Append(tag).Append(">\n");
            foreach (var item in listItems)
                html.Append("<li>").Append(Inline(item, imageBaseUri)).Append("</li>\n");
            html.Append("</").Append(tag).Append(">\n");
            listItems.Clear();
        }

        void FlushQuote()
        {
            if (quoteLines.Count == 0) return;
            html.Append("<blockquote>").Append(Inline(string.Join(" ", quoteLines), imageBaseUri)).Append("</blockquote>\n");
            quoteLines.Clear();
        }

        void FlushAll()
        {
            FlushParagraph();
            FlushList();
            FlushQuote();
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();

            if (line.TrimStart().StartsWith("```"))
            {
                if (inFence)
                {
                    html.Append("<pre><code>").Append(Escape(fence.ToString())).Append("</code></pre>\n");
                    fence.Clear();
                    inFence = false;
                }
                else
                {
                    FlushAll();
                    inFence = true;
                }
                continue;
            }

            if (inFence)
            {
                fence.Append(raw).Append('\n');
                continue;
            }

            if (line.Length == 0)
            {
                FlushAll();
                continue;
            }

            var heading = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
            if (heading.Success)
            {
                FlushAll();
                var level = heading.Groups[1].Value.Length;
                html.Append("<h").Append(level).Append('>')
                    .Append(Inline(heading.Groups[2].Value, imageBaseUri))
                    .Append("</h").Append(level).Append(">\n");
                continue;
            }

            if (Regex.IsMatch(line, @"^\s*([-*_])(\s*\1){2,}\s*$"))
            {
                FlushAll();
                html.Append("<hr>\n");
                continue;
            }

            var quote = Regex.Match(line, @"^>\s?(.*)$");
            if (quote.Success)
            {
                FlushParagraph();
                FlushList();
                quoteLines.Add(quote.Groups[1].Value);
                continue;
            }

            var ul = Regex.Match(line, @"^\s*[-*]\s+(.*)$");
            var ol = Regex.Match(line, @"^\s*\d+\.\s+(.*)$");
            if (ul.Success || ol.Success)
            {
                FlushParagraph();
                FlushQuote();
                var ordered = ol.Success;
                if (listItems.Count > 0 && ordered != listOrdered)
                    FlushList();
                listOrdered = ordered;
                listItems.Add((ordered ? ol : ul).Groups[1].Value);
                continue;
            }

            FlushList();
            FlushQuote();
            paragraph.Add(line);
        }

        if (inFence)
            html.Append("<pre><code>").Append(Escape(fence.ToString())).Append("</code></pre>\n");
        FlushAll();
        return html.ToString();
    }

    private static string Inline(string text, string? imageBaseUri)
    {
        // Protect inline-code spans from further formatting via placeholders.
        var codes = new List<string>();
        text = Regex.Replace(text, "`([^`]+)`", m =>
        {
            codes.Add(m.Groups[1].Value);
            return $"\u0000{codes.Count - 1}\u0000";
        });

        text = Escape(text);

        // Images before links (they share the [..](..) shape).
        text = Regex.Replace(text, @"!\[([^\]]*)\]\(([^)]+)\)", m =>
        {
            var src = ResolveImage(m.Groups[2].Value.Trim(), imageBaseUri);
            return $"<img src=\"{src}\" alt=\"{m.Groups[1].Value}\">";
        });

        text = Regex.Replace(text, @"\[([^\]]+)\]\(([^)]+)\)", m =>
            $"<a href=\"{m.Groups[2].Value.Trim()}\">{m.Groups[1].Value}</a>");

        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "<strong>$1</strong>");
        text = Regex.Replace(text, @"(?<!\*)\*([^*]+)\*(?!\*)", "<em>$1</em>");
        text = Regex.Replace(text, @"(?<![A-Za-z0-9_])_([^_]+)_(?![A-Za-z0-9_])", "<em>$1</em>");

        text = Regex.Replace(text, "\u0000(\\d+)\u0000", m =>
            $"<code>{Escape(codes[int.Parse(m.Groups[1].Value)])}</code>");

        return text;
    }

    private static string ResolveImage(string src, string? imageBaseUri)
    {
        if (string.IsNullOrEmpty(imageBaseUri)) return src;
        if (Regex.IsMatch(src, @"^[a-zA-Z][a-zA-Z0-9+.-]*:")) return src; // already absolute (http:, file:, data:)
        return imageBaseUri.TrimEnd('/') + "/" + src.TrimStart('.', '/');
    }

    private static string Escape(string s) => s
        .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string WebUtilityEscape(string s) => Escape(s).Replace("\"", "&quot;");
}
