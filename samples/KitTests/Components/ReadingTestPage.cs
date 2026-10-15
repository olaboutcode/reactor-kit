using System.Globalization;
using System.Text;

namespace KitTests.Components;

/// <summary>
/// Three readings. The page chooses the metrics. The view does not know the genre.
/// </summary>
sealed class ReadingTestPage : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Muted = Color.FromArgb("#8E8E93");
    static readonly Color Red = Color.FromArgb("#B42318");

    static readonly Passage Gospel = Scripture();
    static readonly Passage Psalm = Psalm23();

    const string Novel =
        "The rain had stopped before she reached the gate. She stood with one hand on the iron and listened to the water still moving in the trees.\n\n" +
        "He had written that he would wait. The letter was soft at the fold, and the ink had run where her thumb stayed too long.";

    public override VisualNode Render()
        => ContentPage("Reading",
            Grid("auto,*", "*",
                NavigationBar.Create("Various Text Types"),
                ScrollView(Sections())
                    .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                    .GridRow(1)
            )
            .Padding(16)
            .RowSpacing(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(false);

    static VStack Sections()
        => VStack(
            Block(
                "Novel",
                "Indented paragraphs. A blank line separates them.",
                RichTextView(Novel)
                    .FontFamily(AppTheme.FontFamily)
                    .FontSize(16)
                    .TextColor(Ink)
                    .Appearance(new TextAppearance
                    {
                        LineSpacing = 6,
                        FirstLineIndent = 28,
                        PreserveLineBreaks = false,
                    })
                    .VStart()),
            Block(
                "Scripture",
                "Verse numbers are small and raised. Words of Jesus are red.",
                Reading(Gospel, poetry: false)),
            Block(
                "Psalm",
                "Line breaks in the verse are kept.",
                Reading(Psalm, poetry: true))
        )
        .Spacing(28);

    static VStack Block(string title, string note, RichTextView text)
        => VStack(
            Label(title).FontSize(13).TextColor(Muted),
            Label(note).FontSize(15).TextColor(Ink),
            text
        )
        .Spacing(6);

    static RichTextView Reading(Passage passage, bool poetry)
        => RichTextView(passage.Text)
            .FontFamily(AppTheme.FontFamily)
            .FontSize(16)
            .TextColor(Ink)
            .LineSpacing(poetry ? 5 : 4)
            .Appearance(_ => Marked(passage, poetry))
            .VStart();

    static TextAppearance Marked(Passage passage, bool poetry)
        => new()
        {
            LineSpacing = poetry ? 5 : 4,
            PreserveLineBreaks = poetry,
            RunAt = (start, _) => StyleOf(passage, start),
        };

    static RunMetrics StyleOf(Passage passage, int start)
    {
        if (Covers(passage.Numbers, start))
            return new RunMetrics { Scale = 0.65f, BaselineShift = 0.4f, Color = Muted };
        if (Covers(passage.Red, start))
            return new RunMetrics { Color = Red };
        return default;
    }

    static bool Covers(IReadOnlyList<Mark> marks, int start)
    {
        for (var i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (start >= mark.Start && start < mark.Start + mark.Length)
                return true;
        }

        return false;
    }

    static Passage Scripture()
    {
        const string Way = "Jesus saith unto him, I am the way, the truth, and the life: no man cometh unto the Father, but by me.";
        var redAt = Way.IndexOf("I am the way", StringComparison.Ordinal);
        return Compose(
            new Verse(5, "Thomas saith unto him, Lord, we know not whither thou goest; and how can we know the way?"),
            new Verse(6, Way, redAt, Way.Length - redAt));
    }

    static Passage Psalm23()
        => Compose(
            new Verse(1, "The LORD is my shepherd; I shall not want."),
            new Verse(2, "He maketh me to lie down in green pastures:\nhe leadeth me beside the still waters."),
            new Verse(3, "He restoreth my soul:\nhe leadeth me in the paths of righteousness for his name's sake."));

    static Passage Compose(params Verse[] verses)
    {
        var text = new StringBuilder();
        var numbers = new List<Mark>();
        var red = new List<Mark>();
        for (var i = 0; i < verses.Length; i++)
        {
            var verse = verses[i];
            var number = verse.Number.ToString(CultureInfo.InvariantCulture);
            numbers.Add(new Mark(text.Length, number.Length));
            text.Append(number).Append(' ').Append(verse.Text);
            if (verse.RedLength > 0)
                red.Add(new Mark(text.Length - verse.Text.Length + verse.RedStart, verse.RedLength));
            if (i < verses.Length - 1)
                text.Append('\n');
        }

        return new Passage(text.ToString(), numbers, red);
    }

    void Close() => _ = Navigation?.PopAsync();

    sealed record Passage(string Text, List<Mark> Numbers, List<Mark> Red);

    sealed record Mark(int Start, int Length);

    sealed record Verse(int Number, string Text, int RedStart = 0, int RedLength = 0);
}
