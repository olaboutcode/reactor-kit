namespace KitTests.Scripture;

/// <summary>
/// A chapter as stored: a book, a chapter number, and titled sections of verses.
/// </summary>
internal sealed record ScriptureDocument(string Book, int Chapter, IReadOnlyList<ScriptureSection> Sections);
