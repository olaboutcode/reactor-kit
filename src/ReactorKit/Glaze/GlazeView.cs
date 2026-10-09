namespace Reactor.Kit.Glaze;

/// <summary>
/// Acrylic container. The child is a normal MAUI view and is never blurred.
/// </summary>
public class GlazeView : ContentView
{
    public static readonly BindableProperty TintColorProperty = BindableProperty.Create(
        nameof(TintColor), typeof(Color), typeof(GlazeView), GlazeTokens.LightTint);

    public static readonly BindableProperty TintOpacityProperty = BindableProperty.Create(
        nameof(TintOpacity), typeof(double), typeof(GlazeView), (double)GlazeTokens.TintOpacity);

    public static readonly BindableProperty BlurRadiusProperty = BindableProperty.Create(
        nameof(BlurRadius), typeof(double), typeof(GlazeView), (double)GlazeTokens.BlurRadius);

    public static readonly BindableProperty NoiseOpacityProperty = BindableProperty.Create(
        nameof(NoiseOpacity), typeof(double), typeof(GlazeView), (double)GlazeTokens.NoiseOpacity);

    public static readonly BindableProperty StrokeProperty = BindableProperty.Create(
        nameof(Stroke), typeof(Color), typeof(GlazeView), GlazeTokens.Stroke);

    public static readonly BindableProperty StrokeThicknessProperty = BindableProperty.Create(
        nameof(StrokeThickness), typeof(double), typeof(GlazeView), (double)GlazeTokens.StrokeThickness);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(GlazeView), 0d);

    public Color TintColor
    {
        get => (Color)GetValue(TintColorProperty);
        set => SetValue(TintColorProperty, value);
    }

    public double TintOpacity
    {
        get => (double)GetValue(TintOpacityProperty);
        set => SetValue(TintOpacityProperty, value);
    }

    public double BlurRadius
    {
        get => (double)GetValue(BlurRadiusProperty);
        set => SetValue(BlurRadiusProperty, value);
    }

    public double NoiseOpacity
    {
        get => (double)GetValue(NoiseOpacityProperty);
        set => SetValue(NoiseOpacityProperty, value);
    }

    public Color Stroke
    {
        get => (Color)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public GlazeView()
    {
        BackgroundColor = Colors.Transparent;
    }
}
