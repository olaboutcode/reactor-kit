using SkiaSharp;
using Reactor.Kit.Internals.RichText;

namespace Reactor.Kit.Internals.Markdown;

/// <summary>Wrapped, shaped runs for one markdown span sequence.</summary>
internal sealed class FlowText : IDisposable
{
    readonly List<Piece> _pieces = [];

    public float Width { get; private set; }

    public float Height { get; private set; }

    public bool Strong { get; set; }

    public static FlowText Layout(string? text, TextShaper shaper, float maxWidth, float spacing)
    {
        var spans = new List<MarkdownInlines.Span>();
        if (!string.IsNullOrEmpty(text))
            spans.Add(new MarkdownInlines.Span(text, MarkdownInlines.Style.None));
        return Layout(spans, shaper, maxWidth, spacing);
    }

    public static FlowText Layout(List<MarkdownInlines.Span> spans, TextShaper shaper, float maxWidth, float spacing)
    {
        var flow = new FlowText();
        maxWidth = Math.Max(1, maxWidth);
        var space = SpaceWidth(shaper);
        var box = LineBox(shaper, out var baselineOffset);
        var x = 0f;
        var y = 0f;
        var line = new List<Piece>();
        var open = false;
        foreach (var span in spans)
            flow.PlaceSpan(span, shaper, maxWidth, space, box, baselineOffset, spacing, line, ref x, ref y, ref open);
        flow.Flush(line, box, baselineOffset, spacing, ref y);
        flow.Height = y > 0 ? y : box;
        return flow;
    }

    public void Draw(SKCanvas canvas, float left, float top, SKPaint text, SKPaint fill)
    {
        foreach (var piece in _pieces)
            piece.Draw(canvas, left, top, text, fill, Strong);
    }

    public void Dispose()
    {
        foreach (var piece in _pieces)
            piece.Dispose();
        _pieces.Clear();
    }

    void PlaceSpan(
        MarkdownInlines.Span span,
        TextShaper shaper,
        float maxWidth,
        float space,
        float box,
        float baselineOffset,
        float spacing,
        List<Piece> line,
        ref float x,
        ref float y,
        ref bool open)
    {
        foreach (var token in Tokens(span.Text))
        {
            if (string.Equals(token, "\n", StringComparison.Ordinal))
            {
                Flush(line, box, baselineOffset, spacing, ref y);
                x = 0;
                open = false;
                continue;
            }

            if (string.Equals(token, " ", StringComparison.Ordinal))
            {
                if (open && x + space <= maxWidth)
                    x += space;
                continue;
            }

            var runs = shaper.Shape(token);
            var width = RunWidth(runs);
            if (open && x + width > maxWidth)
            {
                Flush(line, box, baselineOffset, spacing, ref y);
                x = 0;
                open = false;
            }

            line.Add(new Piece(x, width, span.Style, runs));
            x += width;
            open = true;
        }
    }

    void Flush(List<Piece> line, float box, float baselineOffset, float spacing, ref float y)
    {
        if (line.Count == 0)
            return;
        if (_pieces.Count > 0)
            y += spacing;
        foreach (var piece in line)
        {
            piece.Baseline = y + baselineOffset;
            piece.LineTop = y;
            piece.LineBottom = y + box;
            Width = Math.Max(Width, piece.X + piece.Width);
        }

        _pieces.AddRange(line);
        line.Clear();
        y += box;
    }

    static IEnumerable<string> Tokens(string text)
    {
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            var boundary = i == text.Length || text[i] is ' ' or '\n' or '\t';
            if (!boundary)
                continue;
            if (i > start)
                yield return text[start..i];
            if (i < text.Length && text[i] == '\n')
                yield return "\n";
            else if (i < text.Length)
                yield return " ";
            start = i + 1;
        }
    }

    static float LineBox(TextShaper shaper, out float baselineOffset)
    {
        var probe = shaper.Shape("M");
        if (probe.Length == 0)
        {
            baselineOffset = 0;
            return 1;
        }

        var metrics = probe[0].Font.Metrics;
        var size = probe[0].Font.Size;
        DisposeRuns(probe);
        var em = Math.Max(1f, metrics.Descent - metrics.Ascent);
        var box = Math.Max(em, size * 1.125f);
        baselineOffset = ((box - em) / 2f) - metrics.Ascent;
        return box;
    }

    static float SpaceWidth(TextShaper shaper)
    {
        var probe = shaper.Shape(" ");
        var width = Math.Max(1, RunWidth(probe));
        DisposeRuns(probe);
        return width;
    }

    static float RunWidth(GlyphRun[] runs)
    {
        var width = 0f;
        for (var i = 0; i < runs.Length; i++)
            width += runs[i].Width;
        return width;
    }

    static void DisposeRuns(GlyphRun[] runs)
    {
        for (var i = 0; i < runs.Length; i++)
            runs[i].Dispose();
    }

    sealed class Piece(float x, float width, MarkdownInlines.Style style, GlyphRun[] runs) : IDisposable
    {
        public float X { get; } = x;

        public float Width { get; } = width;

        public float Baseline { get; set; }

        public float LineTop { get; set; }

        public float LineBottom { get; set; }

        public void Draw(SKCanvas canvas, float left, float top, SKPaint text, SKPaint fill, bool strong)
        {
            var origin = left + X;
            var baseline = top + Baseline;
            if (Has(MarkdownInlines.Style.Code) || Has(MarkdownInlines.Style.Mark))
            {
                var rect = new SKRect(origin - 2, top + LineTop, origin + Width + 2, top + LineBottom);
                canvas.DrawRoundRect(rect, 3, 3, fill);
            }

            var bold = strong || Has(MarkdownInlines.Style.Strong);
            var sub = Has(MarkdownInlines.Style.Subscript);
            var sup = Has(MarkdownInlines.Style.Superscript);
            if (sub || sup || Has(MarkdownInlines.Style.Emphasis))
            {
                canvas.Save();
                canvas.Translate(origin, baseline);
                Shift(canvas, sub, sup);
                if (Has(MarkdownInlines.Style.Emphasis))
                    canvas.Skew(-0.22f, 0);
                DrawRuns(canvas, 0, 0, text, bold);
                Decorate(canvas, 0, 0, text);
                canvas.Restore();
                return;
            }

            DrawRuns(canvas, origin, baseline, text, bold);
            Decorate(canvas, origin, baseline, text);
        }

        bool Has(MarkdownInlines.Style flag) => (style & flag) != MarkdownInlines.Style.None;

        void Shift(SKCanvas canvas, bool sub, bool sup)
        {
            var box = LineBottom - LineTop;
            if (sup)
                canvas.Translate(0, -box * 0.35f);
            if (sub)
                canvas.Translate(0, box * 0.2f);
            if (sub || sup)
                canvas.Scale(0.72f);
        }

        void Decorate(SKCanvas canvas, float x, float baseline, SKPaint text)
        {
            var strike = Has(MarkdownInlines.Style.Strike);
            var underline = Has(MarkdownInlines.Style.Link) || Has(MarkdownInlines.Style.Insert);
            if (!strike && !underline)
                return;

            var paintStyle = text.Style;
            var weight = text.StrokeWidth;
            text.Style = SKPaintStyle.Stroke;
            text.StrokeWidth = 1;
            if (strike)
            {
                var rise = (LineBottom - LineTop) * 0.28f;
                canvas.DrawLine(x, baseline - rise, x + Width, baseline - rise, text);
            }

            if (underline)
                canvas.DrawLine(x, baseline + 2, x + Width, baseline + 2, text);
            text.Style = paintStyle;
            text.StrokeWidth = weight;
        }

        void DrawRuns(SKCanvas canvas, float x, float baseline, SKPaint text, bool strong)
        {
            foreach (var run in runs)
            {
                run.Draw(canvas, x, baseline, text);
                if (strong && run.Blob is not null)
                    run.Draw(canvas, x + 0.6f, baseline, text);
                x += run.Width;
            }
        }

        public void Dispose()
        {
            for (var i = 0; i < runs.Length; i++)
                runs[i].Dispose();
        }
    }
}
