namespace Reactor.Kit.Internals.Glaze;

/// <summary>
/// Turns a stroke brush into either the solid hairline or a linear gradient.
/// </summary>
internal readonly struct GlazeStroke
{
    public bool IsGradient { get; private init; }

    public Color Solid { get; private init; }

    public Point Start { get; private init; }

    public Point End { get; private init; }

    public (Color Color, float Offset)[] Stops { get; private init; }

    public static GlazeStroke From(Brush? brush)
    {
        if (brush is LinearGradientBrush linear)
        {
            var collected = new List<(Color Color, float Offset)>();
            if (linear.GradientStops is { } stops)
            {
                foreach (var stop in stops)
                {
                    if (stop is null)
                        continue;

                    collected.Add((stop.Color ?? Colors.Transparent, stop.Offset));
                }
            }

            collected.Sort(static (left, right) => left.Offset.CompareTo(right.Offset));
            return new GlazeStroke
            {
                IsGradient = true,
                Solid = GlazeTokens.Stroke,
                Start = linear.StartPoint,
                End = linear.EndPoint,
                Stops = collected.ToArray(),
            };
        }

        var color = brush is SolidColorBrush solid && solid.Color is not null
            ? solid.Color
            : GlazeTokens.Stroke;
        return new GlazeStroke
        {
            Solid = color,
            Stops = [],
        };
    }
}
