using MauiReactor;
using MauiReactor.Animations;
using MauiContentView = Microsoft.Maui.Controls.ContentView;
using NativeGlaze = Reactor.Kit.Internals.Glaze.GlazeView;

namespace Reactor.Kit;

/// <summary>
/// Mica panel. The child is a normal MAUI view and is never blurred.
/// </summary>
public sealed class GlazeView : MauiReactor.ContentView
{
    internal GlazeView(Action<MauiContentView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new NativeGlaze();
        base.OnMount();
    }

    public GlazeView TintColor(Color color)
        => this.Set(NativeGlaze.TintColorProperty, color);

    public GlazeView TintColor(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeGlaze.TintColorProperty, new PropertyValue<Color>(color, state));

    public GlazeView TintOpacity(double opacity, RxDoubleAnimation? animation = null)
        => Animate(NativeGlaze.TintOpacityProperty, opacity, animation);

    public GlazeView TintOpacity(Func<double> opacity, IComponentWithState? state = null)
        => this.Set(NativeGlaze.TintOpacityProperty, new PropertyValue<double>(opacity, state));

    public GlazeView BlurRadius(double radius, RxDoubleAnimation? animation = null)
        => Animate(NativeGlaze.BlurRadiusProperty, radius, animation);

    public GlazeView BlurRadius(Func<double> radius, IComponentWithState? state = null)
        => this.Set(NativeGlaze.BlurRadiusProperty, new PropertyValue<double>(radius, state));

    public GlazeView NoiseOpacity(double opacity, RxDoubleAnimation? animation = null)
        => Animate(NativeGlaze.NoiseOpacityProperty, opacity, animation);

    public GlazeView NoiseOpacity(Func<double> opacity, IComponentWithState? state = null)
        => this.Set(NativeGlaze.NoiseOpacityProperty, new PropertyValue<double>(opacity, state));

    /// <summary>Paints the hairline with a solid color.</summary>
    public GlazeView Stroke(Color? color)
        => this.Set(NativeGlaze.StrokeProperty, Solid(color));

    /// <summary>Paints the hairline from a color the page owns.</summary>
    public GlazeView Stroke(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeGlaze.StrokeProperty, new PropertyValue<Brush>(() => Solid(color()), state));

    /// <summary>Paints the hairline with a linear gradient. Any other brush stays the default solid line.</summary>
    public GlazeView Stroke(Brush? brush)
        => this.Set(NativeGlaze.StrokeProperty, brush);

    /// <summary>Paints the hairline from a brush the page owns.</summary>
    public GlazeView Stroke(Func<Brush> brush, IComponentWithState? state = null)
        => this.Set(NativeGlaze.StrokeProperty, new PropertyValue<Brush>(brush, state));

    static SolidColorBrush Solid(Color? color)
        => color is null ? Internals.Glaze.GlazeTokens.StrokeBrush : new SolidColorBrush(color);

    public GlazeView StrokeThickness(double thickness, RxDoubleAnimation? animation = null)
        => Animate(NativeGlaze.StrokeThicknessProperty, thickness, animation);

    public GlazeView StrokeThickness(Func<double> thickness, IComponentWithState? state = null)
        => this.Set(NativeGlaze.StrokeThicknessProperty, new PropertyValue<double>(thickness, state));

    public GlazeView CornerRadius(double radius, RxDoubleAnimation? animation = null)
        => Animate(NativeGlaze.CornerRadiusProperty, radius, animation);

    public GlazeView CornerRadius(Func<double> radius, IComponentWithState? state = null)
        => this.Set(NativeGlaze.CornerRadiusProperty, new PropertyValue<double>(radius, state));

    GlazeView Animate(BindableProperty property, double value, RxDoubleAnimation? animation)
    {
        SetProperty(property, value);
        AppendAnimatable(property, animation ?? new RxDoubleAnimation(value));
        return this;
    }
}
