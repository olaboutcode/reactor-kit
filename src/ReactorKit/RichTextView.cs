using MauiReactor;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;
using NativeRichText = Reactor.Kit.RichText.RichTextCanvas;

namespace Reactor.Kit;

/// <summary>
/// Skia text. Tap a word or sentence, and paint highlights behind the text.
/// A long-press on highlighted text raises its own callback.
/// </summary>
public sealed class RichTextView : View<SKCanvasView>
{
    Action<TextTokenHit>? _onTokenTapped;
    Action<TextTokenHit>? _onTokenLongPressed;

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

    public RichTextView OnTokenTapped(Action<TextTokenHit> callback)
    {
        _onTokenTapped = callback;
        return this;
    }

    /// <summary>
    /// Called when highlighted text is held. The hit matches <see cref="TapTarget"/>.
    /// That gesture does not also call <see cref="OnTokenTapped"/>.
    /// </summary>
    public RichTextView OnTokenLongPressed(Action<TextTokenHit> callback)
    {
        _onTokenLongPressed = callback;
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
        }

        base.OnAttachNativeEvents();
    }

    protected override void OnDetachNativeEvents()
    {
        if (NativeControl is NativeRichText canvas)
        {
            canvas.TokenTapped -= OnNativeTokenTapped;
            canvas.TokenLongPressed -= OnNativeTokenLongPressed;
        }

        base.OnDetachNativeEvents();
    }

    void OnNativeTokenTapped(object? sender, TextTokenHit hit)
        => _onTokenTapped?.Invoke(hit);

    void OnNativeTokenLongPressed(object? sender, TextTokenHit hit)
        => _onTokenLongPressed?.Invoke(hit);
}
