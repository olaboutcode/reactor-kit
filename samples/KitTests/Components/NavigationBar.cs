using MauiReactor.Shapes;

namespace KitTests.Components;

public sealed partial class NavigationBar : Component
{
    static readonly Color Ink = Color.FromArgb("#1C1C1E");

    [Prop]private string? _title = null;
    [Prop]private Color? _textColor = null;

    public override VisualNode Render()
        => Grid(
            RoundButton(Close)
            .HStart()
            .VCenter(),

            Label(_title ?? "Page Title")
            .TextTransform(TextTransform.Uppercase)
            .TextColor(_textColor ?? Ink)
            .Center()
        )
        .Padding(0,4)
        .BackgroundColor(Colors.Transparent);

    GlazeView RoundButton(Action onClick)
        => GlazeView(
            new MauiReactor.Shapes.Path()
            .Data("M16,4 L8,12 L16,20")
            .Stroke(_textColor ?? Ink)
            .StrokeThickness(1.5)
            .StrokeLineCap(MauiControls.Shapes.PenLineCap.Round)
            .Width(24)
            .Height(24)
            .Center()
        )
        .Width(44)
        .Height(44)
        .CornerRadius(22)
        .HCenter()
        .Shadow(ShadowElevation.For(1))
        .OnTapped(onClick);

    public static NavigationBar Create(string title) 
        => new NavigationBar().Title(title);

    void Close() => _ = Navigation?.PopModalAsync(animated: false);
}
