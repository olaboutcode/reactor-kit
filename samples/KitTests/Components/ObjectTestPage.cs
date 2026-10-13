using System.Globalization;

namespace KitTests.Components;

sealed class ObjectTestPage : Component<ObjectTestPage.PageState>
{
    static readonly Mesh Cube = Mesh.Box(2f);
    static readonly Color Ink = Color.FromArgb("#1C1C1E");
    static readonly Color Paper = Color.FromArgb("#F7F5F2");

    // Held so scale changes the cube's size. An unset distance would back the camera up instead.
    const double Frame = 6.08;
    const double TurnStep = 15;
    const double ScaleStep = 1.25;

    internal sealed class PageState
    {
        public double Yaw { get; set; }

        public double Pitch { get; set; }

        public double Roll { get; set; }

        public double Scale { get; set; } = 1;
    }

    public override VisualNode Render()
        => ContentPage("Object",
            VStack(
                ObjectView()
                    .Mesh(Cube)
                    .Rotation(State.Yaw, State.Pitch, State.Roll)
                    .Scale(State.Scale)
                    .Distance(Frame)
                    .Color(Color.FromArgb("#C8C2B8"))
                    .Background(Colors.LightBlue)
                    .HeightRequest(400)
                    .HFill(),
                Controls()
            )
            .Spacing(0)
        )
        .BackgroundColor(Ink);

    VStack Controls()
        => VStack(
            Label("Drag orbits. These turn and scale the cube.")
                .TextColor(Ink)
                .FontSize(16),
            Axis("Yaw", Degrees(State.Yaw), Turn),
            Axis("Pitch", Degrees(State.Pitch), Tilt),
            Axis("Roll", Degrees(State.Roll), Roll),
            Axis("Scale", State.Scale.ToString("0.00", CultureInfo.InvariantCulture), Zoom),
            GlassButton("Reset", Reset)
                .HStart()
        )
        .Padding(16, 16)
        .Spacing(10)
        .BackgroundColor(Paper);

    static HStack Axis(string name, string value, Action<int> apply)
        => HStack(
            Label(name)
                .TextColor(Ink)
                .FontSize(15)
                .WidthRequest(64)
                .VCenter(),
            GlassButton("−", () => apply(-1)),
            Label(value)
                .TextColor(Ink)
                .FontSize(15)
                .WidthRequest(72)
                .HCenter()
                .VCenter(),
            GlassButton("+", () => apply(1))
        )
        .Spacing(8)
        .HStart();

    static GlazeView GlassButton(string text, Action onTap)
        => GlazeView(
            Grid(
                Label(text)
                    .TextColor(Ink)
                    .FontSize(16)
                    .Center(),
                Button(text, onTap)
                    .TextColor(Colors.Transparent)
                    .BackgroundColor(Colors.Transparent)
                    .Height(36)
            )
            .Padding(8, 0)
        )
        .CornerRadius(7)
        .Shadow(
            new MauiReactor.Shadow()
                .Brush(Brush.Black)
                .Offset(0.3, 0.3)
                .Radius(4)
                .Opacity(0.28f)
        );

    static string Degrees(double value)
        => value.ToString("0", CultureInfo.InvariantCulture) + "°";

    void Turn(int direction)
        => SetState(s => s.Yaw += direction * TurnStep);

    void Tilt(int direction)
        => SetState(s => s.Pitch += direction * TurnStep);

    void Roll(int direction)
        => SetState(s => s.Roll += direction * TurnStep);

    void Zoom(int direction)
        => SetState(s => s.Scale = Math.Clamp(
            direction < 0 ? s.Scale / ScaleStep : s.Scale * ScaleStep,
            0.25,
            4));

    void Reset()
        => SetState(s =>
        {
            s.Yaw = 0;
            s.Pitch = 0;
            s.Roll = 0;
            s.Scale = 1;
        });
}
