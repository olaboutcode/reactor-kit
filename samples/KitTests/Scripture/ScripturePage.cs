namespace KitTests.Scripture;

/// <summary>
/// The chapter a page renders: one string, and the spans for headings, numbers, speech, and verses.
/// <see cref="Marked"/> holds the start of each verse that begins highlighted.
/// </summary>
internal sealed record ScripturePage(
    string Book,
    int Chapter,
    string Text,
    IReadOnlyList<TextRange> Headings,
    IReadOnlyList<TextRange> Numbers,
    IReadOnlyList<TextRange> Speech,
    IReadOnlyList<TextRange> Verses,
    IReadOnlyList<int> Marked);
