namespace KitTests.Components;

sealed class RichTextTestPage : Component<RichTextTestPage.PageState>
{
    internal sealed class PageState
    {
        public Dictionary<int, string> Sentences { get; } = [];

        public string Highlighted =>
            string.Join(' ', Sentences.OrderBy(pair => pair.Key).Select(pair => pair.Value));

        public string LongPressed { get; set; } = "";
    }

    const string Verse =
        "In the beginning God created the heaven and the earth. " +
        "And the earth was without form, and void. 🙏🏽 神 مرحبا";

    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    public override VisualNode Render()
        => ContentPage("Rich text",
            VStack(
                Button("Close", Close)
                    .TextColor(Paper)
                    .BackgroundColor(Ink)
                    .FontSize(15)
                    .CornerRadius(16)
                    .Height(36)
                    .Padding(14, 0)
                    .HStart(),
                Label(State.Sentences.Count == 0 ? "Tap a sentence" : State.Highlighted)
                    .FontSize(15)
                    .TextColor(Ink),
                Label(State.LongPressed.Length == 0 ? "Long-press a highlighted sentence" : State.LongPressed)
                    .FontSize(15)
                    .TextColor(Ink),
                RichTextView()
                    .Text(Verse)
                    .FontSize(16)
                    .LineSpacing(6)
                    .TextColor(Ink)
                    .HighlightColor(Color.FromRgba(245, 196, 40, 120))
                    .TapTarget(TextTapTarget.Sentence)
                    .ToggleHighlightOnTap(enabled: true)
                    .HighlightedSentences(() => [.. State.Sentences.Keys.Order()])
                    .OnTokenTapped(hit => SetState(s =>
                    {
                        if (!s.Sentences.TryAdd(hit.SentenceIndex, hit.Text))
                            s.Sentences.Remove(hit.SentenceIndex);
                    }))
                    .OnTokenLongPressed(hit => SetState(s => s.LongPressed = hit.Text))
            )
            .Padding(20)
            .Spacing(16)
        )
        .BackgroundColor(Paper);

    void Close()
        => _ = Navigation?.PopModalAsync();
}
