# Mica

This document describes `MicaView`.

`GlazeView` is a container. iOS and Mac Catalyst put a `UIVisualEffectView` behind the child. Android snapshots the page and blurs that bitmap. Windows uses an `AcrylicBrush` on the backdrop. A blur radius is an on/off switch on Apple and a dp gaussian on Android. The same call does not match.

`MicaView` paints one mica material with SkiaSharp. iOS, Mac Catalyst, Android, and Windows run that same paint. The view is a background layer. The page stacks its own content on top and owns the tap.

The recipe follows glaze: tint, noise, hairline, and a soft shadow. The fill is a vertical gradient. There is no backdrop sample, so the layer stays the same over a photo and over a flat page.

`UIVisualEffectView`, Android `RenderEffect`, and `AcrylicBrush` stay unused.

## Pieces

| Type | Role |
|---|---|
| `MicaView` | The painted layer. No children. |
| `MicaView()` | Factory on `Reactor.Kit.Extensions`. |

`MicaBackground` and `MicaPlusButton` are not kit types. A plus button is a `Grid` the page writes.

## MicaView

The public node is `MicaView`, a `View<SKCanvasView>`. `OnMount` creates an internal `SKCanvasView`. `UseReactorKit()` already calls `UseSkiaSharp()`. There is no handler and no platform folder.

```csharp
Grid(
    MicaView()
        .HFill()
        .VFill()
        .TintColor(Color.FromArgb("#2E2E32"))
        .TintOpacity(0.78)
        .ShadeColor(Color.FromArgb("#1A1A1D"))
        .ShadeOpacity(0.69)
        .CornerRadius(28)
        .StrokeThickness(1.2),
    Label("+")
        .FontSize(28)
        .FontAttributes(FontAttributes.Bold)
        .TextColor(Colors.White)
        .HCenter()
        .VCenter()
        .InputTransparent(true))
.WidthRequest(56)
.HeightRequest(56)
.OnTapped(action);
```

The grid is 56 by 56. The layer fills it. `CornerRadius` 28 rounds the inset shape into a circle. The label sits in the same cell and does not take the tap. `OnTapped` is on the grid.

The canvas sets `InputTransparent` to true in its constructor and at the start of every paint. The layer does not take the tap. Content that should let the tap through, such as the label, still sets `InputTransparent` on itself.

Each color and each double has a value method and a `Func<T>` method that takes `IComponentWithState? state = null`. Each double also takes an optional `RxDoubleAnimation`, the same way `GlazeView` does. The canvas invalidates when a bound value changes.

The ref callback receives the mounted `SKCanvasView`, and null when the view unmounts.

```csharp
MicaView(canvas => _canvas = canvas)
    .CornerRadius(22);
```

## Paint

The canvas clears to transparent. Lengths on the public API are dp. The paint multiplies them by `info.Width / Width` when `Width` is greater than 0. `IgnorePixelScaling` stays false.

The shape is a round rect inset by 4 dp on every side, so the shadow blur has room inside the view. The view clips to its bounds. A glow past that inset is cut. A width or height of 0, or an inset rect that is empty, clears and returns.

`CornerRadius` is the corner of that inset rect, in dp. The paint clamps it to half the shorter side. A square view whose radius meets that clamp is a circle. The paint uses a round rect for every radius, including the circle.

Draw order:

1. Shadow. The same round rect, filled, no offset. Black at `ShadowOpacity`. Blur sigma is 10 dp. `ShadowOpacity` 0 skips this step.
2. Fill. A vertical linear gradient from the top of the rect to the bottom, tile mode clamp. The top stop is `TintColor`'s RGB at `TintOpacity`. The bottom stop is the shade RGB at the shade opacity. Color alpha on `TintColor` and `ShadeColor` is ignored.
3. Noise. Glaze's `NoiseTile`, 128 by 128, tiled. Drawn in the round rect at `NoiseOpacity`. `NoiseOpacity` 0 skips this step.
4. Stroke. The same round rect. `Stroke` at `StrokeThickness` dp. A thickness of 0 skips this step.

Antialiasing is on for the shadow, the fill, and the stroke.

When `ShadeColor` is null, the bottom RGB is the top RGB multiplied by 0.94. When `ShadeOpacity` is null, the bottom alpha is `TintOpacity * 0.88`. The dark plus button sets both, so the stops are `#2E2E32` at 0.78 and `#1A1A1D` at 0.69.

## Defaults

The unset layer is a light mica panel. It uses glaze's base, glaze's hairline, and glaze's dusting. The wash is stronger than glaze's 0.28 because nothing behind the view supplies the milk.

| Member | Default | Role |
|---|---|---|
| `TintColor` | `#F3F3F3` | Top stop. Glaze's light mica base. |
| `TintOpacity` | `0.76` | Top alpha. Glaze's no-blur fallback. |
| `ShadeColor` | null | Bottom stop. Null uses 94% of the top RGB. |
| `ShadeOpacity` | null | Bottom alpha. Null uses `TintOpacity * 0.88`. |
| `NoiseOpacity` | `0.015` | Fine dusting. The plus-button sketch left this at the default. |
| `Stroke` | white, alpha 46 | Hairline color. Same as glaze. |
| `StrokeThickness` | `1` | Hairline width, in dp. The plus-button sketch uses `1.2`. |
| `CornerRadius` | `0` | Corner of the inset shape, in dp. |
| `ShadowOpacity` | `60 / 255` | Black glow. `0` draws no shadow. |

There is no `BlurRadius`. The view does not blur the backdrop.

## Files

| Piece | Path |
|---|---|
| Public node | `src/ReactorKit/MicaView.cs` |
| Factory | `src/ReactorKit/Extensions.Components.cs`, next to `GlazeView` |
| Canvas and tokens | `src/ReactorKit/Internals/Mica/` |
| Test page | `samples/KitTests/Components/MicaTestPage.cs` |

`MicaView()` and `MicaView(Action<SKCanvasView?> refAction)` are the factories. Neither takes children.

The canvas type is internal, one type per file, namespace `Reactor.Kit.Internals.Mica`. Tokens are internal. No `MicaViewTokens` namespace, and no new global using.

No files under `Platforms/`. No handler registration.

## Sample

`HomePage` gains a Mica button under the timeline button, same chrome, margin `(16, 502)`. It opens `MicaTestPage` with `PushModalAsync(animated: false)`. The page closes through `NavigationBar` with `PopModalAsync(animated: false)`.

The page shows two layers on a flat background:

- The plus button from the call above. A tap adds one to a count the page owns. The count is a label under the button.
- A light rounded rect, 240 by 72, default tint, `CornerRadius` 22, with a label centered on top of it.

## Out of scope

Replacing `GlazeView`, or routing glaze through this canvas. Sampling the views behind the layer. A public button, chip, or card. Hosting a child inside `MicaView`. A platform handler. Liquid glass. A package version bump.

## Done when

- [ ] `MicaView()` builds from a MauiReactor page and fills the cell the page gives it.
- [ ] The plus button draws a circle, a dark vertical wash, a soft shadow, and a 1.2 dp hairline. The label stays sharp. A tap reaches the grid.
- [ ] The light panel uses the defaults and reads as the same family as glaze's light tint, noise, and hairline.
- [ ] iOS and Android draw that same circle and that same panel. Neither blurs the page behind the layer.
- [ ] `ShadeColor` null darkens the top RGB by 6%. `ShadeOpacity` null uses 88% of `TintOpacity`.
- [ ] `ShadowOpacity` 0 and `NoiseOpacity` 0 skip those steps. `StrokeThickness` 0 skips the hairline.
- [ ] `net10.0`, `net10.0-ios`, and `net10.0-android` build with 0 warnings.
