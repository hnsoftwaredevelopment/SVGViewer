using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace SVGViewer.Services;

/// <summary>
/// Renders parsed quick-reference Markdown (see <see cref="MarkdownParser"/>) into
/// a WPF <see cref="FlowDocument"/>, so help can be shown inside the app (no
/// browser). The parsing logic lives in <see cref="MarkdownParser"/> and is unit
/// tested; this class only maps the parsed model onto WPF text elements.
/// </summary>
public static class MarkdownToFlowDocument
{
    private static readonly Color HeadingColor = Color.FromRgb(0x1F, 0x29, 0x37);
    private static readonly Color CodeBackground = Color.FromRgb(0xF0, 0xF2, 0xF5);

    public static FlowDocument Convert(string? markdown)
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14,
            LineHeight = 22,
            PagePadding = new Thickness(28, 24, 28, 24)
        };

        foreach (var block in MarkdownParser.Parse(markdown))
        {
            switch (block.Kind)
            {
                case MarkdownBlockKind.Heading1:
                    doc.Blocks.Add(Heading(block.Lines[0], 22, new Thickness(0, 0, 0, 12)));
                    break;
                case MarkdownBlockKind.Heading2:
                    doc.Blocks.Add(Heading(block.Lines[0], 16, new Thickness(0, 14, 0, 6)));
                    break;
                case MarkdownBlockKind.Paragraph:
                    doc.Blocks.Add(Body(block.Lines[0]));
                    break;
                case MarkdownBlockKind.BulletList:
                    doc.Blocks.Add(BulletList(block.Lines));
                    break;
            }
        }

        return doc;
    }

    private static Paragraph Heading(IReadOnlyList<MarkdownSegment> segments, double size, Thickness margin)
    {
        var p = new Paragraph
        {
            FontSize = size,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(HeadingColor),
            Margin = margin
        };
        AddSegments(p.Inlines, segments);
        return p;
    }

    private static Paragraph Body(IReadOnlyList<MarkdownSegment> segments)
    {
        var p = new Paragraph { Margin = new Thickness(0, 0, 0, 10) };
        AddSegments(p.Inlines, segments);
        return p;
    }

    private static List BulletList(IReadOnlyList<IReadOnlyList<MarkdownSegment>> items)
    {
        var list = new List
        {
            MarkerStyle = TextMarkerStyle.Disc,
            Margin = new Thickness(0, 0, 0, 10),
            Padding = new Thickness(22, 0, 0, 0)
        };

        foreach (var item in items)
        {
            var para = new Paragraph { Margin = new Thickness(0, 0, 0, 4) };
            AddSegments(para.Inlines, item);
            list.ListItems.Add(new ListItem(para));
        }

        return list;
    }

    private static void AddSegments(InlineCollection target, IReadOnlyList<MarkdownSegment> segments)
    {
        foreach (var s in segments)
        {
            if (s.Bold)
            {
                target.Add(new Bold(new Run(s.Text)));
            }
            else if (s.Italic)
            {
                target.Add(new Italic(new Run(s.Text)));
            }
            else if (s.Code)
            {
                target.Add(new Run(s.Text)
                {
                    FontFamily = new FontFamily("Consolas"),
                    Background = new SolidColorBrush(CodeBackground)
                });
            }
            else
            {
                target.Add(new Run(s.Text));
            }
        }
    }
}
