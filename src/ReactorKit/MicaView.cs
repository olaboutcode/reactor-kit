using MauiReactor;
using MauiReactor.Animations;
using SKCanvasView = SkiaSharp.Views.Maui.Controls.SKCanvasView;
using NativeMica = Reactor.Kit.Internals.Mica.MicaCanvas;

namespace Reactor.Kit;

/// <summary>
/// Painted mica background. Content is a sibling. The layer does not take the tap.
/// </summary>
public sealed class MicaView : View<SKCanvasView>
{
    internal MicaView(Action<SKCanvasView?>? componentRefAction = null)
        : base(componentRefAction)
    {
    }

    protected override void OnMount()
    {
        _nativeControl ??= new NativeMica();
        base.OnMount();
    }

    public MicaView TintColor(Color color)
        => this.Set(NativeMica.TintColorProperty, color);

    public MicaView TintColor(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeMica.TintColorProperty, new PropertyValue<Color>(color, state));

    public MicaView TintOpacity(double opacity, RxDoubleAnimation? animation = null)
        => Animate(NativeMica.TintOpacityProperty, opacity, animation);

    public MicaView TintOpacity(Func<double> opacity, IComponentWithState? state = null)
        => this.Set(NativeMica.TintOpacityProperty, new PropertyValue<double>(opacity, state));

    /// <summary>Bottom stop. Null darkens <see cref="TintColor(Color)"/> by 6%.</summary>
    public MicaView ShadeColor(Color? color)
        => this.Set(NativeMica.ShadeColorProperty, color);

    /// <summary>Bottom stop. Null darkens the tint by 6%.</summary>
    public MicaView ShadeColor(Func<Color?> color, IComponentWithState? state = null)
        => this.Set(NativeMica.ShadeColorProperty, new PropertyValue<Color?>(color, state));

    /// <summary>Bottom alpha. A negative value uses 88% of the tint opacity.</summary>
    public MicaView ShadeOpacity(double opacity, RxDoubleAnimation? animation = null)
        => Animate(NativeMica.ShadeOpacityProperty, opacity, animation);

    /// <summary>Bottom alpha. A negative value uses 88% of the tint opacity.</summary>
    public MicaView ShadeOpacity(Func<double> opacity, IComponentWithState? state = null)
        => this.Set(NativeMica.ShadeOpacityProperty, new PropertyValue<double>(opacity, state));

    public MicaView NoiseOpacity(double opacity, RxDoubleAnimation? animation = null)
        => Animate(NativeMica.NoiseOpacityProperty, opacity, animation);

    public MicaView NoiseOpacity(Func<double> opacity, IComponentWithState? state = null)
        => this.Set(NativeMica.NoiseOpacityProperty, new PropertyValue<double>(opacity, state));

    public MicaView Stroke(Color color)
        => this.Set(NativeMica.StrokeProperty, color);

    public MicaView Stroke(Func<Color> color, IComponentWithState? state = null)
        => this.Set(NativeMica.StrokeProperty, new PropertyValue<Color>(color, state));

    /// <summary>
    /// Stroke width in dp. The default is one physical pixel, <c>1 / density</c>, on iOS and Android.
    /// Zero draws no stroke.
    /// </summary>
    public MicaView StrokeThickness(double thickness, RxDoubleAnimation? animation = null)
        => Animate(NativeMica.StrokeThicknessProperty, thickness, animation);

    public MicaView StrokeThickness(Func<double> thickness, IComponentWithState? state = null)
        => this.Set(NativeMica.StrokeThicknessProperty, new PropertyValue<double>(thickness, state));

    public MicaView CornerRadius(double radius, RxDoubleAnimation? animation = null)
        => Animate(NativeMica.CornerRadiusProperty, radius, animation);

    public MicaView CornerRadius(Func<double> radius, IComponentWithState? state = null)
        => this.Set(NativeMica.CornerRadiusProperty, new PropertyValue<double>(radius, state));

    MicaView Animate(BindableProperty property, double value, RxDoubleAnimation? animation)
    {
        SetProperty(property, value);
        AppendAnimatable(property, animation ?? new RxDoubleAnimation(value));
        return this;
    }
}
