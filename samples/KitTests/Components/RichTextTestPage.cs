namespace KitTests.Components;

sealed class RichTextTestPage : Component<RichTextTestPage.PageState>
{
    internal sealed class PageState
    {
        public string LastTap { get; set; } = "Tap a word";
    }

    const string Verse =
        "In the beginning God created the heaven and the earth. " +
        "And the earth was without form, and void.";

    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");
    static readonly int[] OpeningSentence = [0];

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
                Label(State.LastTap)
                    .FontSize(15)
                    .TextColor(Ink),
                RichTextView()
                    .Text(Verse)
                    .FontSize(22)
                    .LineSpacing(6)
                    .TextColor(Ink)
                    .HighlightColor(Color.FromRgba(245, 196, 40, 120))
                    .HighlightedSentences(OpeningSentence)
                    .OnTokenTapped(hit => SetState(s => s.LastTap = hit.Text))
            )
            .Padding(20)
            .Spacing(16)
        )
        .BackgroundColor(Paper);

    void Close()
        => _ = Navigation?.PopModalAsync();
}
