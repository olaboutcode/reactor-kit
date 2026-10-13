namespace Reactor.Kit.RichTextViewTokens;

/// <summary>
/// The word, sentence, or custom range under a tap or a long-press.
/// </summary>
public sealed class TextTokenHitEventArgs : EventArgs
{
    internal TextTokenHitEventArgs(
        string text,
        int start,
        int end,
        int wordIndex,
        int sentenceIndex,
        TextTokenKind kind,
        object? tag,
        Rect bounds)
    {
        Text = text;
        Start = start;
        End = end;
        WordIndex = wordIndex;
        SentenceIndex = sentenceIndex;
        Kind = kind;
        Tag = tag;
        Bounds = bounds;
    }

    /// <summary>
    /// The tapped word, the sentence around it, or the custom range that overlaps it.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Inclusive character index in the paragraph.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Exclusive character index in the paragraph.
    /// </summary>
    public int End { get; }

    /// <summary>Index of the tapped word.</summary>
    public int WordIndex { get; }

    /// <summary>
    /// Index of the sentence that contains the tapped word.
    /// </summary>
    public int SentenceIndex { get; }

    /// <summary>
    /// Word, sentence, or a custom range.
    /// </summary>
    public TextTokenKind Kind { get; }

    /// <summary>
    /// Optional value associated with the tapped word.
    /// </summary>
    public object? Tag { get; }

    /// <summary>
    /// Bounds of the tapped word, in device-independent pixels.
    /// </summary>
    public Rect Bounds { get; }
}
