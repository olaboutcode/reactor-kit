using MauiReactor;
using Reactor.Kit.RichTextViewTokens;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;
using NativeRichText = Reactor.Kit.Internals.RichText.RichTextCanvas;

namespace Reactor.Kit;

/// <summary>
/// Skia text. Tap a word or sentence, and paint highlights behind the text.
/// A long-press on highlighted text raises its own callback.
/// <see cref="SplitColumn()"/> lays the text out as two columns.
/// </summary>
public sealed class RichTextView : View<SKCanvasView>
{
    Action<TextTokenHitEventArgs>? _onTokenTapped;
    Action<TextTokenHitEventArgs>? _onTokenLongPressed;
    Action<int>? _onSpreadChanged;

    internal RichTextView(Action<SKCanvasView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new NativeRichText();
        base.OnMount();
    }

    public RichTextView Text(string text)
        => this.Set(NativeRichText.TextProperty, text);

    public RichTextView Text(Func<string> text, IComponentWithState? state = null)
        => this.Set(NativeRichText.TextProperty, new PropertyValue<string>(text, state));

    public RichTextView FontSize(double size)
        => this.Set(NativeRichText.FontSizeProperty, size);

    public RichTextView FontSize(Func<double> size, IComponentWithState? state = null)
        => this.Set(NativeRichText.FontSizeProperty, new PropertyValue<double>(size, state));

    public RichTextView FontFamily(string family)
        => this.Set(NativeRichText.FontFamilyProperty, family);

    public RichTextView FontFamily(Func<string> family, IComponentWithState? state = null)
        => this.Set(NativeRichText.FontFamilyProperty, new PropertyValue<string>(family, state));

    public RichTextView LineSpacing(double spacing)
        => this.Set(NativeRichText.LineSpacingProperty, spacing);

    public RichTextView LineSpacing(Func<double> spacing, IComponentWithState? state = null)
        => this.Set(NativeRichText.LineSpacingProperty, new PropertyValue<double>(spacing, state));

    /// <summary>
    /// Uses these metrics for the whole document.
    /// An unset size or spacing keeps the view font size and line spacing.
    /// </summary>
    public RichTextView Appearance(TextAppearance appearance)
        => this.Set(NativeRichText.AppearanceProperty, appearance);

    /// <summary>
    /// Builds the metrics from the current text before each layout.
    /// This is used instead of <see cref="Appearance(TextAppearance)"/> when both are set.
    /// </summary>
    public RichTextView Appearance(Func<string, TextAppearance> appearance)
        => this.Set(NativeRichText.AppearanceFactoryProperty, appearance);

    public RichTextView TextColor(Color color)
        => this.Set(NativeRichText.TextColorProperty, color);

    public RichTextView TextColor(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeRichText.TextColorProperty, new PropertyValue<Color>(color, state));

    public RichTextView HighlightColor(Color color)
        => this.Set(NativeRichText.HighlightColorProperty, color);

    public RichTextView HighlightColor(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeRichText.HighlightColorProperty, new PropertyValue<Color>(color, state));

    public RichTextView ToggleHighlightOnTap(bool enabled)
        => this.Set(NativeRichText.ToggleHighlightOnTapProperty, enabled);

    public RichTextView TapTarget(TextTapTarget target)
        => this.Set(NativeRichText.TapTargetProperty, target);

    public RichTextView TapRanges(IReadOnlyList<TextRange> ranges)
        => this.Set(NativeRichText.TapRangesProperty, ranges);

    public RichTextView TapRanges(Func<IReadOnlyList<TextRange>> ranges, IComponentWithState? state = null)
        => this.Set(NativeRichText.TapRangesProperty, new PropertyValue<IReadOnlyList<TextRange>>(ranges, state));

    public RichTextView HighlightedWords(IReadOnlyList<int> wordIndexes)
        => this.Set(NativeRichText.HighlightedWordsProperty, wordIndexes);

    public RichTextView HighlightedWords(Func<IReadOnlyList<int>> wordIndexes, IComponentWithState? state = null)
        => this.Set(NativeRichText.HighlightedWordsProperty, new PropertyValue<IReadOnlyList<int>>(wordIndexes, state));

    public RichTextView HighlightedSentences(IReadOnlyList<int> sentenceIndexes)
        => this.Set(NativeRichText.HighlightedSentencesProperty, sentenceIndexes);

    public RichTextView HighlightedSentences(Func<IReadOnlyList<int>> sentenceIndexes, IComponentWithState? state = null)
        => this.Set(NativeRichText.HighlightedSentencesProperty, new PropertyValue<IReadOnlyList<int>>(sentenceIndexes, state));

    public RichTextView HighlightedRanges(IReadOnlyList<TextRange> ranges)
        => this.Set(NativeRichText.HighlightedRangesProperty, ranges);

    public RichTextView HighlightedRanges(Func<IReadOnlyList<TextRange>> ranges, IComponentWithState? state = null)
        => this.Set(NativeRichText.HighlightedRangesProperty, new PropertyValue<IReadOnlyList<TextRange>>(ranges, state));

    /// <summary>
    /// Lays the text out as two columns. A later call replaces an earlier one.
    /// </summary>
    public RichTextView SplitColumn()
        => this.Set(NativeRichText.SplitColumnProperty, true);

    /// <summary>
    /// Lays the text out as two columns when <paramref name="when"/> returns true.
    /// A null function is true. False keeps the one continuous column.
    /// </summary>
    public RichTextView SplitColumn(Func<bool>? when, IComponentWithState? state = null)
    {
        if (when is null)
            return SplitColumn();

        // Set(object) would store the property value itself. The bool overload keeps the condition.
        SetProperty(NativeRichText.SplitColumnProperty, new PropertyValue<bool>(when, state));
        return this;
    }

    /// <summary>Space between the two columns, in points. The default is 24. A negative value is laid out as 0.</summary>
    public RichTextView ColumnGap(double gap)
        => this.Set(NativeRichText.ColumnGapProperty, gap);

    /// <summary>Space between the two columns, in points. The default is 24. A negative value is laid out as 0.</summary>
    public RichTextView ColumnGap(Func<double> gap, IComponentWithState? state = null)
    {
        SetProperty(NativeRichText.ColumnGapProperty, new PropertyValue<double>(gap, state));
        return this;
    }

    /// <summary>
    /// Zero-based spread the page owns. The default is 0.
    /// A value outside the document is clamped, and that clamp calls <see cref="OnSpreadChanged"/>.
    /// </summary>
    public RichTextView Spread(int spread)
        => this.Set(NativeRichText.SpreadProperty, spread);

    /// <summary>
    /// Zero-based spread the page owns. The default is 0.
    /// A value outside the document is clamped, and that clamp calls <see cref="OnSpreadChanged"/>.
    /// </summary>
    public RichTextView Spread(Func<int> spread, IComponentWithState? state = null)
    {
        SetProperty(NativeRichText.SpreadProperty, new PropertyValue<int>(spread, state));
        return this;
    }

    public RichTextView OnTokenTapped(Action<TextTokenHitEventArgs> callback)
    {
        _onTokenTapped = callback;
        return this;
    }

    /// <summary>
    /// Called when highlighted text is held. The hit matches a custom range when the word overlaps one, and otherwise matches <see cref="TapTarget"/>.
    /// That gesture does not also call <see cref="OnTokenTapped"/>.
    /// </summary>
    public RichTextView OnTokenLongPressed(Action<TextTokenHitEventArgs> callback)
    {
        _onTokenLongPressed = callback;
        return this;
    }

    /// <summary>
    /// Called with the spread now shown when a drag commits, or when a stored spread is clamped.
    /// Setting <see cref="Spread(int)"/> to the value already shown does not call this.
    /// A false <see cref="SplitColumn()"/> condition does not call this.
    /// </summary>
    public RichTextView OnSpreadChanged(Action<int> callback)
    {
        _onSpreadChanged = callback;
        return this;
    }

    protected override void OnAttachNativeEvents()
    {
        if (NativeControl is NativeRichText canvas)
        {
            canvas.TokenTapped -= OnNativeTokenTapped;
            if (_onTokenTapped is not null)
                canvas.TokenTapped += OnNativeTokenTapped;

            canvas.TokenLongPressed -= OnNativeTokenLongPressed;
            if (_onTokenLongPressed is not null)
                canvas.TokenLongPressed += OnNativeTokenLongPressed;

            canvas.SpreadChanged -= OnNativeSpreadChanged;
            if (_onSpreadChanged is not null)
                canvas.SpreadChanged += OnNativeSpreadChanged;
        }

        base.OnAttachNativeEvents();
    }

    protected override void OnDetachNativeEvents()
    {
        if (NativeControl is NativeRichText canvas)
        {
            canvas.TokenTapped -= OnNativeTokenTapped;
            canvas.TokenLongPressed -= OnNativeTokenLongPressed;
            canvas.SpreadChanged -= OnNativeSpreadChanged;
        }

        base.OnDetachNativeEvents();
    }

    void OnNativeTokenTapped(object? sender, TextTokenHitEventArgs hit)
        => _onTokenTapped?.Invoke(hit);

    void OnNativeTokenLongPressed(object? sender, TextTokenHitEventArgs hit)
        => _onTokenLongPressed?.Invoke(hit);

    void OnNativeSpreadChanged(object? sender, NativeRichText.SpreadEventArgs spread)
        => _onSpreadChanged?.Invoke(spread.Spread);
}
