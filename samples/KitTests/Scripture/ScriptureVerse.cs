namespace KitTests.Scripture;

/// <summary>
/// One verse. <see cref="Speech"/> is null for narration, empty or <c>all</c> for the whole verse,
/// or the first spoken words. <see cref="Marked"/> is the verse a page highlights at first.
/// </summary>
internal sealed record ScriptureVerse(int Number, string Text, string? Speech, bool Marked);
