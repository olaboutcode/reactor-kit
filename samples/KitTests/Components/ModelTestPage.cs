namespace KitTests.Components;

sealed class ModelTestPage : Component<ModelTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public string? Path { get; set; }

        public string Note { get; set; } = "Opening the microscope";

        public double ScreenHeight { get; set; }
    }

    public override VisualNode Render()
        => ContentPage("Model",
            Grid("auto,*", "*",
                Button("Close", Close)
                    .TextColor(Paper)
                    .BackgroundColor(Ink)
                    .FontSize(15)
                    .CornerRadius(16)
                    .Height(36)
                    .Padding(14, 0)
                    .HStart(),

                VStack(
                    State.Path is null
                        ? Label(State.Note)
                            .TextColor(Paper)
                            .FontSize(15)
                            .Margin(16)
                        : ObjectView()
                            .Source(State.Path)
                            .Background(Colors.LightBlue)
                            .HeightRequest(State.ScreenHeight * 0.7) 
                            .HFill(),
                    Label("Industrial Microscope by Lukas Walzer, Poly Haven (CC0). Drag to orbit.")
                        .TextColor(Paper)
                        .FontSize(13)
                        .Margin(16, 12)
                )
                .Spacing(0)
                .GridRow(1)
            )
        )
        .BackgroundColor(Ink);

    protected override void OnMounted()
    {
        base.OnMounted();

        DeviceDisplay.Current.MainDisplayInfoChanged += OnDisplayInfoChanged;
        UpdateDimensions();

        _ = Open();
    }

    protected override void OnWillUnmount()
    {
        DeviceDisplay.Current.MainDisplayInfoChanged -= OnDisplayInfoChanged;
        base.OnWillUnmount();
    }

    void OnDisplayInfoChanged(object? sender, DisplayInfoChangedEventArgs e)
        => UpdateDimensions();

    void UpdateDimensions()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        double calculatedHeight = displayInfo.Height / displayInfo.Density;
        SetState(s => s.ScreenHeight = calculatedHeight);
    }

    void Close() => _ = Navigation?.PopModalAsync();

    // Source wants a filesystem path. A packaged asset does not have one.
    async Task Open()
    {
        try
        {
            var path = System.IO.Path.Combine(FileSystem.CacheDirectory, "industrial_microscope.glb");
            var source = await FileSystem.OpenAppPackageFileAsync("industrial_microscope.glb").ConfigureAwait(false);
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
            MainThread.BeginInvokeOnMainThread(() => SetState(s => s.Note = "The microscope file did not open."));
        }
    }
}
