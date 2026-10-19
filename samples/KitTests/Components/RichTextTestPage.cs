using System.Globalization;
using KitTests.Scripture;

namespace KitTests.Components;

/// <summary>
/// John 3, King James Version, laid out from a chapter document.
/// </summary>
sealed class RichTextTestPage : Component<RichTextTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly Color Muted = Color.FromArgb("#8E8E93");
    static readonly Color Red = Color.FromArgb("#B42318");
    static readonly Color Gold = Color.FromRgba(245, 196, 40, 120);

    static readonly ScripturePage John = ScriptureLayout.Load();

    internal sealed class PageState
    {
        public Mode Tap { get; set; } = Mode.Verse;

        public HashSet<int> Words { get; } = [];

        public HashSet<int> Sentences { get; } = [];

        public HashSet<int> Verses { get; } = [.. John.Marked];

        public string Hit { get; set; } = "";

        public string Hold { get; set; } = "";

        public bool Split { get; set; } = true;

        public int Spread { get; set; }
    }

    internal enum Mode
    {
        Verse,
        Sentence,
        Word,
    }

    public override VisualNode Render()
        => ContentPage($"{John.Book} {John.Chapter}",
            Grid("auto,auto,*", "*",
                NavigationBar.Create("Rich Text Test"),
                VStack(
                    Label(John.Book)
                        .FontSize(16)
                        .TextColor(Muted),
                    Label(John.Chapter.ToString(CultureInfo.InvariantCulture))
                        .FontSize(32)
                        .TextColor(Ink),
                    HStack(
                        ModeButton("Verse", Mode.Verse),
                        ModeButton("Sentence", Mode.Sentence),
                        ModeButton("Word", Mode.Word)
                    )
                    .Spacing(7),
                    ColumnsButton(),
                    Label(State.Hit.Length == 0 ? "Tap a verse" : State.Hit)
                        .FontSize(15)
                        .TextColor(Ink),
                    Label(State.Hold.Length == 0 ? "Hold a highlight" : State.Hold)
                        .FontSize(13)
                        .TextColor(Muted)
                )
                .Spacing(8)
                .GridRow(1),
                ScrollView(
                    VStack(
                        PagedExcerpt(),
                        ChapterText()
                    )
                    .Spacing(28))
                    .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                    .GridRow(2)
            )
            .Padding(16)
            .RowSpacing(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    RichTextView ChapterText()
        => RichTextView(John.Text)
            .FontFamily(AppTheme.FontFamily)
            .FontSize(16)
            .LineSpacing(7)
            .TextColor(Ink)
            .HighlightColor(Gold)
            .Appearance(_ => new TextAppearance
            {
                LineSpacing = 7,
                PreserveLineBreaks = true,
                RunAt = (start, _) => Style(start),
            })
            .TapTarget(State.Tap == Mode.Sentence ? TextTapTarget.Sentence : TextTapTarget.Word)
            .TapRanges(State.Tap == Mode.Verse ? John.Verses : [])
            .ToggleHighlightOnTap(enabled: false)
            .HighlightedWords(State.Tap == Mode.Word ? [.. State.Words.Order()] : [])
            .HighlightedSentences(State.Tap == Mode.Sentence ? [.. State.Sentences.Order()] : [])
            .HighlightedRanges(State.Tap == Mode.Verse ? Ranges(State.Verses) : [])
            .OnTokenTapped(OnTap)
            .OnTokenLongPressed(hit => SetState(s => s.Hold = hit.Text))
            .SplitColumn(when: () => State.Split, this)
            .ColumnGap(32)
            .VStart();

    VStack PagedExcerpt()
        => VStack(
            Label("Two columns, gap 48")
                .FontSize(13)
                .TextColor(Muted),
            Label($"Spread {State.Spread + 1}")
                .FontSize(13)
                .TextColor(Muted),
            Grid(
                "220",
                "*",
                RichTextView(Opening())
                    .FontFamily(AppTheme.FontFamily)
                    .FontSize(16)
                    .LineSpacing(7)
                    .TextColor(Ink)
                    .SplitColumn()
                    .ColumnGap(48)
                    .Spread(() => State.Spread, this)
                    .OnSpreadChanged(spread => SetState(s => s.Spread = spread))
            )
        )
        .Spacing(8);

    static string Opening()
    {
        const int verses = 8;
        var index = Math.Min(verses, John.Verses.Count) - 1;
        if (index < 0)
            return John.Text;

        var last = John.Verses[index];
        var end = (int)Math.Min(John.Text.Length, (long)last.Start + last.Length);
        return John.Text[..end];
    }

    MauiReactor.Button ModeButton(string label, Mode mode)
        => Button(label, () => SetState(s => s.Tap = mode))
            .TextColor(State.Tap == mode ? Paper : Ink)
            .BackgroundColor(State.Tap == mode ? Ink : Colors.Transparent)
            .CornerRadius(14)
            .Height(32)
            .Padding(12, 0);

    MauiReactor.Button ColumnsButton()
        => Button(State.Split ? "Split" : "One column", () => SetState(s => s.Split = !s.Split))
            .TextColor(State.Split ? Paper : Ink)
            .BackgroundColor(State.Split ? Ink : Colors.Transparent)
            .CornerRadius(14)
            .Height(32)
            .Padding(12, 0)
            .HStart();

    void OnTap(TextTokenHitEventArgs hit)
        => SetState(s =>
        {
            s.Hit = hit.Text;
            if (s.Tap == Mode.Verse && hit.Kind == TextTokenKind.Range)
            {
                Toggle(s.Verses, hit.Start);
            }
            else if (s.Tap == Mode.Sentence)
            {
                Toggle(s.Sentences, hit.SentenceIndex);
            }
            else if (s.Tap == Mode.Word)
            {
                Toggle(s.Words, hit.WordIndex);
            }
        });

    static void Toggle(HashSet<int> keys, int key)
    {
        if (!keys.Remove(key))
        {
            keys.Add(key);
        }
    }

    static TextRange[] Ranges(HashSet<int> starts)
    {
        var ranges = new List<TextRange>(starts.Count);
        for (var i = 0; i < John.Verses.Count; i++)
        {
            var range = John.Verses[i];
            if (starts.Contains(range.Start))
            {
                ranges.Add(range);
            }
        }

        return [.. ranges];
    }

    static RunMetrics Style(int start)
    {
        if (Covers(John.Headings, start))
        {
            return new RunMetrics { Bold = true };
        }

        if (Covers(John.Numbers, start))
        {
            return new RunMetrics { Scale = 0.62f, BaselineShift = 0.45f, Color = Muted };
        }

        if (Covers(John.Speech, start))
        {
            return new RunMetrics { Color = Red };
        }

        return default;
    }

    static bool Covers(IReadOnlyList<TextRange> marks, int start)
    {
        for (var i = 0; i < marks.Count; i++)
        {
            var mark = marks[i];
            if (start >= mark.Start && start < mark.Start + (long)mark.Length)
            {
                return true;
            }
        }

        return false;
    }

    void Close()
        => _ = Navigation?.PopModalAsync();
}
