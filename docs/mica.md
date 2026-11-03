# Mica

This document describes `MicaView`.

`GlazeView` is a container. iOS and Mac Catalyst put a `UIVisualEffectView` behind the child. Android snapshots the page and blurs that bitmap. Windows uses an `AcrylicBrush` on the backdrop. A blur radius is an on/off switch on Apple and a dp gaussian on Android. The same call does not match.

`MicaView` paints one mica material with SkiaSharp. iOS, Mac Catalyst, Android, and Windows run that same paint. The view is a background layer. The page stacks its own content on top and owns the tap.

The recipe follows glaze: tint, noise, and a hairline. The fill is a vertical gradient. The fill reaches the edge of the view. There is no backdrop sample, so the layer stays the same over a photo and over a flat page. A shadow belongs on the container around the layer, such as a `Border`.

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
        .CornerRadius(28),
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

The grid is 56 by 56. The layer fills it. `CornerRadius` 28 rounds that box into a circle. The fill reaches the edge. The label sits in the same cell and does not take the tap. `OnTapped` is on the grid.

The canvas sets `InputTransparent` to true in its constructor and at the start of every paint. The layer does not take the tap. Content that should let the tap through, such as the label, still sets `InputTransparent` on itself.

Each color and each double has a value method and a `Func<T>` method that takes `IComponentWithState? state = null`. Each double also takes an optional `RxDoubleAnimation`, the same way `GlazeView` does. The canvas invalidates when a bound value changes.

The ref callback receives the mounted `SKCanvasView`, and null when the view unmounts.

```csharp
MicaView(canvas => _canvas = canvas)
    .CornerRadius(22);
```

## Paint

The canvas clears to transparent. Lengths on the public API are dp. The paint multiplies them by `info.Width / Width` when `Width` is greater than 0. `IgnorePixelScaling` stays false.

The shape is a round rect of the canvas bounds. The fill reaches the edge of the view. A width or height of 0 clears and returns.

`CornerRadius` is the corner of that rect, in dp. The paint clamps it to half the shorter side. A square view whose radius meets that clamp is a circle. The paint uses a round rect for every radius, including the circle.

Draw order:

1. Fill. A vertical linear gradient from the top of the rect to the bottom, tile mode clamp. The top stop is `TintColor`'s RGB at `TintOpacity`. The bottom stop is the shade RGB at the shade opacity. Color alpha on `TintColor` and `ShadeColor` is ignored.
2. Noise. Glaze's `NoiseTile`, 128 by 128, tiled. Drawn in the round rect at `NoiseOpacity`. `NoiseOpacity` 0 skips this step.
3. Stroke. The hairline. The default width is one physical pixel, `1 / density` dp, on iOS and Android. @2x is 0.5 dp. @3x is about 0.33 dp. A set `StrokeThickness` is dp. The path is inset by half that thickness, so the outer edge of the stroke is the view edge and the whole stroke stays inside. A thickness of 0 skips this step.

Antialiasing is on for the fill and the stroke.

When `ShadeColor` is null, the bottom RGB is the top RGB multiplied by 0.94. When `ShadeOpacity` is negative, including the default, the bottom alpha is `TintOpacity * 0.88`. The property is a `double` so it can animate. The dark plus button sets both stops, so they are `#2E2E32` at 0.78 and `#1A1A1D` at 0.69.

## Defaults

The unset layer is a light translucent panel. It uses glaze's base, glaze's hairline, and glaze's dusting. The wash is `0.5`, so content behind the layer shows through.

| Member | Default | Role |
|---|---|---|
| `TintColor` | `#F3F3F3` | Top stop. Glaze's light mica base. |
| `TintOpacity` | `0.5` | Top alpha. The layer stays translucent. |
| `ShadeColor` | null | Bottom stop. Null uses 94% of the top RGB. |
| `ShadeOpacity` | negative | Bottom alpha. A negative value uses `TintOpacity * 0.88`. |
| `NoiseOpacity` | `0.015` | Fine dusting. The plus-button sketch left this at the default. |
| `Stroke` | white, alpha 46 | Hairline color. Same as glaze. |
| `StrokeThickness` | one physical pixel | `1 / density` dp. The same hairline on iOS and Android. A positive value is dp. |
| `CornerRadius` | `0` | Corner of the view, in dp. |

There is no `BlurRadius` and no shadow. The view does not blur the backdrop. The edge is the hairline. A page that wants a shadow wraps the layer in a `Border` or another container and sets `Shadow` on that container. The container corner matches `CornerRadius`.

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

The page scrolls lines behind the mica, the same arrangement as the glaze samples on Home. The lines move. The mica stays.

- A bar at the top of the content is 64 tall, uses the default mica, and reads "Text scrolls behind".
- The plus button from the call above stays centered, inside a `Border` with the same corner and `ShadowElevation` level 1. A tap adds one to a count the page owns. The count is a label under the button.
- A light rounded rect, 240 by 72, default tint, `CornerRadius` 22, sits under the count with a label centered on top of it.

## Out of scope

Replacing `GlazeView`, or routing glaze through this canvas. Sampling the views behind the layer. A shadow painted by this view. A public button, chip, or card. Hosting a child inside `MicaView`. A platform handler. Liquid glass. A package version bump.

## Done when

- [ ] `MicaView()` builds from a MauiReactor page and fills the cell the page gives it.
- [ ] The plus button draws a circle that fills the 56 box, a dark vertical wash, and a one-pixel hairline. The label stays sharp. A tap reaches the grid.
- [ ] The light panel uses the defaults and reads as the same family as glaze's light tint, noise, and hairline.
- [ ] iOS and Android draw that same circle and that same panel. Neither blurs the page behind the layer.
- [ ] `ShadeColor` null darkens the top RGB by 6%. A negative `ShadeOpacity` uses 88% of `TintOpacity`.
- [ ] `NoiseOpacity` 0 skips the dusting. `StrokeThickness` 0 skips the hairline.
- [x] `net10.0`, `net10.0-ios`, and `net10.0-android` build with 0 warnings.
