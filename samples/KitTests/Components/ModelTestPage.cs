namespace KitTests.Components;

sealed class ModelTestPage : Component<ModelTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public string? Path { get; set; }

        public string Note { get; set; } = "Opening gem.glb";
    }

    public override VisualNode Render()
        => ContentPage("Model",
            VStack(
                State.Path is null
                    ? Label(State.Note)
                        .TextColor(Paper)
                        .FontSize(15)
                        .Margin(16)
                    : ObjectView()
                        .Source(State.Path)
                        .Background(Colors.LightBlue)
                        .HeightRequest(400)
                        .HFill(),
                Label("Two colors come from the file. Drag to orbit.")
                    .TextColor(Paper)
                    .FontSize(13)
                    .Margin(16, 12)
            )
            .Spacing(0)
        )
        .BackgroundColor(Ink);

    protected override void OnMounted()
    {
        base.OnMounted();
        _ = Open();
    }

    // Source wants a filesystem path. A packaged asset does not have one.
    async Task Open()
    {
        try
        {
            var path = System.IO.Path.Combine(FileSystem.CacheDirectory, "gem.glb");
            var source = await FileSystem.OpenAppPackageFileAsync("gem.glb").ConfigureAwait(false);
            try
            {
                var target = File.Create(path);
                try
                {
                    await source.CopyToAsync(target).ConfigureAwait(false);
                }
                finally
                {
                    await target.DisposeAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                await source.DisposeAsync().ConfigureAwait(false);
            }

            MainThread.BeginInvokeOnMainThread(() => SetState(s => s.Path = path));
        }
        catch (Exception ex) when (ex is IOException or FileNotFoundException or UnauthorizedAccessException)
        {
            MainThread.BeginInvokeOnMainThread(() => SetState(s => s.Note = "gem.glb did not open."));
        }
    }
}
