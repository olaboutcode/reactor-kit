namespace Reactor.Kit.RichTextViewTokens;

/// <summary>A word or a sentence inside <see cref="RichTextView"/>.</summary>
public enum TextTokenKind
{
    /// <summary>One whitespace-delimited word.</summary>
    Word,

    /// <summary>Words up to a sentence terminator.</summary>
    Sentence,
}
