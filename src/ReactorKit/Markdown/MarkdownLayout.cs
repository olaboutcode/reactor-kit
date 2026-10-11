using SkiaSharp;
using Reactor.Kit.RichText;

namespace Reactor.Kit.Markdown;

/// <summary>Lays out parsed markdown into shaped text, tables, and diagrams.</summary>
internal sealed class MarkdownLayout : IDisposable
{
    readonly List<Drawn> _blocks = [];
    readonly Dictionary<int, TextShaper> _shapers = [];
    readonly SKTypeface _monoFace;
    readonly SKFont _mono;
    readonly bool _disposeMono;

    public float Height { get; }

    public float Width { get; }

    public MarkdownLayout(string? markdown, float width, float fontSize)
    {
        Width = Math.Max(1, width);
        fontSize = Math.Max(1, fontSize);
        (_monoFace, _disposeMono) = OpenMono();
        _mono = new SKFont(_monoFace, Math.Max(1, fontSize * 0.92f));
        var y = 0f;
        foreach (var drawn in Lay(MarkdownParser.Parse(markdown), fontSize, Width))
        {
            drawn.Y = y;
            _blocks.Add(drawn);
            y += drawn.Height + 14;
        }

        Height = _blocks.Count == 0 ? 0 : y - 14;
    }

    public void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
    {
        foreach (var block in _blocks)
            block.Draw(canvas, text, fill, stroke);
    }

    public void Dispose()
    {
        foreach (var block in _blocks)
            block.Dispose();
        _blocks.Clear();
        foreach (var shaper in _shapers.Values)
            shaper.Dispose();
        _shapers.Clear();
        _mono.Dispose();
        if (_disposeMono)
            _monoFace.Dispose();
    }

    Drawn Create(MarkdownParser.Block block, float fontSize, float width) => block switch
    {
        MarkdownParser.Heading heading => Flow(heading.Inlines, HeadingSize(fontSize, heading.Level), width, true),
        MarkdownParser.Paragraph paragraph => Flow(paragraph.Inlines, fontSize, width, false),
        MarkdownParser.Quote quote => Group(quote.Children, fontSize, width, 12, bar: true),
        MarkdownParser.Items list => new ListDrawn(ListItems(list, fontSize, width)),
        MarkdownParser.Code code => new CodeDrawn(WrapCode(code.Text, Math.Max(1, width - 16)), _mono, width),
        MarkdownParser.Rule => new RuleDrawn(width),
        MarkdownParser.Table table => BuildTable(table, fontSize, width),
        MarkdownParser.Diagram diagram => BuildDiagram(diagram, fontSize, width),
        MarkdownParser.Definitions definitions => Glossary(definitions, fontSize, width),
        _ => new RuleDrawn(0),
    };

    FlowDrawn Flow(List<MarkdownInlines.Span> spans, float size, float width, bool strong)
    {
        var flow = FlowText.Layout(spans, Shaper(size), width, 4);
        flow.Strong = strong;
        return new FlowDrawn(flow);
    }

    List<Drawn> Lay(IReadOnlyList<MarkdownParser.Block> children, float fontSize, float width)
    {
        var drawn = new List<Drawn>();
        foreach (var child in children)
        {
            var block = Create(child, fontSize, width);
            if (block.Height <= 0)
            {
                block.Dispose();
                continue;
            }

            drawn.Add(block);
        }

        return drawn;
    }

    GroupDrawn Group(IReadOnlyList<MarkdownParser.Block> children, float fontSize, float width, float indent, bool bar)
        => new(Lay(children, fontSize, Math.Max(1, width - indent)), indent, bar);

    List<ItemDrawn> ListItems(MarkdownParser.Items list, float fontSize, float width)
    {
        var shaper = Shaper(fontSize);
        var items = new List<ItemDrawn>();
        foreach (var item in list.Entries)
        {
            var marker = FlowText.Layout(item.Marker, shaper, width, 2);
            var hang = marker.Width + 4;
            items.Add(new ItemDrawn(marker, Lay(item.Children, fontSize, Math.Max(1, width - hang))));
        }

        return items;
    }

    DefinitionDrawn Glossary(MarkdownParser.Definitions definitions, float fontSize, float width)
    {
        var shaper = Shaper(fontSize);
        var entries = new List<EntryDrawn>();
        foreach (var entry in definitions.Entries)
        {
            var term = FlowText.Layout(entry.Term, shaper, width, 4);
            term.Strong = true;
            entries.Add(new EntryDrawn(term, Lay(entry.Body, fontSize, Math.Max(1, width - 16))));
        }

        return new DefinitionDrawn(entries);
    }

    TextShaper Shaper(float size)
    {
        var key = (int)Math.Round(size * 100);
        if (_shapers.TryGetValue(key, out var existing))
            return existing;
        var shaper = new TextShaper();
        shaper.Use(size);
        _shapers[key] = shaper;
        return shaper;
    }

    static float HeadingSize(float body, int level) => level switch
    {
        1 => body * 1.7f,
        2 => body * 1.42f,
        3 => body * 1.22f,
        4 => body * 1.08f,
        _ => body,
    };

    List<string> WrapCode(string text, float maxWidth)
    {
        var lines = new List<string>();
        var limit = Math.Max(1, maxWidth);
        foreach (var raw in text.Split('\n'))
            WrapCodeLine(raw, limit, lines);
        if (lines.Count == 0)
            lines.Add(string.Empty);
        return lines;
    }

    void WrapCodeLine(string raw, float limit, List<string> lines)
    {
        if (_mono.MeasureText(raw) <= limit)
        {
            lines.Add(raw);
            return;
        }

        var start = 0;
        for (var i = 1; i <= raw.Length; i++)
        {
            if (_mono.MeasureText(raw[start..i]) <= limit)
                continue;
            lines.Add(raw[start..(i - 1)]);
            start = i - 1;
        }

        lines.Add(raw[start..]);
    }

    TableDrawn BuildTable(MarkdownParser.Table table, float fontSize, float width)
    {
        var columns = 1;
        foreach (var row in table.Rows)
            columns = Math.Max(columns, row.Count);
        var shaper = Shaper(fontSize);
        var rows = TableRows(table, columns);
        var colW = ColumnWidths(rows, columns, shaper, width);
        var laid = new FlowText[rows.Count][];
        var rowH = new float[rows.Count];
        for (var r = 0; r < rows.Count; r++)
        {
            laid[r] = new FlowText[columns];
            for (var c = 0; c < columns; c++)
            {
                var inner = Math.Max(1, colW[c] - 16);
                var flow = FlowText.Layout(rows[r][c], shaper, inner, 2);
                flow.Strong = r < table.Header.Count && table.Header[r];
                laid[r][c] = flow;
                rowH[r] = Math.Max(rowH[r], flow.Height + 12);
            }
        }

        var header = new bool[rows.Count];
        for (var r = 0; r < header.Length; r++)
            header[r] = r < table.Header.Count && table.Header[r];
        return new TableDrawn(colW, laid, rowH, header, table.Align, colW.Sum());
    }

    static List<List<MarkdownInlines.Span>[]> TableRows(MarkdownParser.Table table, int columns)
    {
        var rows = new List<List<MarkdownInlines.Span>[]>();
        foreach (var row in table.Rows)
            rows.Add(FitRow(row, columns));
        return rows;
    }

    static List<MarkdownInlines.Span>[] FitRow(IReadOnlyList<List<MarkdownInlines.Span>> cells, int columns)
    {
        var row = new List<MarkdownInlines.Span>[columns];
        for (var i = 0; i < columns; i++)
            row[i] = i < cells.Count ? cells[i] : [];
        return row;
    }

    static float[] ColumnWidths(List<List<MarkdownInlines.Span>[]> rows, int columns, TextShaper shaper, float width)
    {
        var natural = new float[columns];
        for (var c = 0; c < columns; c++)
        {
            for (var r = 0; r < rows.Count; r++)
            {
                var single = FlowText.Layout(rows[r][c], shaper, width, 2);
                natural[c] = Math.Max(natural[c], single.Width + 16);
                single.Dispose();
            }

            natural[c] = Math.Max(48, natural[c]);
        }

        var sum = natural.Sum();
        var scale = sum > width ? width / sum : 1f;
        for (var c = 0; c < columns; c++)
            natural[c] *= scale;
        return natural;
    }

    Drawn BuildDiagram(MarkdownParser.Diagram diagram, float fontSize, float width)
    {
        if (diagram.Nodes.Count == 0)
            return new RuleDrawn(0);

        var ranks = Ranks(diagram);
        var horizontal = diagram.Direction is MarkdownParser.FlowDirection.Right or MarkdownParser.FlowDirection.Left;
        var upward = diagram.Direction is MarkdownParser.FlowDirection.Up or MarkdownParser.FlowDirection.Left;
        var placed = PlaceNodes(diagram, ranks, horizontal, upward, fontSize, width);
        var edges = PlaceEdges(diagram, placed, Shaper(fontSize));
        var height = 8f;
        foreach (var node in placed)
            height = Math.Max(height, node.Y + node.H + 8);
        return new DiagramDrawn(placed, edges, height);
    }

    List<PlacedNode> PlaceNodes(
        MarkdownParser.Diagram diagram,
        Dictionary<string, int> ranks,
        bool horizontal,
        bool upward,
        float fontSize,
        float width)
    {
        var shaper = Shaper(fontSize);
        var nodes = new List<PlacedNode>();
        foreach (var node in diagram.Nodes)
        {
            nodes.Add(new PlacedNode
            {
                Id = node.Id,
                Label = node.Label,
                Shape = node.Shape,
                Rank = ranks.GetValueOrDefault(node.Id)
            });
        }

        var groups = nodes.GroupBy(node => node.Rank).OrderBy(group => group.Key).ToList();
        if (upward)
            groups.Reverse();
        var slot = SlotWidth(groups.Count, horizontal, width);
        foreach (var group in groups)
            SizeGroup(group.ToList(), horizontal, slot, shaper);

        var cursor = 4f;
        foreach (var group in groups)
        {
            PositionGroup(group.ToList(), horizontal, slot, ref cursor);
            cursor += 36;
        }

        return nodes;
    }

    static float SlotWidth(int rankCount, bool horizontal, float width)
    {
        if (!horizontal)
            return width - 8;
        var gaps = 36f * Math.Max(0, rankCount - 1);
        return Math.Max(88, (width - 8 - gaps) / Math.Max(1, rankCount));
    }

    static void SizeGroup(List<PlacedNode> group, bool horizontal, float slot, TextShaper shaper)
    {
        var count = Math.Max(1, group.Count);
        var each = horizontal ? slot : Math.Max(72, (slot - (16f * (count - 1))) / count);
        foreach (var node in group)
        {
            node.Text = FlowText.Layout(node.Label, shaper, Math.Max(1, each - 28), 2);
            node.W = Math.Clamp(node.Text.Width + 28, 72, Math.Max(72, each));
            node.H = node.Text.Height + 20;
            if (node.Shape == MarkdownParser.NodeShape.Diamond)
            {
                node.W += 18;
                node.H += 12;
            }
        }
    }

    static void PositionGroup(List<PlacedNode> group, bool horizontal, float slot, ref float cursor)
    {
        var cross = 0f;
        foreach (var node in group)
        {
            if (horizontal)
            {
                node.X = cursor + Math.Max(0, (slot - node.W) / 2f);
                node.Y = cross;
                cross += node.H + 16;
            }
            else
            {
                node.X = cross;
                node.Y = cursor;
                cross += node.W + 16;
            }
        }

        if (!horizontal)
        {
            var used = cross - 16;
            var shift = Math.Max(0, (slot - used) / 2f);
            foreach (var node in group)
                node.X += shift;
        }

        cursor += horizontal ? slot : Tallest(group);
    }

    static float Tallest(List<PlacedNode> group)
    {
        var height = 0f;
        foreach (var node in group)
            height = Math.Max(height, node.H);
        return height;
    }

    static List<DiagramEdgeDraw> PlaceEdges(
        MarkdownParser.Diagram diagram,
        List<PlacedNode> nodes,
        TextShaper shaper)
    {
        var byId = new Dictionary<string, PlacedNode>(StringComparer.Ordinal);
        foreach (var node in nodes)
            byId[node.Id] = node;
        var edges = new List<DiagramEdgeDraw>();
        foreach (var edge in diagram.Edges)
        {
            if (!byId.TryGetValue(edge.From, out var from) || !byId.TryGetValue(edge.To, out var to))
                continue;
            FlowText? label = edge.Label.Length == 0
                ? null
                : FlowText.Layout(edge.Label, shaper, 160, 2);
            edges.Add(new DiagramEdgeDraw(
                Anchor(from, diagram.Direction, source: true),
                Anchor(to, diagram.Direction, source: false),
                label));
        }

        return edges;
    }

    static SKPoint Anchor(PlacedNode node, MarkdownParser.FlowDirection direction, bool source)
    {
        var midX = node.X + (node.W / 2f);
        var midY = node.Y + (node.H / 2f);
        return direction switch
        {
            MarkdownParser.FlowDirection.Right => new SKPoint(source ? node.X + node.W : node.X, midY),
            MarkdownParser.FlowDirection.Left => new SKPoint(source ? node.X : node.X + node.W, midY),
            MarkdownParser.FlowDirection.Up => new SKPoint(midX, source ? node.Y : node.Y + node.H),
            _ => new SKPoint(midX, source ? node.Y + node.H : node.Y),
        };
    }

    static Dictionary<string, int> Ranks(MarkdownParser.Diagram diagram)
    {
        var memo = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in diagram.Nodes)
            Rank(node.Id);
        return memo;

        int Rank(string id)
        {
            if (memo.TryGetValue(id, out var known))
                return known;
            if (!stack.Add(id))
                return 0;
            var best = 0;
            foreach (var edge in diagram.Edges)
            {
                if (string.Equals(edge.To, id, StringComparison.Ordinal))
                    best = Math.Max(best, Rank(edge.From) + 1);
            }

            stack.Remove(id);
            memo[id] = best;
            return best;
        }
    }

    static (SKTypeface Face, bool Dispose) OpenMono()
    {
        string[] names = ["Menlo", "Consolas", "Courier New", "DejaVu Sans Mono"];
        foreach (var name in names)
        {
            var face = SKTypeface.FromFamilyName(name);
            if (face is null)
                continue;
            if (face.FamilyName.Contains(name, StringComparison.OrdinalIgnoreCase))
                return (face, true);
            face.Dispose();
        }

        return (SKTypeface.Default, false);
    }

    sealed class PlacedNode
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public MarkdownParser.NodeShape Shape;
        public int Rank;
        public float X;
        public float Y;
        public float W;
        public float H;
        public FlowText? Text;
    }

    sealed class DiagramEdgeDraw(SKPoint from, SKPoint to, FlowText? label)
    {
        public SKPoint From { get; } = from;
        public SKPoint To { get; } = to;
        public FlowText? Label { get; } = label;
    }

    abstract class Drawn : IDisposable
    {
        public float Y { get; set; }

        public abstract float Height { get; }

        public abstract void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke);

        public abstract void Dispose();
    }

    sealed class FlowDrawn(FlowText flow) : Drawn
    {
        public override float Height => flow.Height;

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
            => flow.Draw(canvas, 0, Y, text, fill);

        public override void Dispose() => flow.Dispose();
    }

    static float StackHeight(List<Drawn> children)
    {
        if (children.Count == 0)
            return 0;
        var height = 0f;
        foreach (var child in children)
            height += child.Height;
        return height + (8f * (children.Count - 1));
    }

    static void DrawStack(
        SKCanvas canvas,
        List<Drawn> children,
        float x,
        float y,
        SKPaint text,
        SKPaint fill,
        SKPaint stroke)
    {
        canvas.Save();
        canvas.Translate(x, 0);
        foreach (var child in children)
        {
            child.Y = y;
            child.Draw(canvas, text, fill, stroke);
            y += child.Height + 8;
        }

        canvas.Restore();
    }

    sealed class GroupDrawn(List<Drawn> children, float indent, bool bar) : Drawn
    {
        public override float Height => StackHeight(children);

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            if (bar)
            {
                stroke.StrokeWidth = 3;
                canvas.DrawLine(1, Y, 1, Y + Height, stroke);
                stroke.StrokeWidth = 1;
            }

            DrawStack(canvas, children, indent, Y, text, fill, stroke);
        }

        public override void Dispose()
        {
            foreach (var child in children)
                child.Dispose();
        }
    }

    sealed class ItemDrawn(FlowText marker, List<Drawn> body)
    {
        public FlowText Marker { get; } = marker;

        public List<Drawn> Body { get; } = body;

        public float Hang => Marker.Width + 4;

        public float Height => Math.Max(Marker.Height, StackHeight(Body));

        public void Dispose()
        {
            Marker.Dispose();
            foreach (var child in Body)
                child.Dispose();
        }
    }

    sealed class ListDrawn(List<ItemDrawn> items) : Drawn
    {
        public override float Height
        {
            get
            {
                var height = 0f;
                foreach (var item in items)
                    height += item.Height + 6;
                return items.Count == 0 ? 0 : height - 6;
            }
        }

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            var y = Y;
            foreach (var item in items)
            {
                item.Marker.Draw(canvas, 0, y, text, fill);
                DrawStack(canvas, item.Body, item.Hang, y, text, fill, stroke);
                y += item.Height + 6;
            }
        }

        public override void Dispose()
        {
            foreach (var item in items)
                item.Dispose();
        }
    }

    sealed class EntryDrawn(FlowText term, List<Drawn> body)
    {
        public FlowText Term { get; } = term;

        public List<Drawn> Body { get; } = body;

        public void Dispose()
        {
            Term.Dispose();
            foreach (var child in Body)
                child.Dispose();
        }
    }

    sealed class DefinitionDrawn(List<EntryDrawn> entries) : Drawn
    {
        public override float Height
        {
            get
            {
                if (entries.Count == 0)
                    return 0;
                var height = 0f;
                foreach (var entry in entries)
                    height += entry.Term.Height + 4 + StackHeight(entry.Body) + 10;
                return height - 10;
            }
        }

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            var y = Y;
            foreach (var entry in entries)
            {
                entry.Term.Draw(canvas, 0, y, text, fill);
                y += entry.Term.Height + 4;
                DrawStack(canvas, entry.Body, 16, y, text, fill, stroke);
                y += StackHeight(entry.Body) + 10;
            }
        }

        public override void Dispose()
        {
            foreach (var entry in entries)
                entry.Dispose();
        }
    }

    sealed class CodeDrawn(List<string> lines, SKFont font, float width) : Drawn
    {
        public override float Height => (lines.Count * (font.Size * 1.35f)) + 16;

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            canvas.DrawRoundRect(new SKRect(0, Y, width, Y + Height), 6, 6, fill);
            var baseline = Y + 8 + (font.Size * 0.8f);
            foreach (var line in lines)
            {
                canvas.DrawText(line, 8, baseline, SKTextAlign.Left, font, text);
                baseline += font.Size * 1.35f;
            }
        }

        public override void Dispose()
        {
        }
    }

    sealed class RuleDrawn(float width) : Drawn
    {
        public override float Height => width <= 0 ? 0 : 12;

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            if (width <= 0)
                return;
            canvas.DrawLine(0, Y + 6, width, Y + 6, stroke);
        }

        public override void Dispose()
        {
        }
    }

    sealed class TableDrawn(
        float[] columns,
        FlowText[][] cells,
        float[] rowHeights,
        bool[] header,
        IReadOnlyList<MarkdownParser.ColumnAlign> align,
        float tableWidth) : Drawn
    {
        public override float Height
        {
            get
            {
                var height = 0f;
                foreach (var row in rowHeights)
                    height += row;
                return height;
            }
        }

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            var y = Y;
            for (var r = 0; r < cells.Length; r++)
            {
                if (r < header.Length && header[r])
                    canvas.DrawRect(0, y, tableWidth, rowHeights[r], fill);
                DrawRow(canvas, text, fill, r, y);
                y += rowHeights[r];
            }

            DrawGrid(canvas, stroke);
        }

        void DrawRow(SKCanvas canvas, SKPaint text, SKPaint fill, int row, float y)
        {
            var x = 0f;
            for (var c = 0; c < cells[row].Length; c++)
            {
                var flow = cells[row][c];
                var left = Left(c, x, flow.Width);
                flow.Draw(canvas, left, y + 6, text, fill);
                x += columns[c];
            }
        }

        float Left(int column, float x, float contentWidth)
        {
            var mode = column < align.Count ? align[column] : MarkdownParser.ColumnAlign.Left;
            if (mode == MarkdownParser.ColumnAlign.Center)
                return x + Math.Max(8, (columns[column] - contentWidth) / 2f);
            if (mode == MarkdownParser.ColumnAlign.Right)
                return x + Math.Max(8, columns[column] - contentWidth - 8);
            return x + 8;
        }

        void DrawGrid(SKCanvas canvas, SKPaint stroke)
        {
            var y = Y;
            canvas.DrawLine(0, y, tableWidth, y, stroke);
            foreach (var row in rowHeights)
            {
                y += row;
                canvas.DrawLine(0, y, tableWidth, y, stroke);
            }

            var x = 0f;
            canvas.DrawLine(x, Y, x, Y + Height, stroke);
            foreach (var column in columns)
            {
                x += column;
                canvas.DrawLine(x, Y, x, Y + Height, stroke);
            }
        }

        public override void Dispose()
        {
            foreach (var row in cells)
            {
                foreach (var cell in row)
                    cell.Dispose();
            }
        }
    }

    sealed class DiagramDrawn(List<PlacedNode> nodes, List<DiagramEdgeDraw> edges, float height) : Drawn
    {
        public override float Height => height;

        public override void Draw(SKCanvas canvas, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            foreach (var edge in edges)
                DrawEdge(canvas, edge, text, fill, stroke);
            foreach (var node in nodes)
                DrawNode(canvas, node, text, fill, stroke);
        }

        void DrawEdge(SKCanvas canvas, DiagramEdgeDraw edge, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            var from = new SKPoint(edge.From.X, edge.From.Y + Y);
            var to = new SKPoint(edge.To.X, edge.To.Y + Y);
            Arrow(canvas, from, to, stroke);
            if (edge.Label is not { } label)
                return;
            var midX = ((from.X + to.X) / 2f) - (label.Width / 2f);
            var midY = ((from.Y + to.Y) / 2f) - label.Height - 2;
            label.Draw(canvas, midX, midY, text, fill);
        }

        static void Arrow(SKCanvas canvas, SKPoint from, SKPoint to, SKPaint stroke)
        {
            var angle = Math.Atan2(to.Y - from.Y, to.X - from.X);
            var head = 9f;
            var end = new SKPoint(
                to.X - (head * (float)Math.Cos(angle)),
                to.Y - (head * (float)Math.Sin(angle)));
            canvas.DrawLine(from, end, stroke);
            using var path = Head(to, angle, head);
            var style = stroke.Style;
            stroke.Style = SKPaintStyle.Fill;
            canvas.DrawPath(path, stroke);
            stroke.Style = style;
        }

        static SKPath Head(SKPoint to, double angle, float head)
        {
            var builder = new SKPathBuilder();
            builder.MoveTo(to);
            builder.LineTo(
                to.X - (head * (float)Math.Cos(angle - 0.4)),
                to.Y - (head * (float)Math.Sin(angle - 0.4)));
            builder.LineTo(
                to.X - (head * (float)Math.Cos(angle + 0.4)),
                to.Y - (head * (float)Math.Sin(angle + 0.4)));
            builder.Close();
            return builder.Snapshot();
        }

        void DrawNode(SKCanvas canvas, PlacedNode node, SKPaint text, SKPaint fill, SKPaint stroke)
        {
            var rect = new SKRect(node.X, Y + node.Y, node.X + node.W, Y + node.Y + node.H);
            if (node.Shape == MarkdownParser.NodeShape.Diamond)
                DrawDiamond(canvas, rect, fill, stroke);
            else if (node.Shape == MarkdownParser.NodeShape.Round)
                canvas.DrawRoundRect(rect, 14, 14, fill);
            else
                canvas.DrawRoundRect(rect, 6, 6, fill);

            if (node.Shape == MarkdownParser.NodeShape.Round)
                canvas.DrawRoundRect(rect, 14, 14, stroke);
            else if (node.Shape != MarkdownParser.NodeShape.Diamond)
                canvas.DrawRoundRect(rect, 6, 6, stroke);

            var label = node.Text;
            if (label is null)
                return;
            var left = rect.Left + ((rect.Width - label.Width) / 2f);
            var top = rect.Top + ((rect.Height - label.Height) / 2f);
            label.Draw(canvas, left, top, text, fill);
        }

        static void DrawDiamond(SKCanvas canvas, SKRect rect, SKPaint fill, SKPaint stroke)
        {
            var builder = new SKPathBuilder();
            builder.MoveTo(rect.MidX, rect.Top);
            builder.LineTo(rect.Right, rect.MidY);
            builder.LineTo(rect.MidX, rect.Bottom);
            builder.LineTo(rect.Left, rect.MidY);
            builder.Close();
            using var path = builder.Snapshot();
            canvas.DrawPath(path, fill);
            canvas.DrawPath(path, stroke);
        }

        public override void Dispose()
        {
            foreach (var node in nodes)
                node.Text?.Dispose();
            foreach (var edge in edges)
                edge.Label?.Dispose();
        }
    }
}
