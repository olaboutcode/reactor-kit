using System.Runtime.InteropServices;
using Microsoft.Maui.Dispatching;
using Reactor.Kit.RichTextViewTokens;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Reactor.Kit.Internals.RichText;

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

    public static readonly BindableProperty AppearanceProperty = BindableProperty.Create(
        nameof(Appearance), typeof(TextAppearance), typeof(RichTextCanvas), null, propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty AppearanceFactoryProperty = BindableProperty.Create(
        nameof(AppearanceFactory), typeof(Func<string, TextAppearance>), typeof(RichTextCanvas), null, propertyChanged: OnLayoutPropertyChanged);

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

    /// <summary>Hold this long on highlighted text to raise <see cref="TokenLongPressed"/>.</summary>
    static readonly TimeSpan LongPressDuration = TimeSpan.FromMilliseconds(500);

    /// <summary>Highlight box is at least this multiple of the font size. 16 lands on 18.</summary>
    const float LineBoxScale = 1.125f;

    readonly Dictionary<int, object?> _tags = [];
    readonly Dictionary<int, TextShaper> _sizes = [];
    readonly TextShaper _shaper = new();
    Word[] _words = [];
    Sentence[] _sentences = [];
    bool[] _highlighted = [];
    SKFont? _font;
    SKPaint? _textPaint;
    SKPaint? _highlightPaint;
    float _laidWidth = -1;
    float _laidSize = 17;
    float _contentWidth;
    float _contentHeight;
    bool _layoutDirty = true;
    bool _disposed;
    int _pressedWord = -1;
    SKPoint _pressPoint;
    bool _longPressFired;
    int _longPressWord = -1;
    IDispatcherTimer? _longPressTimer;

    public RichTextCanvas()
    {
        IgnorePixelScaling = true;
        EnableTouchEvents = true;
        BackgroundColor = Colors.Transparent;
        PaintSurface += OnPaintSurface;
        Touch += OnTouch;
    }

    /// <summary>
    /// Raised after a tap on a word.
    /// The hit is the word, or its sentence when <see cref="TapTarget"/> is <see cref="TextTapTarget.Sentence"/>
    /// .</summary>
    public event EventHandler<TextTokenHitEventArgs>? TokenTapped;

    /// <summary>
    /// Raised when highlighted text is held.
    /// The hit matches <see cref="TapTarget"/>. That gesture does not also raise <see cref="TokenTapped"/>.
    /// </summary>
    public event EventHandler<TextTokenHitEventArgs>? TokenLongPressed;

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

    /// <summary>Page metrics. The factory is used instead when it is set.</summary>
    public TextAppearance? Appearance
    {
        get => (TextAppearance?)GetValue(AppearanceProperty);
        set => SetValue(AppearanceProperty, value);
    }

    /// <summary>Builds the page metrics from the text before each layout.</summary>
    public Func<string, TextAppearance>? AppearanceFactory
    {
        get => (Func<string, TextAppearance>?)GetValue(AppearanceFactoryProperty);
        set => SetValue(AppearanceFactoryProperty, value);
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
        EndPress();
        DisposeSkia();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
        {
            EndPress();
            DisposeSkia();
        }
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
        ReleaseWords();
        var text = Text ?? string.Empty;
        var appearance = AppearanceFactory?.Invoke(text) ?? Appearance;
        var size = (float)Math.Max(1, appearance?.FontSize ?? FontSize);
        var spacing = (float)Math.Max(0, appearance?.LineSpacing ?? LineSpacing);
        var preserve = appearance?.PreserveLineBreaks ?? true;
        var indent = Math.Max(0, appearance?.FirstLineIndent ?? 0);
        _laidSize = size;
        EnsureFont(size);
        _shaper.Use(size);
        var font = _font!;
        var metrics = font.Metrics;
        var em = Math.Max(1f, metrics.Descent - metrics.Ascent);
        var box = Math.Max(em, size * LineBoxScale);
        var pad = 2f;
        var placed = Place(
            Split(text, preserve),
            appearance?.RunAt,
            size,
            new LineMetrics(em, metrics.Ascent, box, spacing, font.MeasureText(" "), indent, pad, Math.Max(1, width - (pad * 2))));

        _words = placed.Words;
        _sentences = placed.Sentences;
        _highlighted = new bool[placed.Words.Length];
        _laidWidth = width;
        _contentWidth = placed.ContentWidth;
        _contentHeight = placed.ContentHeight;
        _layoutDirty = false;
        EndPress();
        PruneTags();
        ApplyDeclarativeHighlights(repaint: false);
        var content = Math.Max(1d, _contentHeight);
        if (Math.Abs(HeightRequest - content) > 0.5)
            HeightRequest = content;
    }

    PlacedText Place(List<RawWord> raw, Func<int, int, RunMetrics>? runAt, float pageSize, LineMetrics line)
    {
        var placer = new Placer(raw.Count, line);
        var sentences = new List<Sentence>();
        var sentenceStart = 0;
        for (var i = 0; i < raw.Count; i++)
        {
            var piece = raw[i];
            var length = piece.End - piece.Start;
            var metrics = runAt?.Invoke(piece.Start, length) ?? default;
            var shaped = ShapeWord(piece.Text, pageSize, metrics);
            var broken = i > 0 && raw[i - 1].BreakAfter;
            var paragraph = i > 0 && raw[i - 1].ParagraphAfter;
            placer.Add(i, piece, shaped, broken, paragraph);
            if (!ClosesSentence(raw, i))
                continue;
            var first = placer.Words[sentenceStart];
            sentences.Add(new Sentence(piece.SentenceIndex, first.Start, placer.Words[i].End, sentenceStart, i - sentenceStart + 1));
            sentenceStart = i + 1;
        }

        placer.Close(raw.Count);
        return new PlacedText(placer.Words, [.. sentences], placer.ContentWidth, placer.ContentHeight);
    }

    ShapedWord ShapeWord(string text, float pageSize, RunMetrics metrics)
    {
        var scale = metrics.Scale ?? 1f;
        if (scale <= 0)
            scale = 1f;
        var size = Math.Max(1f, pageSize * scale);
        var runs = ShaperFor(size).Shape(text);
        var width = 0f;
        for (var i = 0; i < runs.Length; i++)
            width += runs[i].Width;
        var em = runs.Length == 0 ? size : Math.Max(1f, runs[0].Font.Metrics.Descent - runs[0].Font.Metrics.Ascent);
        var box = Math.Max(em, size * LineBoxScale);
        SKColor? color = metrics.Color is { } ink ? ToSk(ink) : null;
        return new ShapedWord(runs, width, box, metrics.BaselineShift * pageSize, color, metrics.Italic, metrics.Bold);
    }

    TextShaper ShaperFor(float size)
    {
        if (_font is not null && Math.Abs(_font.Size - size) < 0.05f)
            return _shaper;
        var key = (int)Math.Round(size * 100f);
        if (_sizes.TryGetValue(key, out var found))
            return found;
        var created = new TextShaper();
        created.Use(size);
        _sizes[key] = created;
        return created;
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

        EnsureFont(_laidSize);
        _highlightPaint ??= new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };
        _textPaint ??= new SKPaint { IsAntialias = true };
        _highlightPaint.Color = ToSk(HighlightColor);

        DrawHighlights(canvas);
        for (var i = 0; i < _words.Length; i++)
            DrawWord(canvas, _words[i], _textPaint);
    }

    void DrawWord(SKCanvas canvas, Word word, SKPaint paint)
    {
        paint.Color = word.Color ?? ToSk(TextColor);
        if (!word.Italic)
        {
            DrawRuns(canvas, word, word.X, word.Baseline, paint);
            return;
        }

        canvas.Save();
        canvas.Translate(word.X, word.Baseline);
        canvas.Skew(-0.22f, 0);
        DrawRuns(canvas, word, 0, 0, paint);
        canvas.Restore();
    }

    static void DrawRuns(SKCanvas canvas, Word word, float x, float baseline, SKPaint paint)
    {
        var runs = word.Runs;
        for (var i = 0; i < runs.Length; i++)
        {
            var run = runs[i];
            run.Draw(canvas, x, baseline, paint);
            if (word.Bold && run.Blob is not null)
                run.Draw(canvas, x + 0.6f, baseline, paint);
            x += run.Width;
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
            _pressPoint = e.Location;
            _longPressFired = false;
            // iOS ignores the rest of the touch unless the press is handled.
            e.Handled = _pressedWord >= 0;
            ArmLongPress(_pressedWord);
            return;
        }

        if (e.ActionType == SKTouchAction.Moved)
        {
            _pressPoint = e.Location;
            if (_pressedWord >= 0 && !Hits(_pressedWord, e.Location))
                CancelLongPress();
            e.Handled = _pressedWord >= 0;
            return;
        }

        if (e.ActionType is SKTouchAction.Cancelled or SKTouchAction.Exited)
        {
            EndPress();
            return;
        }

        if (e.ActionType != SKTouchAction.Released)
            return;

        var pressed = _pressedWord;
        var longPressed = _longPressFired;
        EndPress();
        if (longPressed || pressed < 0 || !Hits(pressed, e.Location))
        {
            e.Handled = longPressed;
            return;
        }

        e.Handled = true;
        Activate(pressed);
    }

    void ArmLongPress(int wordIndex)
    {
        CancelLongPress();
        if (wordIndex < 0 || TokenLongPressed is null || Dispatcher is not { } dispatcher)
            return;

        _longPressWord = wordIndex;
        var timer = dispatcher.CreateTimer();
        timer.Interval = LongPressDuration;
        timer.IsRepeating = false;
        timer.Tick += OnLongPressTick;
        _longPressTimer = timer;
        timer.Start();
    }

    void OnLongPressTick(object? sender, EventArgs e)
    {
        var word = _longPressWord;
        CancelLongPress();
        if (word < 0 || word != _pressedWord || (uint)word >= (uint)_highlighted.Length)
            return;
        if (!Hits(word, _pressPoint) || !_highlighted[word])
            return;

        _longPressFired = true;
        TokenLongPressed?.Invoke(this, CreateHit(word, SentenceIndex(word)));
    }

    void CancelLongPress()
    {
        if (_longPressTimer is null)
            return;
        _longPressTimer.Tick -= OnLongPressTick;
        _longPressTimer.Stop();
        _longPressTimer = null;
        _longPressWord = -1;
    }

    void EndPress()
    {
        _pressedWord = -1;
        _longPressFired = false;
        CancelLongPress();
    }

    void Activate(int wordIndex)
    {
        var sentenceIndex = SentenceIndex(wordIndex);

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

    int SentenceIndex(int wordIndex)
    {
        var sentence = _sentences.FirstOrDefault(item => wordIndex >= item.FirstWord && wordIndex < item.FirstWord + item.WordCount);
        return sentence.WordCount == 0 ? _words[wordIndex].SentenceIndex : sentence.Index;
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

    TextTokenHitEventArgs CreateHit(int wordIndex, int sentenceIndex)
    {
        var word = _words[wordIndex];
        _tags.TryGetValue(wordIndex, out var tag);
        var bounds = new Rect(word.X, word.LineTop, word.Width, word.LineHeight);
        if (TapTarget == TextTapTarget.Word || (uint)sentenceIndex >= (uint)_sentences.Length)
        {
            return new TextTokenHitEventArgs(
                word.Text,
                word.Start,
                word.End,
                wordIndex,
                sentenceIndex,
                TextTokenKind.Word,
                tag,
                bounds);
        }

        var sentence = _sentences[sentenceIndex];
        var text = Text ?? string.Empty;

        var slice = text.Substring(
            sentence.Start,
            Math.Max(0, sentence.End - sentence.Start));

        return new TextTokenHitEventArgs(
            slice,
            sentence.Start,
            sentence.End,
            wordIndex,
            sentenceIndex,
            TextTokenKind.Sentence,
            tag,
            bounds);
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

    void EnsureFont(float size)
    {
        if (_font is not null && Math.Abs(_font.Size - size) < 0.01f)
            return;
        _font?.Dispose();
        _font = new SKFont(SKTypeface.Default, size);
    }

    void DisposeSkia()
    {
        ReleaseWords();
        _words = [];
        foreach (var shaper in _sizes.Values)
            shaper.Dispose();
        _sizes.Clear();
        _shaper.Dispose();
        _font?.Dispose();
        _textPaint?.Dispose();
        _highlightPaint?.Dispose();
        _font = null;
        _textPaint = null;
        _highlightPaint = null;
    }

    void ReleaseWords()
    {
        for (var i = 0; i < _words.Length; i++)
        {
            var runs = _words[i].Runs;
            for (var r = 0; r < runs.Length; r++)
                runs[r].Dispose();
        }
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

    static List<RawWord> Split(string text, bool preserveLineBreaks)
    {
        var words = new List<RawWord>();
        var sentence = 0;
        var i = 0;
        while (i < text.Length)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                var newlines = CountNewlines(text, ref i);
                if (words.Count > 0)
                {
                    var paragraph = newlines >= 2;
                    words[^1] = words[^1] with
                    {
                        BreakAfter = paragraph || (preserveLineBreaks && newlines > 0),
                        ParagraphAfter = paragraph,
                    };
                }

                continue;
            }

            var start = i;
            while (i < text.Length && !char.IsWhiteSpace(text[i]))
                i++;
            var slice = text.AsSpan(start, i - start);
            words.Add(new RawWord(
                slice.ToString(),
                start,
                i,
                sentence,
                EndsSentence(slice),
                BreakAfter: false,
                ParagraphAfter: false));

            if (EndsSentence(slice))
                sentence++;
        }

        return words;
    }

    static int CountNewlines(string text, ref int index)
    {
        var newlines = 0;
        while (index < text.Length && char.IsWhiteSpace(text[index]))
        {
            if (text[index] is '\n' or '\r')
            {
                newlines++;
                if (text[index] == '\r' && index + 1 < text.Length && text[index + 1] == '\n')
                    index++;
            }

            index++;
        }

        return newlines;
    }

    static bool EndsSentence(ReadOnlySpan<char> word)
    {
        for (var i = word.Length - 1; i >= 0; i--)
        {
            var c = word[i];
            if (c is '"' or '\'' or ')' or ']' or '”' or '’')
                continue;
            return c is '.' or '!' or '?' or '。' or '！' or '？' or '؟';
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
    readonly record struct RawWord(
        string Text,
        int Start,
        int End,
        int SentenceIndex,
        bool EndsSentence,
        bool BreakAfter,
        bool ParagraphAfter);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Word(
        string Text,
        int Start,
        int End,
        int SentenceIndex,
        float X,
        float LineTop,
        float Width,
        float LineHeight,
        float Baseline,
        GlyphRun[] Runs,
        SKColor? Color,
        bool Italic,
        bool Bold,
        float Raise)
    {
        public float Right => X + Width;

        public float LineBottom => LineTop + LineHeight;
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct ShapedWord(
        GlyphRun[] Runs,
        float Width,
        float Box,
        float Raise,
        SKColor? Color,
        bool Italic,
        bool Bold);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct LineMetrics(
        float Em,
        float Ascent,
        float Box,
        float Spacing,
        float Space,
        float Indent,
        float Pad,
        float Max);

    sealed class Placer
    {
        readonly float _pageEm;
        readonly float _ascent;
        readonly float _pageBox;
        readonly float _spacing;
        readonly float _space;
        readonly float _indent;
        readonly float _pad;
        readonly float _max;
        readonly Word[] _words;
        float _x;
        float _lineTop;
        float _lineBox;
        float _contentWidth;
        int _lineStart;
        bool _hasWord;

        public Placer(int count, LineMetrics line)
        {
            _pageEm = line.Em;
            _ascent = line.Ascent;
            _pageBox = line.Box;
            _spacing = line.Spacing;
            _space = line.Space;
            _indent = line.Indent;
            _pad = line.Pad;
            _max = line.Max;
            _words = new Word[count];
            _x = line.Pad + line.Indent;
            _lineTop = line.Pad;
            _lineBox = line.Box;
            _contentWidth = line.Pad;
        }

        public Word[] Words => _words;

        public float ContentWidth => _words.Length == 0 ? _pad * 2 : _contentWidth;

        public float ContentHeight => _words.Length == 0 ? _pad * 2 : _lineTop + _lineBox + _pad;

        public void Add(int index, RawWord piece, ShapedWord shaped, bool breakBefore, bool paragraph)
        {
            if (breakBefore || (_hasWord && _x + _space + shaped.Width > _pad + _max))
            {
                Close(index);
                _lineTop += _lineBox + _spacing;
                if (paragraph)
                    _lineTop += _lineBox + _spacing;
                _x = _pad + (paragraph ? _indent : 0);
                _lineStart = index;
                _lineBox = _pageBox;
                _hasWord = false;
            }
            else if (_hasWord)
            {
                _x += _space;
            }

            _words[index] = new Word(
                piece.Text,
                piece.Start,
                piece.End,
                piece.SentenceIndex,
                _x,
                0,
                shaped.Width,
                0,
                0,
                shaped.Runs,
                shaped.Color,
                shaped.Italic,
                shaped.Bold,
                shaped.Raise);
            _lineBox = Math.Max(_lineBox, shaped.Box);
            _x += shaped.Width;
            _contentWidth = Math.Max(_contentWidth, _x + _pad);
            _hasWord = true;
        }

        public void Close(int end)
        {
            if (end <= _lineStart)
                return;
            var offset = ((_lineBox - _pageEm) / 2f) - _ascent;
            for (var i = _lineStart; i < end; i++)
            {
                var word = _words[i];
                _words[i] = word with
                {
                    LineTop = _lineTop,
                    LineHeight = _lineBox,
                    Baseline = _lineTop + offset - word.Raise,
                };
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    readonly record struct Sentence(
        int Index,
        int Start,
        int End,
        int FirstWord,
        int WordCount);

    [StructLayout(LayoutKind.Auto)]
    readonly record struct PlacedText(
        Word[] Words,
        Sentence[] Sentences,
        float ContentWidth,
        float ContentHeight);
}
