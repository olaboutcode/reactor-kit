using System.Globalization;
using System.Text;
using System.Text.Json;

namespace KitTests.Scripture;

/// <summary>
/// Reads a chapter document and lays it out for a page.
/// A blank line separates sections, and a title occupies its own line.
/// A verse is its number, a space, and its text. Verses in a section are separated by a space.
/// The verse span includes the number. Spoken words run from the speech phrase through the end of the verse.
/// </summary>
internal static class ScriptureLayout
{
    const string ResourceName = "KitTests.john-3.json";

    internal static ScripturePage Load()
    {
        using var stream = typeof(ScriptureLayout).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidDataException($"The chapter resource {ResourceName} is missing.");
        using var reader = new StreamReader(stream);
        return LayOut(Read(reader.ReadToEnd()));
    }

    static ScriptureDocument Read(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("The chapter document must be an object.");
            }

            return ReadChapter(root);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The chapter document is not valid JSON.", exception);
        }
    }

    static ScriptureDocument ReadChapter(JsonElement root)
    {
        var book = RequiredString(root, "book", "The book name is missing.");
        var chapter = RequiredInt(root, "chapter", "The chapter number is missing.");
        if (chapter < 1)
        {
            throw new InvalidDataException("The chapter number must be at least 1.");
        }

        var sections = ReadSections(root);
        RequireOrder(sections);
        return new ScriptureDocument(book, chapter, sections);
    }

    static List<ScriptureSection> ReadSections(JsonElement root)
    {
        if (!root.TryGetProperty("sections", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            throw new InvalidDataException("The chapter has no sections.");
        }

        var sections = new List<ScriptureSection>(value.GetArrayLength());
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException("A section must be an object.");
            }

            sections.Add(ReadSection(item));
        }

        return sections;
    }

    static ScriptureSection ReadSection(JsonElement item)
    {
        var title = RequiredString(item, "title", "A section title is missing.");
        if (!item.TryGetProperty("verses", out var value) || value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            throw new InvalidDataException($"The section \"{title}\" has no verses.");
        }

        var verses = new List<ScriptureVerse>(value.GetArrayLength());
        foreach (var verse in value.EnumerateArray())
        {
            if (verse.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"A verse in \"{title}\" must be an object.");
            }

            verses.Add(ReadVerse(verse));
        }

        return new ScriptureSection(title, verses);
    }

    static ScriptureVerse ReadVerse(JsonElement item)
    {
        var number = RequiredInt(item, "number", "A verse number is missing.");
        var text = RequiredString(item, "text", $"Verse {number} has no text.");
        return new ScriptureVerse(number, text, Speech(item, number), Marked(item, number));
    }

    static string? Speech(JsonElement item, int number)
    {
        if (!item.TryGetProperty("speech", out var value))
        {
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException($"Verse {number} has a speech mark that is not text.");
        }

        return value.GetString()?.Trim() ?? "";
    }

    static bool Marked(JsonElement item, int number)
    {
        if (!item.TryGetProperty("marked", out var value))
        {
            return false;
        }

        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new InvalidDataException($"Verse {number} has a marked value that is not true or false.");
        }

        return value.GetBoolean();
    }

    static void RequireOrder(List<ScriptureSection> sections)
    {
        var previous = 0;
        for (var s = 0; s < sections.Count; s++)
        {
            var verses = sections[s].Verses;
            for (var v = 0; v < verses.Count; v++)
            {
                var number = verses[v].Number;
                if (number < 1)
                {
                    throw new InvalidDataException("A verse number must be at least 1.");
                }

                if (number <= previous)
                {
                    throw new InvalidDataException($"Verse {number} repeats or precedes verse {previous}.");
                }

                previous = number;
            }
        }
    }

    static ScripturePage LayOut(ScriptureDocument document)
    {
        var draft = new Draft();
        for (var i = 0; i < document.Sections.Count; i++)
        {
            AppendSection(document.Sections[i], draft);
        }

        return new ScripturePage(
            document.Book,
            document.Chapter,
            draft.Text.ToString(),
            draft.Headings,
            draft.Numbers,
            draft.Speech,
            draft.Verses,
            draft.Marked);
    }

    static void AppendSection(ScriptureSection section, Draft draft)
    {
        if (draft.Text.Length > 0)
        {
            draft.Text.Append("\n\n");
        }

        draft.Headings.Add(new TextRange(draft.Text.Length, section.Title.Length));
        draft.Text.Append(section.Title).Append('\n');
        for (var i = 0; i < section.Verses.Count; i++)
        {
            if (i > 0)
            {
                draft.Text.Append(' ');
            }

            AppendVerse(section.Verses[i], draft);
        }
    }

    static void AppendVerse(ScriptureVerse verse, Draft draft)
    {
        var label = verse.Number.ToString(CultureInfo.InvariantCulture);
        var origin = draft.Text.Length;
        draft.Numbers.Add(new TextRange(origin, label.Length));
        draft.Text.Append(label).Append(' ');
        var bodyAt = draft.Text.Length;
        draft.Text.Append(verse.Text);
        draft.Verses.Add(new TextRange(origin, draft.Text.Length - origin));
        var spoken = SpokenLength(verse);
        if (spoken > 0)
        {
            draft.Speech.Add(new TextRange(bodyAt + verse.Text.Length - spoken, spoken));
        }

        if (verse.Marked)
        {
            draft.Marked.Add(origin);
        }
    }

    static int SpokenLength(ScriptureVerse verse)
    {
        var speech = verse.Speech;
        if (speech is null)
        {
            return 0;
        }

        if (speech.Length == 0 || speech.Equals("all", StringComparison.Ordinal))
        {
            return verse.Text.Length;
        }

        var at = verse.Text.IndexOf(speech, StringComparison.Ordinal);
        if (at < 0)
        {
            throw new InvalidDataException($"Verse {verse.Number} does not contain the spoken words \"{speech}\".");
        }

        return verse.Text.Length - at;
    }

    static string RequiredString(JsonElement owner, string name, string message)
    {
        if (!owner.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw new InvalidDataException(message);
        }

        var text = value.GetString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException(message);
        }

        return text.Trim();
    }

    static int RequiredInt(JsonElement owner, string name, string message)
    {
        if (!owner.TryGetProperty(name, out var value) || !value.TryGetInt32(out var number))
        {
            throw new InvalidDataException(message);
        }

        return number;
    }

    private sealed class Draft
    {
        public StringBuilder Text { get; } = new();

        public List<TextRange> Headings { get; } = [];

        public List<TextRange> Numbers { get; } = [];

        public List<TextRange> Speech { get; } = [];

        public List<TextRange> Verses { get; } = [];

        public List<int> Marked { get; } = [];
    }
}
