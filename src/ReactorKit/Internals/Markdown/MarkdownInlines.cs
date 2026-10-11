namespace Reactor.Kit.Internals.Markdown;

/// <summary>Inline marks inside a markdown span. Image files are not opened.</summary>
internal static class MarkdownInlines
{
    [Flags]
    internal enum Style
    {
        None = 0,
        Strong = 1,
        Emphasis = 2,
        Strike = 4,
        Code = 8,
        Subscript = 16,
        Superscript = 32,
        Mark = 64,
        Insert = 128,
        Link = 256,
    }

    internal readonly record struct Span(string Text, Style Style);
}
