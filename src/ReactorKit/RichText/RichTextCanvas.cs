using System.Runtime.InteropServices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Reactor.Kit.RichText;

/// <summary>
/// Paragraph text drawn with SkiaSharp. Highlights are rectangles behind words.
/// A highlight change repaints. It does not measure the text again.
/// </summary>
internal sealed class RichTextCanvas : SKCanvasView, IDisposable
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(
        nameof(Text), typeof(string), typeof(RichTextCanvas), string.Empty, propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(RichTextCanvas), 17d, propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty LineSpacingProperty = BindableProperty.Create(
        nameof(LineSpacing), typeof(double), typeof(RichTextCanvas), 4d, propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
        nameof(TextColor), typeof(Color), typeof(RichTextCanvas), Colors.Black, propertyChanged: OnPaintPropertyChanged);

    public static readonly BindableProperty HighlightColorProperty = BindableProperty.Create(
        nameof(HighlightColor), typeof(Color), typeof(RichTextCanvas), Color.FromRgba(245, 196, 40, 120), propertyChanged: OnPaintPropertyChanged);

    public static readonly BindableProperty ToggleHighlightOnTapProperty = BindableProperty.Create(
        nameof(ToggleHighlightOnTap), typeof(bool), typeof(RichTextCanvas), true);

    public static readonly BindableProperty TapTargetProperty = BindableProperty.Create(
        nameof(TapTarget), typeof(TextTapTarget), typeof(RichTextCanvas), TextTapTarget.Word);

    public static readonly BindableProperty HighlightedWordsProperty = BindableProperty.Create(
        nameof(HighlightedWords), typeof(IReadOnlyList<int>), typeof(RichTextCanvas), null, propertyChanged: OnHighlightsChanged);

    public static readonly BindableProperty HighlightedSentencesProperty = BindableProperty.Create(
        nameof(HighlightedSentences), typeof(IReadOnlyList<int>), typeof(RichTextCanvas), null, propertyChanged: OnHighlightsChanged);

    public static readonly BindableProperty HighlightedRangesProperty = BindableProperty.Create(
        nameof(HighlightedRanges), typeof(IReadOnlyList<TextRange>), typeof(RichTextCanvas), null, propertyChanged: OnHighlightsChanged);

    const float HitSlop = 4f;

    /// <summary>Highlight box is at least this multiple of the font size. 16 lands on 18.</summary>
    const float LineBoxScale = 1.125f;

    readonly Dictionary<int, object?> _tags = [];
    Word[] _words = [];
    Sentence[] _sentences = [];
    bool[] _highlighted = [];
    SKFont? _font;
    SKPaint? _textPaint;
    SKPaint? _highlightPaint;
    float _laidWidth = -1;
    float _contentWidth;
    float _contentHeight;
    bool _layoutDirty = true;
    bool _disposed;
    int _pressedWord = -1;

    public RichTextCanvas()
    {
        IgnorePixelScaling = true;
        EnableTouchEvents = true;
        BackgroundColor = Colors.Transparent;
        PaintSurface += OnPaintSurface;
        Touch += OnTouch;
    }

    /// <summary>Raised after a tap on a word. The hit is the word, or its sentence when <see cref="TapTarget"/> is <see cref="TextTapTarget.Sentence"/>.</summary>
    public event EventHandler<TextTokenHit>? TokenTapped;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>Extra pixels between lines.</summary>
    public double LineSpacing
    {
        get => (double)GetValue(LineSpacingProperty);
        set => SetValue(LineSpacingProperty, value);
    }

    public Color TextColor
    {
        get => (Color)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public Color HighlightColor
    {
        get => (Color)GetValue(HighlightColorProperty);
        set => SetValue(HighlightColorProperty, value);
    }

    /// <summary>When true, a tap toggles the highlight of the word or sentence. The tap event still runs.</summary>
    public bool ToggleHighlightOnTap
    {
        get => (bool)GetValue(ToggleHighlightOnTapProperty);
        set => SetValue(ToggleHighlightOnTapProperty, value);
    }

    public TextTapTarget TapTarget
    {
        get => (TextTapTarget)GetValue(TapTargetProperty);
        set => SetValue(TapTargetProperty, value);
    }

    public IReadOnlyList<int>? HighlightedWords
    {
        get => (IReadOnlyList<int>?)GetValue(HighlightedWordsProperty);
        set => SetValue(HighlightedWordsProperty, value);
    }

    public IReadOnlyList<int>? HighlightedSentences
    {
        get => (IReadOnlyList<int>?)GetValue(HighlightedSentencesProperty);
        set => SetValue(HighlightedSentencesProperty, value);
    }

    public IReadOnlyList<TextRange>? HighlightedRanges
    {
        get => (IReadOnlyList<TextRange>?)GetValue(HighlightedRangesProperty);
        set => SetValue(HighlightedRangesProperty, value);
    }

    public void HighlightWord(int wordIndex)
        => SetHighlighted(wordIndex, true);

    public void HighlightSentence(int sentenceIndex)
    {
        if ((uint)sentenceIndex >= (uint)_sentences.Length)
            return;
        var sentence = _sentences[sentenceIndex];
        for (var i = 0; i < sentence.WordCount; i++)
            _highlighted[sentence.FirstWord + i] = true;
        InvalidateSurface();
    }

    public void HighlightRange(int start, int length)
    {
        if (length <= 0)
            return;
        MarkRange(start, start + length, true);
        InvalidateSurface();
    }

    public void ClearHighlights()
    {
        Array.Clear(_highlighted);
        InvalidateSurface();
    }

    public void SetWordTag(int wordIndex, object? tag)
    {
        if ((uint)wordIndex >= (uint)_words.Length)
            return;
        if (tag is null)
            _tags.Remove(wordIndex);
        else
            _tags[wordIndex] = tag;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        PaintSurface -= OnPaintSurface;
        Touch -= OnTouch;
        DisposeSkia();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
            DisposeSkia();
    }

    protected override Size MeasureOverride(double widthConstraint, double heightConstraint)
    {
        var width = double.IsFinite(widthConstraint) && widthConstraint > 0 ? widthConstraint : 10_000d;
        LayoutIfNeeded((float)width);
        var measuredWidth = double.IsFinite(widthConstraint) ? width : Math.Max(1, _contentWidth);
        return new Size(measuredWidth, Math.Max(1, _contentHeight));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || Math.Abs(width - _laidWidth) < 0.5)
            return;
        LayoutIfNeeded((float)width);
        InvalidateSurface();
    }

    static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((RichTextCanvas)bindable).InvalidateText();

    static void OnPaintPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        => ((RichTextCanvas)bindable).InvalidateSurface();

    static void OnHighlightsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (SameList<int>(oldValue, newValue) || SameList<TextRange>(oldValue, newValue))
            return;
        ((RichTextCanvas)bindable).ApplyDeclarativeHighlights(repaint: true);
    }

    static bool SameList<T>(object? oldValue, object? newValue)
        where T : IEquatable<T>
    {
        if (ReferenceEquals(oldValue, newValue))
            return true;
        if (oldValue is not IReadOnlyList<T> oldList || newValue is not IReadOnlyList<T> newList || oldList.Count != newList.Count)
            return false;
        for (var i = 0; i < oldList.Count; i++)
        {
            if (!oldList[i].Equals(newList[i]))
                return false;
        }

        return true;
    }

    void InvalidateText()
    {
        _layoutDirty = true;
        InvalidateMeasure();
        var width = Width > 0 ? Width : _laidWidth;
        if (width > 0)
            LayoutIfNeeded((float)width);
        InvalidateSurface();
    }

    void LayoutIfNeeded(float width)
    {
        if (!_layoutDirty && Math.Abs(width - _laidWidth) < 0.5f)
            return;
        LayoutText(width);
    }

    void LayoutText(float width)
    {
        EnsureFont();
        var font = _font!;
        var metrics = font.Metrics;
        var em = Math.Max(1f, metrics.Descent - metrics.Ascent);
        var box = Math.Max(em, font.Size * LineBoxScale);
        var spacing = (float)Math.Max(0, LineSpacing);
        var pad = 2f;
        var placed = Place(Split(Text ?? string.Empty), font, metrics, box, spacing, font.MeasureText(" "), pad, Math.Max(1, width - (pad * 2)));

        _words = placed.Words;
        _sentences = placed.Sentences;
        _highlighted = new bool[placed.Words.Length];
        _laidWidth = width;
        _contentWidth = placed.ContentWidth;
        _contentHeight = placed.ContentHeight;
        _layoutDirty = false;
        _pressedWord = -1;
        PruneTags();
        ApplyDeclarativeHighlights(repaint: false);
    }

    static PlacedText Place(List<RawWord> raw, SKFont font, SKFontMetrics metrics, float box, float spacing, float space, float pad, float max)
    {
        var words = new Word[raw.Count];
        var x = pad;
        var lineTop = pad;
        var contentWidth = pad;
        var sentenceStart = 0;
        var sentences = new List<Sentence>();
        var em = metrics.Descent - metrics.Ascent;
        var baselineOffset = ((box - em) / 2f) - metrics.Ascent;

        for (var i = 0; i < raw.Count; i++)
        {
            var piece = raw[i];
            var measured = font.MeasureText(piece.Text);
            var breakBefore = i > 0 && raw[i - 1].BreakAfter;
            if (breakBefore || (x > pad && x + space + measured > pad + max))
            {
                x = pad;
                lineTop += box + spacing;
            }
            else if (x > pad)
            {
                x += space;
            }

            words[i] = new Word(piece.Text, piece.Start, piece.End, piece.SentenceIndex, x, lineTop, measured, box, lineTop + baselineOffset);
            x += measured;
            contentWidth = Math.Max(contentWidth, x + pad);
            if (ClosesSentence(raw, i))
            {
                var first = words[sentenceStart];
                sentences.Add(new Sentence(piece.SentenceIndex, first.Start, words[i].End, sentenceStart, i - sentenceStart + 1));
                sentenceStart = i + 1;
            }
        }

        var contentHeight = words.Length == 0 ? pad * 2 : lineTop + box + pad;
        if (words.Length == 0)
            contentWidth = pad * 2;
        return new PlacedText(words, [.. sentences], contentWidth, contentHeight);
    }

    static bool ClosesSentence(List<RawWord> raw, int index)
    {
        var piece = raw[index];
        if (piece.EndsSentence || index == raw.Count - 1)
            return true;
        return raw[index + 1].SentenceIndex != piece.SentenceIndex;
    }

    void ApplyDeclarativeHighlights(bool repaint)
    {
        if (_highlighted.Length != _words.Length)
            return;
        Array.Clear(_highlighted);
        MarkIndexes(HighlightedWords, word: true);
        MarkIndexes(HighlightedSentences, word: false);
        var ranges = HighlightedRanges;
        if (ranges is not null)
        {
            for (var i = 0; i < ranges.Count; i++)
            {
                var range = ranges[i];
                if (range.Length > 0)
                    MarkRange(range.Start, range.Start + range.Length, true);
            }
        }

        if (repaint)
            InvalidateSurface();
    }

    void MarkIndexes(IReadOnlyList<int>? indexes, bool word)
    {
        if (indexes is null)
            return;
        for (var i = 0; i < indexes.Count; i++)
        {
            var index = indexes[i];
            if (word)
            {
                SetHighlighted(index, true, repaint: false);
            }
            else if ((uint)index < (uint)_sentences.Length)
            {
                var sentence = _sentences[index];
                for (var w = 0; w < sentence.WordCount; w++)
                    _highlighted[sentence.FirstWord + w] = true;
            }
        }
    }

    void MarkRange(int start, int end, bool highlighted)
    {
        for (var i = 0; i < _words.Length; i++)
        {
            var word = _words[i];
            if (word.End > start && word.Start < end)
                _highlighted[i] = highlighted;
        }
    }

    void SetHighlighted(int wordIndex, bool highlighted, bool repaint = true)
    {
        if ((uint)wordIndex >= (uint)_highlighted.Length)
            return;
        _highlighted[wordIndex] = highlighted;
        if (repaint)
            InvalidateSurface();
    }

    void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_words.Length == 0)
            return;

        EnsureFont();
        _highlightPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _textPaint ??= new SKPaint { IsAntialias = true };
        _highlightPaint.Color = ToSk(HighlightColor);
        _textPaint.Color = ToSk(TextColor);

        DrawHighlights(canvas);
        var font = _font!;
        var paint = _textPaint;
        for (var i = 0; i < _words.Length; i++)
        {
            var word = _words[i];
            canvas.DrawText(word.Text, word.X, word.Baseline, SKTextAlign.Left, font, paint);
        }
    }

    void DrawHighlights(SKCanvas canvas)
    {
        var paint = _highlightPaint!;
        var i = 0;
        while (i < _words.Length)
        {
            if (!_highlighted[i])
            {
                i++;
                continue;
            }

            var top = _words[i].LineTop;
            var left = _words[i].X;
            var right = _words[i].Right;
            var bottom = _words[i].LineBottom;
            var j = i + 1;
            while (j < _words.Length && _highlighted[j] && Math.Abs(_words[j].LineTop - top) < 0.5f)
            {
                right = _words[j].Right;
                j++;
            }

            canvas.DrawRoundRect(new SKRect(left - 2, top, right + 2, bottom), 4, 4, paint);
            i = j;
        }
    }

    void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            _pressedWord = HitWord(e.Location);
            // iOS ignores the rest of the touch unless the press is handled.
            e.Handled = _pressedWord >= 0;
            return;
        }

        if (e.ActionType == SKTouchAction.Moved)
        {
            e.Handled = _pressedWord >= 0;
            return;
        }

        if (e.ActionType is SKTouchAction.Cancelled or SKTouchAction.Exited)
        {
            _pressedWord = -1;
            return;
        }

        if (e.ActionType != SKTouchAction.Released)
            return;

        var pressed = _pressedWord;
        _pressedWord = -1;
        if (pressed < 0 || !Hits(pressed, e.Location))
            return;

        e.Handled = true;
        Activate(pressed);
    }

    void Activate(int wordIndex)
    {
        var sentence = _sentences.FirstOrDefault(item => wordIndex >= item.FirstWord && wordIndex < item.FirstWord + item.WordCount);
        var sentenceIndex = sentence.WordCount == 0 ? _words[wordIndex].SentenceIndex : sentence.Index;
        if (ToggleHighlightOnTap)
        {
            if (TapTarget == TextTapTarget.Sentence && (uint)sentenceIndex < (uint)_sentences.Length)
                ToggleSentence(sentenceIndex);
            else
                _highlighted[wordIndex] = !_highlighted[wordIndex];
            InvalidateSurface();
        }

        TokenTapped?.Invoke(this, CreateHit(wordIndex, sentenceIndex));
    }

    void ToggleSentence(int sentenceIndex)
    {
        var sentence = _sentences[sentenceIndex];
        var on = false;
        for (var i = 0; i < sentence.WordCount; i++)
            on |= _highlighted[sentence.FirstWord + i];
        for (var i = 0; i < sentence.WordCount; i++)
            _highlighted[sentence.FirstWord + i] = !on;
    }

    TextTokenHit CreateHit(int wordIndex, int sentenceIndex)
    {
        var word = _words[wordIndex];
        _tags.TryGetValue(wordIndex, out var tag);
        var bounds = new Rect(word.X, word.LineTop, word.Width, word.LineHeight);
        if (TapTarget == TextTapTarget.Word || (uint)sentenceIndex >= (uint)_sentences.Length)
        {
            return new TextTokenHit(word.Text, word.Start, word.End, wordIndex, sentenceIndex, TextTokenKind.Word, tag, bounds);
        }

        var sentence = _sentences[sentenceIndex];
        var text = Text ?? string.Empty;
        var slice = text.Substring(sentence.Start, Math.Max(0, sentence.End - sentence.Start));
        return new TextTokenHit(slice, sentence.Start, sentence.End, wordIndex, sentenceIndex, TextTokenKind.Sentence, tag, bounds);
    }

    int HitWord(SKPoint point)
    {
        for (var i = 0; i < _words.Length; i++)
        {
            if (Hits(i, point))
                return i;
        }

        return -1;
    }

    bool Hits(int wordIndex, SKPoint point)
    {
        var word = _words[wordIndex];
        return point.X >= word.X - HitSlop && point.X <= word.Right + HitSlop
            && point.Y >= word.LineTop - HitSlop && point.Y <= word.LineBottom + HitSlop;
    }

    void EnsureFont()
    {
        var size = (float)Math.Max(1, FontSize);
        if (_font is not null && Math.Abs(_font.Size - size) < 0.01f)
            return;
        _font?.Dispose();
        _font = new SKFont(SKTypeface.Default, size);
    }

    void DisposeSkia()
    {
        _font?.Dispose();
        _textPaint?.Dispose();
        _highlightPaint?.Dispose();
        _font = null;
        _textPaint = null;
        _highlightPaint = null;
    }

    void PruneTags()
    {
        if (_tags.Count == 0)
            return;
        List<int>? stale = null;
        foreach (var index in _tags.Keys)
        {
            if ((uint)index >= (uint)_words.Length)
                (stale ??= []).Add(index);
        }

        if (stale is null)
            return;
        for (var i = 0; i < stale.Count; i++)
            _tags.Remove(stale[i]);
    }

    static List<RawWord> Split(string text)
    {
        var words = new List<RawWord>();
        var sentence = 0;
        var i = 0;
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                var breaks = false;
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                {
                    breaks |= text[i] == '\n' || text[i] == '\r';
                    i++;
                }

                if (words.Count > 0)
                    words[^1] = words[^1] with { BreakAfter = breaks };
                continue;
            }

            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;
            var slice = text.AsSpan(start, i - start);
            words.Add(new RawWord(slice.ToString(), start, i, sentence, EndsSentence(slice), false));
            if (EndsSentence(slice))
                sentence++;
        }

        return words;
    }

    static bool EndsSentence(ReadOnlySpan<char> word)
    {
        for (var i = word.Length - 1; i >= 0; i--)
        {
            var c = word[i];
            if (c is '"' or '\'' or ')' or ']' or '”' or '’')
                continue;
            return c is '.' or '!' or '?';
        }

        return false;
    }

    static SKColor ToSk(Color color)
        => new(
            (byte)Math.Clamp(color.Red * 255d, 0, 255),
            (byte)Math.Clamp(color.Green * 255d, 0, 255),
            (byte)Math.Clamp(color.Blue * 255d, 0, 255),
            (byte)Math.Clamp(color.Alpha * 255d, 0, 255));

    [StructLayout(LayoutKind.Auto)]
    readonly record struct RawWord(string Text, int Start, int End, int SentenceIndex, bool EndsSentence, bool BreakAfter);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Word(string Text, int Start, int End, int SentenceIndex, float X, float LineTop, float Width, float LineHeight, float Baseline)
    {
        public float Right => X + Width;

        public float LineBottom => LineTop + LineHeight;
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Sentence(int Index, int Start, int End, int FirstWord, int WordCount);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct PlacedText(Word[] Words, Sentence[] Sentences, float ContentWidth, float ContentHeight);
}
