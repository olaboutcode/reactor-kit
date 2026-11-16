# Glaze

This document describes `GlazeView`.

`GlazeView` is a container. The child is a normal MAUI view and is never blurred. iOS and Mac Catalyst put a `UIVisualEffectView` behind the child. Android snapshots the composited page and blurs that bitmap. Windows uses an `AcrylicBrush` with `AcrylicBackgroundSource.Backdrop`. A blur radius is an on/off switch on Apple and a dp gaussian on Android. The same call does not match.

The glass shows what is visible behind the panel. A view that covers content, and a scroll view that clips content, keep that content out of the glass. A panel on an opaque bar samples that bar. A panel over transparent content samples what shows through.

## Pieces

| Type | Role |
|---|---|
| `GlazeView` | The panel. Hosts one child tree. |
| `GlazeView()` | Factory on `Reactor.Kit.Extensions`. |

## GlazeView

The public node is `GlazeView`, a `ContentView`. `OnMount` creates the native element. `UseReactorKit()` registers the handler.

```csharp
GlazeView(Label("Title").Center())
    .TintColor(Color.FromArgb("#1C1C1E"))
    .CornerRadius(22)
    .Stroke(Colors.White)
    .StrokeThickness(1);
```

The page lays the panel out. The kit does not call `SetState`. Each color and each double has a value method and a `Func<T>` method that takes `IComponentWithState? state = null`. Each double also takes an optional `RxDoubleAnimation`.

### Backdrop

iOS and Mac Catalyst use `UIBlurEffectStyle.SystemUltraThinMaterial` when `BlurRadius` is above 0. `0` turns the material off. The effect samples the composited pixels behind the panel.

Windows uses the acrylic backdrop brush. That brush samples the composited backdrop.

Android blurs a bitmap of the page on API 31 and above. The bitmap is the pixels behind the panel after the screen has clipped and covered them. Content a scroll view has clipped away does not appear. Content an opaque view covers does not appear. The panel and its shadow are left out of the bitmap, so the blur does not turn the shadow into a dark fill. Below API 31 there is no blur. The wash uses at least the fallback opacity.

The page has to stay attached. `PushModalAsync` leaves it in place. A `NavigationPage`, or a Shell `PushAsync`, takes the page down, and the panel draws flat for a frame when the page comes back.

### Defaults

| Member | Default | Role |
|---|---|---|
| `TintColor` | `#F3F3F3` | Wash color. The mica base, not pure white. |
| `TintOpacity` | `0.28` | Requested wash. A light color is eased down, so `#F3F3F3` lands near 6% where a material already veils the backlight. A dark color stays near 25%. |
| `BlurRadius` | `8`, or `32` on Android | On iOS and Mac Catalyst, any value above 0 turns the system material on. On Android this is the blur in dp. |
| `NoiseOpacity` | `0.015` | Fine dusting over the wash. |
| `Stroke` | white, alpha 46 | Solid hairline. A linear gradient replaces that color. |
| `StrokeThickness` | `1` | Hairline width, in dp. `0` draws nothing. |
| `CornerRadius` | `0` | Rounds the panel. The caller's shadow follows this corner. |

On Android the material fill, in addition to the wash, is `#F3F3F3` at 44% in light mode and `#1C1C1E` at 55% in dark mode. The fallback wash, when blur is unavailable, is at least 76%.

A shadow is the caller's `Shadow`. This view does not add one.

### Stroke

`Stroke(Color)` stays. It stores that color as a solid brush and paints the hairline that platform paints today. `Stroke(Func<Color>)` stays. `Stroke(Brush)` accepts a `LinearGradientBrush`. `Stroke(Func<Brush>)` sits beside it. Both `Func` methods take `IComponentWithState? state = null`.

The native stroke is a `Brush`. The default is a solid brush of white at alpha 46. A null brush paints that default. A `SolidColorBrush` paints its color as that same solid hairline. A radial gradient, or any other brush, paints the default solid hairline.

A `StrokeThickness` of `0` draws nothing, for a solid color and for a gradient.

A linear gradient draws that same hairline. The path is inset by half `StrokeThickness` and follows `CornerRadius`. The gradient is mapped across the panel. `(0, 0)` is the top left of the panel. `(1, 1)` is the bottom right. Offset `0` sits on `StartPoint`. Offset `1` sits on `EndPoint`. Stops between are placed by their offset. Stop colors keep their alpha. `TintOpacity` does not scale the stroke.

A `LinearGradientBrush` that omits the points uses the MAUI defaults, `StartPoint` `(0, 0)` and `EndPoint` `(1, 1)`. A brush with fewer than two stops draws nothing. The paint pads: the end stops hold past the segment. Reflect and repeat are out of scope and still pad.

iOS, Mac Catalyst, Android, and Windows all paint the gradient. A solid color keeps the path that platform already uses.

```csharp
.Stroke(new LinearGradientBrush
{
    StartPoint = new Point(0, 0.5),
    EndPoint = new Point(1, 0.5),
    GradientStops =
    {
        new GradientStop(Color.FromRgba(255, 255, 255, 46), 0),
        new GradientStop(Color.FromArgb("#3D7EFF"), 1),
    },
})
.StrokeThickness(2)
```

The README `Stroke` row keeps the default, white at 18%. Its role says a color is a solid line, and a linear gradient runs from `StartPoint` to `EndPoint`.

## Files

| Piece | Path |
|---|---|
| Public node | `src/ReactorKit/GlazeView.cs` |
| Factory | `src/ReactorKit/Extensions.Components.cs` |
| Tokens and native node | `src/ReactorKit/Internals/Glaze/` |
| iOS and Mac Catalyst | `src/ReactorKit/Platforms/iOS/Glaze/` |
| Android | `src/ReactorKit/Platforms/Android/Glaze/` |
| Windows | `src/ReactorKit/Platforms/Windows/Glaze/` |

`UseReactorKit()` registers the glaze handler. The iOS folder is shared with Mac Catalyst.

## Sample

`GlazeTestPage` is the glaze sample. `HomePage` opens it with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through `NavigationBar`. It is not opened with `PushAsync`.

The page scrolls colored lines. The centered stack holds an opaque bar, the round plus, the glaze text button, the entry panel, and the dark title panel. The round glaze sits on that opaque bar. The mica buttons stay on `MicaTestPage`.

The dark title panel's hairline is a linear gradient. `StartPoint` is `(0, 0.5)` and `EndPoint` is `(1, 0.5)`. Offset `0` is white at alpha 46. Offset `1` is `#3D7EFF`. `StrokeThickness` is `2`. `CornerRadius` stays `22`. The other panels keep the default solid hairline.

## Out of scope

`MicaView`. A radial gradient. Animating the gradient stops. Reflect and repeat spread. A new public button type. Replacing the iOS material. A package version bump.

## Done when

- [x] On Android, a `GlazeView` on an opaque bar above a `ScrollView` stays the bar color while the scroll moves. The scrolled text does not show inside the glass.
- [x] On Android, a `GlazeView` that overlaps scrolling content, with no opaque view between them, still shows that content.
- [x] On iOS, both of those layouts match the behavior they have today.
- [x] The child of the panel is sharp. Tint, noise, stroke, and corner are unchanged.
- [x] The library and KitTests build for `net10.0` with no warnings. Android compiles. iOS compiles when the shared glaze files change. Windows is not required on the Mac.
- [ ] `Stroke(Color)` still paints the solid hairline it paints today, on iOS and on Android.
- [ ] A left-to-right `LinearGradientBrush` paints that same rounded hairline on iOS, on Android, and on Mac Catalyst.
- [ ] `StrokeThickness` of `0` draws no gradient.
- [ ] A radial brush paints the default solid hairline.
- [ ] The dark title panel on `GlazeTestPage` shows the sample gradient above.
- [ ] The library and KitTests build for `net10.0` with no warnings. Android compiles. iOS compiles. Windows is not required on the Mac.
