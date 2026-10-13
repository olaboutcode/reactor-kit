namespace KitTests.Scripture;

/// <summary>
/// A titled group of verses inside a chapter.
/// </summary>
internal sealed record ScriptureSection(string Title, IReadOnlyList<ScriptureVerse> Verses);
