namespace KitTests.Components;

/// <summary>
/// One checkbox, set to the plain box, a square, a circle, and the Material, macOS, and Fluent looks.
/// </summary>
sealed class CheckboxTestPage : Component<CheckboxTestPage.PageState>
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    internal sealed class PageState
    {
        public bool Terms { get; set; }

        public bool Backup { get; set; }

        public bool Agree { get; set; }

        public bool Sync { get; set; }

        public bool ICloud { get; set; }

        public bool Mail { get; set; }

        public bool Notifications { get; set; }

        public bool Locked { get; set; } = true;
    }

    public override VisualNode Render()
        => ContentPage("Checkbox",
            Grid("auto,*", "*",
                NavigationBar.Create("Checkbox"),
                ScrollView(
                    VStack(
                        Plain("Terms", State.Terms, on => SetState(s => s.Terms = on)),
                        Plain("Backup", State.Backup, on => SetState(s => s.Backup = on))
                            .BoxCornerRadius(0),
                        Plain("Agree", State.Agree, on => SetState(s => s.Agree = on))
                            .BoxCornerRadius(10),
                        Material(State.Sync, on => SetState(s => s.Sync = on)),
                        Mac(State.ICloud, on => SetState(s => s.ICloud = on)),
                        Fluent(State.Mail, on => SetState(s => s.Mail = on)),
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

    static CheckboxView Plain(string label, bool on, Action<bool> changed)
        => CheckboxView()
            .Label(label)
            .IsChecked(on)
            .OnChanged(changed);

    static CheckboxView Material(bool on, Action<bool> changed)
        => CheckboxView()
            .BoxSize(18)
            .BoxCornerRadius(2)
            .BoxOffColor(Colors.Transparent)
            .BoxOnColor(Color.FromArgb("#6750A4"))
            .BoxOffStroke(Color.FromArgb("#79747E"))
            .BoxOffStrokeThickness(2)
            .BoxOnStroke(Color.FromArgb("#6750A4"))
            .BoxOnStrokeThickness(0)
            .CheckColor(Colors.White)
            .Label("Sync")
            .IsChecked(on)
            .OnChanged(changed);

    static CheckboxView Mac(bool on, Action<bool> changed)
        => CheckboxView()
            .BoxSize(14)
            .BoxCornerRadius(4)
            .BoxOffColor(Colors.White)
            .BoxOnColor(Color.FromArgb("#007AFF"))
            .BoxOffStroke(Color.FromArgb("#C6C6C8"))
            .BoxOffStrokeThickness(1)
            .BoxOnStrokeThickness(0)
            .CheckColor(Colors.White)
            .Label("iCloud")
            .IsChecked(on)
            .OnChanged(changed);

    static CheckboxView Fluent(bool on, Action<bool> changed)
        => CheckboxView()
            .BoxSize(20)
            .BoxCornerRadius(4)
            .BoxOffColor(Colors.Transparent)
            .BoxOnColor(Color.FromArgb("#0078D4"))
            .BoxOffStroke(Color.FromArgb("#616161"))
            .BoxOffStrokeThickness(1)
            .BoxOnStrokeThickness(0)
            .CheckColor(Colors.White)
            .Label("Mail")
            .IsChecked(on)
            .OnChanged(changed);

    CheckboxView Notifications()
        => CheckboxView()
            .Label(HStack(spacing: 8,
                Image(HeroIcons.Bell)
                    .Width(20)
                    .Height(20),
                Label("Notifications")
                    .FontSize(17)
                    .TextColor(Ink)))
            .IsChecked(State.Notifications)
            .OnChanged(on => SetState(s => s.Notifications = on));
}
