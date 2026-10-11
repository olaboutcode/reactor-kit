using System.Globalization;
using System.Text;
using Markdig;
using Markdig.Extensions.Abbreviations;
using Markdig.Extensions.Alerts;
using Markdig.Extensions.DefinitionLists;
using Markdig.Extensions.Footnotes;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.Tables;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MdTable = Markdig.Extensions.Tables.Table;
using Span = Reactor.Kit.Internals.Markdown.MarkdownInlines.Span;
using Style = Reactor.Kit.Internals.Markdown.MarkdownInlines.Style;

namespace Reactor.Kit.Internals.Markdown;

/// <summary>
/// Turns markdown into blocks. CommonMark and the usual GitHub extensions are recognized.
/// Picture files are not opened.
/// </summary>
internal static class MarkdownParser
{
    internal enum ColumnAlign { Left, Center, Right }

    internal enum FlowDirection { Down, Up, Right, Left }

    internal enum NodeShape { Box, Round, Diamond }

    internal abstract record Block;

    internal sealed record Heading(int Level, List<Span> Inlines) : Block;

    internal sealed record Paragraph(List<Span> Inlines) : Block;

    internal sealed record Quote(IReadOnlyList<Block> Children) : Block;

    internal sealed record Code(string Text) : Block;

    internal sealed record Item(string Marker, List<Block> Children);

    internal sealed record Items(IReadOnlyList<Item> Entries) : Block;

    internal sealed record Table(
        IReadOnlyList<IReadOnlyList<List<Span>>> Rows,
        IReadOnlyList<bool> Header,
        IReadOnlyList<ColumnAlign> Align) : Block;

    internal sealed record DiagramNode(string Id, string Label, NodeShape Shape);

    internal sealed record DiagramEdge(string From, string To, string Label);

    internal sealed record Diagram(
        FlowDirection Direction,
        IReadOnlyList<DiagramNode> Nodes,
        IReadOnlyList<DiagramEdge> Edges) : Block;

    internal sealed record Rule : Block;

    internal sealed record Defined(List<Span> Term, List<Block> Body);

    internal sealed record Definitions(IReadOnlyList<Defined> Entries) : Block;

    static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley(false)
        .UseSmartyPants()
        .UseYamlFrontMatter()
        .UseCjkFriendlyEmphasis()
        .Build();

    public static List<Block> Parse(string? markdown)
    {
        var blocks = new List<Block>();
        if (string.IsNullOrEmpty(markdown))
            return blocks;

        foreach (var block in Markdig.Markdown.Parse(markdown, Pipeline))
            Add(block, blocks);
        return blocks;
    }

    static void Add(Markdig.Syntax.Block block, List<Block> target)
    {
        if (block is LinkReferenceDefinition or LinkReferenceDefinitionGroup)
            return;
        if (block is FootnoteGroup notes)
        {
            AddNotes(notes, target);
            return;
        }

        var one = One(block);
        if (one is not null)
        {
            target.Add(one);
            return;
        }

        if (block is not ContainerBlock container)
            return;
        foreach (var child in container)
            Add(child, target);
    }

    static Block? One(Markdig.Syntax.Block block) => block switch
    {
        HeadingBlock heading => new Heading(heading.Level, Inlines(heading)),
        ParagraphBlock paragraph => ParagraphOf(paragraph),
        AlertBlock alert => new Quote(AlertChildren(alert)),
        QuoteBlock quote => new Quote(Children(quote)),
        MathBlock math => new Code(Lines(math)),
        FencedCodeBlock fence => Fence(fence),
        CodeBlock code => new Code(Lines(code)),
        HtmlBlock html => new Code(Lines(html)),
        ListBlock list => new Items(ListItems(list)),
        ThematicBreakBlock => new Rule(),
        MdTable table => TableOf(table),
        DefinitionList definitions => DefinitionsOf(definitions),
        LeafBlock leaf => Leaf(leaf),
        _ => null
    };

    static Paragraph? ParagraphOf(ParagraphBlock paragraph)
    {
        var spans = Inlines(paragraph);
        return spans.Count == 0 ? null : new Paragraph(spans);
    }

    static Block Fence(FencedCodeBlock fence)
    {
        var text = Lines(fence);
        var language = fence.Info?.Trim() ?? string.Empty;
        if (!IsDiagram(language))
            return new Code(text);

        var diagram = ParseDiagram(text);
        return diagram.Nodes.Count == 0 ? new Code(text) : diagram;
    }

    static bool IsDiagram(string language)
    {
        return language.Equals("diagram", StringComparison.OrdinalIgnoreCase)
            || language.Equals("mermaid", StringComparison.OrdinalIgnoreCase)
            || language.Equals("flowchart", StringComparison.OrdinalIgnoreCase)
            || language.Equals("graph", StringComparison.OrdinalIgnoreCase);
    }

    static List<Block> AlertChildren(AlertBlock alert)
    {
        var blocks = new List<Block>
        {
            new Paragraph([new Span(AlertTitle(alert.Kind.ToString()), Style.Strong)])
        };
        blocks.AddRange(Children(alert));
        return blocks;
    }

    static string AlertTitle(string kind)
    {
        if (kind.Length == 0)
            return "Note";
        var lower = kind.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    static List<Item> ListItems(ListBlock list)
    {
        var items = new List<Item>();
        var index = 0;
        foreach (var child in list)
        {
            if (child is not ListItemBlock item)
                continue;
            var task = IsTask(item, out var isChecked);
            items.Add(new Item(Marker(list, index, task, isChecked), Children(item)));
            index++;
        }

        return items;
    }

    static bool IsTask(ListItemBlock item, out bool isChecked)
    {
        isChecked = false;
        if (item.Count == 0 || item[0] is not ParagraphBlock paragraph)
            return false;
        if (paragraph.Inline?.FirstChild is not TaskList task)
            return false;
        isChecked = task.Checked;
        return true;
    }

    static string Marker(ListBlock list, int index, bool task, bool isChecked)
    {
        if (task)
            return isChecked ? "☑ " : "☐ ";
        if (!list.IsOrdered)
            return "• ";
        var suffix = list.OrderedDelimiter == ')' ? ") " : ". ";
        return Body(list.BulletType, Start(list) + index) + suffix;
    }

    static int Start(ListBlock list)
    {
        var raw = list.OrderedStart;
        if (string.IsNullOrEmpty(raw))
            return 1;
        if (int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            return number;
        if (list.BulletType is 'a' or 'A')
            return AlphaValue(raw);
        if (list.BulletType is 'i' or 'I')
            return RomanValue(raw);
        return 1;
    }

    static string Body(char bullet, int number) => bullet switch
    {
        'a' => Alpha(number, upper: false),
        'A' => Alpha(number, upper: true),
        'i' => Roman(number, upper: false),
        'I' => Roman(number, upper: true),
        _ => number.ToString(CultureInfo.InvariantCulture)
    };

    static string Alpha(int number, bool upper)
    {
        if (number <= 0)
            return "0";
        var chars = new StringBuilder();
        while (number > 0)
        {
            number--;
            chars.Insert(0, (char)((upper ? 'A' : 'a') + (number % 26)));
            number /= 26;
        }

        return chars.ToString();
    }

    static int AlphaValue(string raw)
    {
        var total = 0;
        foreach (var character in raw)
        {
            var letter = char.ToLowerInvariant(character);
            if (letter is < 'a' or > 'z')
                return 1;
            total = (total * 26) + (letter - 'a' + 1);
        }

        return Math.Max(1, total);
    }

    static string Roman(int number, bool upper)
    {
        if (number <= 0 || number > 3999)
            return number.ToString(CultureInfo.InvariantCulture);
        var text = new StringBuilder();
        foreach (var (value, token) in RomanParts)
        {
            while (number >= value)
            {
                text.Append(token);
                number -= value;
            }
        }

        var roman = text.ToString();
        return upper ? roman : roman.ToLowerInvariant();
    }

    static readonly (int Value, string Token)[] RomanParts =
    [
        (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"),
        (100, "C"), (90, "XC"), (50, "L"), (40, "XL"),
        (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
    ];

    static int RomanValue(string raw)
    {
        var text = raw.ToUpperInvariant();
        var total = 0;
        var previous = 0;
        for (var i = text.Length - 1; i >= 0; i--)
        {
            if (!RomanDigits.TryGetValue(text[i], out var value))
                return 1;
            total += value < previous ? -value : value;
            previous = Math.Max(previous, value);
        }

        return Math.Max(1, total);
    }

    static readonly Dictionary<char, int> RomanDigits = new()
    {
        ['I'] = 1, ['V'] = 5, ['X'] = 10, ['L'] = 50, ['C'] = 100, ['D'] = 500, ['M'] = 1000
    };

    static Table TableOf(MdTable source)
    {
        var rows = new List<IReadOnlyList<List<Span>>>();
        var header = new List<bool>();
        foreach (var child in source)
        {
            if (child is not TableRow row)
                continue;
            rows.Add(Cells(row));
            header.Add(row.IsHeader);
        }

        return new Table(rows, header, Alignments(source));
    }

    static List<List<Span>> Cells(TableRow row)
    {
        var cells = new List<List<Span>>();
        foreach (var child in row)
        {
            if (child is TableCell cell)
                cells.Add(Cell(cell));
        }

        return cells;
    }

    static List<Span> Cell(TableCell cell)
    {
        var spans = new List<Span>();
        foreach (var child in cell)
            AppendCell(child, spans);
        if (spans.Count > 0 && string.Equals(spans[0].Text, "\n", StringComparison.Ordinal))
            spans.RemoveAt(0);
        return spans;
    }

    static void AppendCell(Markdig.Syntax.Block block, List<Span> spans)
    {
        if (block is LeafBlock leaf)
        {
            if (spans.Count > 0)
                spans.Add(new Span("\n", Style.None));
            Collect(leaf.Inline, Style.None, spans);
            return;
        }

        if (block is not ContainerBlock container)
            return;
        foreach (var child in container)
            AppendCell(child, spans);
    }

    static List<ColumnAlign> Alignments(MdTable source)
    {
        var align = new List<ColumnAlign>();
        foreach (var column in source.ColumnDefinitions)
        {
            align.Add(column.Alignment switch
            {
                TableColumnAlign.Center => ColumnAlign.Center,
                TableColumnAlign.Right => ColumnAlign.Right,
                _ => ColumnAlign.Left
            });
        }

        return align;
    }

    static Definitions DefinitionsOf(DefinitionList list)
    {
        var entries = new List<Defined>();
        foreach (var child in list)
        {
            if (child is DefinitionItem item)
                entries.Add(Define(item));
        }

        return new Definitions(entries);
    }

    static Defined Define(DefinitionItem item)
    {
        var term = new List<Span>();
        var body = new List<Block>();
        foreach (var child in item)
        {
            if (child is DefinitionTerm word)
            {
                AppendTerm(term, word);
                continue;
            }

            Add(child, body);
        }

        return new Defined(term, body);
    }

    static void AppendTerm(List<Span> term, DefinitionTerm word)
    {
        if (term.Count > 0)
            term.Add(new Span(", ", Style.Strong));
        foreach (var span in Inlines(word))
            term.Add(span with { Style = span.Style | Style.Strong });
    }

    static void AddNotes(FootnoteGroup group, List<Block> target)
    {
        var items = new List<Item>();
        var index = 0;
        foreach (var child in group)
        {
            if (child is not Footnote note)
                continue;
            index++;
            items.Add(new Item(NoteMarker(note, index), Children(note)));
        }

        if (items.Count == 0)
            return;
        target.Add(new Rule());
        target.Add(new Heading(2, [new Span("Notes", Style.Strong)]));
        target.Add(new Items(items));
    }

    static string NoteMarker(Footnote note, int index)
    {
        var number = index;
        if (note.Links.Count > 0 && note.Links[0].Index > 0)
            number = note.Links[0].Index;
        else if (note.Order >= 0)
            number = note.Order + 1;
        return number.ToString(CultureInfo.InvariantCulture) + ". ";
    }

    static List<Block> Children(ContainerBlock container)
    {
        var blocks = new List<Block>();
        foreach (var child in container)
            Add(child, blocks);
        return blocks;
    }

    static Code? Leaf(LeafBlock leaf)
    {
        var text = Lines(leaf);
        return text.Length == 0 ? null : new Code(text);
    }

    static string Lines(LeafBlock leaf) => leaf.Lines.ToString();

    static List<Span> Inlines(LeafBlock leaf)
    {
        var spans = new List<Span>();
        Collect(leaf.Inline, Style.None, spans);
        return spans;
    }

    static void Collect(ContainerInline? inline, Style style, List<Span> spans)
    {
        var afterTask = false;
        for (var child = inline?.FirstChild; child is not null; child = child.NextSibling)
        {
            if (child is TaskList)
            {
                afterTask = true;
                continue;
            }

            if (afterTask && child is LiteralInline literal)
            {
                var text = literal.Content.ToString();
                if (text.StartsWith(' '))
                    text = text[1..];
                AddText(spans, text, style);
                afterTask = false;
                continue;
            }

            afterTask = false;
            Append(child, style, spans);
        }
    }

    static void Append(Inline inline, Style style, List<Span> spans)
    {
        switch (inline)
        {
            case LiteralInline literal:
                AddText(spans, literal.Content.ToString(), style);
                return;
            case CodeInline code:
                AddText(spans, code.Content, style | Style.Code);
                return;
            case LineBreakInline line:
                spans.Add(new Span(line.IsHard ? "\n" : " ", style));
                return;
            case TaskList:
                return;
            case FootnoteLink note:
                AddNote(spans, note, style);
                return;
            case EmphasisInline emphasis:
                Collect(emphasis, style | EmphasisStyle(emphasis), spans);
                return;
            case MathInline math:
                AddText(spans, math.Content.ToString(), style | Style.Code);
                return;
            case LinkInline link:
                AppendLink(link, style, spans);
                return;
            case AutolinkInline auto:
                AddText(spans, auto.Url, style | Style.Link);
                return;
            case HtmlInline html:
                AddText(spans, html.Tag, style);
                return;
            case HtmlEntityInline entity:
                AddEntity(spans, entity, style);
                return;
            case AbbreviationInline abbreviation:
                AddText(spans, abbreviation.Abbreviation.Label ?? string.Empty, style | Style.Insert);
                return;
            case ContainerInline container:
                Collect(container, style, spans);
                return;
        }
    }

    static void AddNote(List<Span> spans, FootnoteLink note, Style style)
    {
        if (note.IsBackLink)
            return;
        var number = note.Index > 0 ? note.Index : Math.Max(1, note.Footnote.Order + 1);
        AddText(spans, number.ToString(CultureInfo.InvariantCulture), style | Style.Superscript);
    }

    static void AddEntity(List<Span> spans, HtmlEntityInline entity, Style style)
    {
        var text = entity.Transcoded.ToString();
        if (text.Length == 0)
            text = entity.Original.ToString();
        AddText(spans, text, style);
    }

    static void AppendLink(LinkInline link, Style style, List<Span> spans)
    {
        var next = link.IsImage ? style | Style.Emphasis : style | Style.Link;
        var count = spans.Count;
        Collect(link, next, spans);
        if (link.IsImage || spans.Count > count)
            return;
        AddText(spans, link.Url ?? string.Empty, next);
    }

    static Style EmphasisStyle(EmphasisInline emphasis)
    {
        var doubled = emphasis.DelimiterCount >= 2;
        return emphasis.DelimiterChar switch
        {
            '*' or '_' when doubled => Style.Strong,
            '*' or '_' => Style.Emphasis,
            '~' when doubled => Style.Strike,
            '~' => Style.Subscript,
            '^' => Style.Superscript,
            '+' => Style.Insert,
            '=' => Style.Mark,
            _ => Style.Emphasis
        };
    }

    static void AddText(List<Span> spans, string text, Style style)
    {
        if (text.Length == 0)
            return;
        spans.Add(new Span(text, style));
    }

    static Diagram ParseDiagram(string body)
    {
        var nodes = new List<DiagramNode>();
        var index = new Dictionary<string, int>(StringComparer.Ordinal);
        var edges = new List<DiagramEdge>();
        var direction = FlowDirection.Down;
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || SkipDiagramLine(line))
                continue;
            if (TryDirection(line, out var parsed))
            {
                direction = parsed;
                continue;
            }

            if (!line.Contains("--", StringComparison.Ordinal))
            {
                AddLooseNode(line, nodes, index);
                continue;
            }

            AddEdges(line, nodes, index, edges);
        }

        return new Diagram(direction, nodes, edges);
    }

    static bool SkipDiagramLine(string line)
        => line.StartsWith("%%", StringComparison.Ordinal)
            || line.StartsWith("classDef", StringComparison.Ordinal)
            || line.StartsWith("style ", StringComparison.Ordinal)
            || line.StartsWith("click ", StringComparison.Ordinal)
            || line.StartsWith("subgraph", StringComparison.Ordinal)
            || line.Equals("end", StringComparison.Ordinal);

    static bool TryDirection(string line, out FlowDirection direction)
    {
        var token = line;
        if (line.StartsWith("flowchart ", StringComparison.Ordinal)
            || line.StartsWith("graph ", StringComparison.Ordinal)
            || line.StartsWith("direction ", StringComparison.Ordinal))
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            token = parts[^1];
        }

        direction = token switch
        {
            "LR" => FlowDirection.Right,
            "RL" => FlowDirection.Left,
            "BT" => FlowDirection.Up,
            "TB" or "TD" => FlowDirection.Down,
            _ => FlowDirection.Down
        };
        return token is "LR" or "RL" or "TB" or "TD" or "BT";
    }

    static void AddLooseNode(string line, List<DiagramNode> nodes, Dictionary<string, int> index)
    {
        if (TryNode(line, out var id, out var label, out var shape))
            AddNode(id, label, shape, nodes, index);
    }

    static void AddEdges(
        string line,
        List<DiagramNode> nodes,
        Dictionary<string, int> index,
        List<DiagramEdge> edges)
    {
        var parts = new List<(string Text, string Via)>();
        var current = new StringBuilder();
        var via = string.Empty;
        for (var i = 0; i < line.Length; i++)
        {
            if (!IsArrow(line, i, out var length))
            {
                current.Append(line[i]);
                continue;
            }

            parts.Add((current.ToString(), via));
            current.Clear();
            via = string.Empty;
            i += length - 1;
            if (i + 1 >= line.Length || line[i + 1] != '|')
                continue;
            var end = line.IndexOf('|', i + 2);
            if (end <= i + 1)
                continue;
            via = line[(i + 2)..end].Trim();
            i = end;
        }

        parts.Add((current.ToString(), via));
        string? previous = null;
        foreach (var part in parts)
        {
            if (!TryNode(part.Text, out var id, out var label, out var shape))
            {
                previous = null;
                continue;
            }

            AddNode(id, label, shape, nodes, index);
            if (previous is not null)
                edges.Add(new DiagramEdge(previous, id, part.Via));
            previous = id;
        }
    }

    static bool IsArrow(string line, int index, out int length)
    {
        length = 0;
        if (index > 0 && line[index - 1] == '-')
            return false;
        if (index + 2 >= line.Length || line[index] != '-' || line[index + 1] != '-')
            return false;
        if (line[index + 2] == '>')
        {
            length = 3;
            return true;
        }

        if (line[index + 2] == '-' && (index + 3 >= line.Length || line[index + 3] != '-'))
        {
            length = 3;
            return true;
        }

        return false;
    }

    static void AddNode(
        string id,
        string label,
        NodeShape shape,
        List<DiagramNode> nodes,
        Dictionary<string, int> index)
    {
        if (!index.TryGetValue(id, out var at))
        {
            index[id] = nodes.Count;
            nodes.Add(new DiagramNode(id, label, shape));
            return;
        }

        var existing = nodes[at];
        var same = string.Equals(existing.Label, existing.Id, StringComparison.Ordinal);
        var nextLabel = same && label.Length > 0 ? label : existing.Label;
        var nextShape = existing.Shape == NodeShape.Box ? shape : existing.Shape;
        nodes[at] = existing with { Label = nextLabel, Shape = nextShape };
    }

    static bool TryNode(string raw, out string id, out string label, out NodeShape shape)
    {
        id = string.Empty;
        label = string.Empty;
        shape = NodeShape.Box;
        raw = raw.Trim().Trim('"');
        if (raw.Length == 0)
            return false;

        var open = raw.IndexOfAny(['[', '(', '{']);
        if (open < 0)
        {
            if (raw.Any(char.IsWhiteSpace))
                return false;
            id = raw;
            label = raw;
            return true;
        }

        var close = raw[open] switch
        {
            '[' => ']',
            '(' => ')',
            _ => '}'
        };
        var end = raw.LastIndexOf(close);
        if (end <= open)
            return false;

        id = raw[..open].Trim();
        label = raw[(open + 1)..end].Trim();
        shape = raw[open] switch
        {
            '(' => NodeShape.Round,
            '{' => NodeShape.Diamond,
            _ => NodeShape.Box
        };
        if (label.Length == 0)
            label = id;
        if (id.Length == 0)
            id = label;
        return id.Length > 0;
    }
}
