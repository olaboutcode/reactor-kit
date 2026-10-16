namespace KitTests.Components;

sealed class MarkdownTestPage : Component
{
    const string Notes =
        """
        # Field notes

        A paragraph with **bold**, *italic*, and `code`. A picture link such as ![Field sketch](sketch.png) keeps its alt text. The picture file is not loaded.

        ## Table

        | Script | Example |
        | --- | :---: |
        | Han | 神 |
        | Arabic | مرحبا |
        | Emoji | 🙏🏽 |

        ## Diagram

        ```diagram
        TB
        Read[Read file] --> Parse
        Parse --> Table[Draw table]
        Parse --> Graph[Draw diagram]
        Table --> Page
        Graph --> Page
        ```

        ## Code

        ```csharp
        var notes = MarkdownView(markdown);
        notes.FontSize(16);
        ```

        ## More tokens

        Setext
        ------

        > A quote
        >
        > > A nested quote

        - Parent
          - Child
          - [x] Done
          - [ ] Open

        ~~Strike~~, ***both***, H~2~O, x^2^, ==mark==, and ++insert++.

        See [Reactor](https://github.com/olaboutcode/reactor-kit), https://example.com, and `code`.

        A hard break follows.  
        The next line stays in the paragraph.

        Inline math $e = mc^2$ and a block:

        $$
        a^2 + b^2 = c^2
        $$

        A footnote sits here.[^1]

        > [!WARNING]
        > Watch the edge.

        Term
        :   A defined word.

        HTML is markup.

        *[HTML]: HyperText Markup Language

        :smile:

        [^1]: The note under the rule.
        """;

    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    public override VisualNode Render()
        => ContentPage("Markdown",
            Grid("Auto,*", "*",
                NavigationBar.Create("Markdown"),
                ScrollView(
                    MarkdownView()
                        .Markdown(Notes)
                        .FontFamily(AppTheme.FontFamily)
                        .FontSize(16)
                        .TextColor(Ink)
                        .VStart()
                )
                .VerticalScrollBarVisibility(ScrollBarVisibility.Never)
                .GridRow(1)
            )
            .Padding(16)
            .RowSpacing(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(false);

    void Close()
        => _ = Navigation?.PopModalAsync();
}
