using MauiReactor.Shapes;

namespace KitTests.Components;

/// <summary>
/// One switch, set to the plain pill and to the Material, Cupertino, and Fluent looks.
/// </summary>
sealed class SwitchTestPage : Component<SwitchTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public bool Wifi { get; set; }

        public bool Sync { get; set; }

        public bool Bluetooth { get; set; }

        public bool Airplane { get; set; }

        public bool Notifications { get; set; }

        public bool Locked { get; set; } = true;
    }

    public override VisualNode Render()
        => ContentPage("Switch",
            Grid("auto,*", "*",
                NavigationBar.Create("Switch"),
                ScrollView(
                    VStack(
                        Plain("Wi-Fi", State.Wifi, on => SetState(s => s.Wifi = on)),
                        Material(State.Sync, on => SetState(s => s.Sync = on)),
                        Cupertino(State.Bluetooth, on => SetState(s => s.Bluetooth = on)),
                        Fluent(State.Airplane, on => SetState(s => s.Airplane = on)),
                        Notifications(),
                        Plain("Locked", State.Locked, on => SetState(s => s.Locked = on))
                            .IsEnabled(false)
                    )
                    .Spacing(28)
                    .Padding(0, 8)
                )
                .GridRow(1)
            )
            .Padding(16)
        )
        .BackgroundColor(Paper)
        .HasNavigationBar(hasNavigationBar: false);

    static SwitchView Plain(string label, bool on, Action<bool> changed)
        => SwitchView()
            .Label(label)
            .IsOn(on)
            .OnChanged(changed);

    static SwitchView Material(bool on, Action<bool> changed)
        => SwitchView()
            .TrackWidth(52)
            .TrackHeight(32)
            .TrackOffColor(Color.FromArgb("#E6E0E9"))
            .TrackOnColor(Color.FromArgb("#6750A4"))
            .TrackOffStroke(Color.FromArgb("#79747E"))
            .TrackOffStrokeThickness(2)
            .ThumbOffSize(16)
            .ThumbOffColor(Color.FromArgb("#79747E"))
            .ThumbOnSize(24)
            .ThumbPressedSize(28)
            .ThumbOnColor(Colors.White)
            .ThumbOnContent(Check())
            .Label("Sync")
            .IsOn(on)
            .OnChanged(changed);

    static SwitchView Cupertino(bool on, Action<bool> changed)
        => SwitchView()
            .TrackWidth(51)
            .TrackHeight(31)
            .TrackOffColor(Colors.White)
            .TrackOnColor(Color.FromArgb("#007AFF"))
            .TrackOffStroke(Color.FromArgb("#C6C6C8"))
            .TrackOffStrokeThickness(1.5)
            .ThumbOffSize(27)
            .ThumbOnSize(27)
            .ThumbOffColor(Colors.White)
            .ThumbOnColor(Colors.White)
            .ThumbOffStroke(Color.FromArgb("#C6C6C8"))
            .ThumbOffStrokeThickness(1.5)
            .Label("Bluetooth")
            .IsOn(on)
            .OnChanged(changed);

    static SwitchView Fluent(bool on, Action<bool> changed)
        => SwitchView()
            .TrackWidth(40)
            .TrackHeight(20)
            .TrackOffColor(Color.FromArgb("#F3F3F3"))
            .TrackOnColor(Color.FromArgb("#0078D4"))
            .ThumbOffSize(10)
            .ThumbOnSize(10)
            .ThumbOffColor(Color.FromArgb("#616161"))
            .ThumbOnColor(Colors.White)
            .ThumbOnInnerSize(4)
            .Label("Airplane")
            .IsOn(on)
            .OnChanged(changed);

    SwitchView Notifications()
        => SwitchView()
            .Label(HStack(spacing: 8,
                Image(HeroIcons.Bell)
                    .Width(20)
                    .Height(20),
                Label("Notifications")
                    .FontSize(17)
                    .TextColor(Ink)))
            .IsOn(State.Notifications)
            .OnChanged(on => SetState(s => s.Notifications = on));

    static MauiReactor.Shapes.Path Check()
        => new MauiReactor.Shapes.Path()
            .Data("M2,6 L5,10 L11,2")
            .Stroke(Color.FromArgb("#4F378B"))
            .StrokeThickness(2)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .StrokeLineJoin(MauiControls.Shapes.PenLineJoin.Round)
            .Aspect(Stretch.Uniform)
            .WidthRequest(10)
            .HeightRequest(10)
            .BackgroundColor(Colors.Transparent);
}
