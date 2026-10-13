namespace KitTests.Components;

sealed class ModelTestPage : Component<ModelTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
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
                    Object3DView()
                        .PackagedSource("industrial_microscope.glb")
                        .Background(Ink)
                        .HeightRequest(State.ScreenHeight * 0.7)
                        .HFill(),
                    Label("Industrial Microscope by Lukas Walzer, Poly Haven (CC0). Drag to orbit.")
                        .TextColor(Paper)
                        .FontSize(13)
                        .Margin(0,16)
                )
                .Padding(16,0)
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
}
