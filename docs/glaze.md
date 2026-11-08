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
| `Stroke` | white, alpha 46 | Hairline color. |
| `StrokeThickness` | `1` | Hairline width, in dp. `0` draws nothing. |
| `CornerRadius` | `0` | Rounds the panel. The caller's shadow follows this corner. |

On Android the material fill, in addition to the wash, is `#F3F3F3` at 44% in light mode and `#1C1C1E` at 55% in dark mode. The fallback wash, when blur is unavailable, is at least 76%.

A shadow is the caller's `Shadow`. This view does not add one.

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

The page scrolls colored lines. A glaze banner at the top overlaps that scroll and has no opaque plate of its own, so the lines stay visible in the banner while they move. An opaque bar, the page background color, sits above the scroll and does not overlap it. A fully rounded `GlazeView` sits on that bar. Scrolling the lines does not paint them into that circle. The circle stays the bar color, plus the wash, the noise, and the hairline.

The centered samples stay: the round plus, the glaze text button, the entry panel, and the dark title panel.

## Out of scope

`MicaView`. A new public button type. Replacing the iOS material. A package version bump.

## Done when

- [x] On Android, a `GlazeView` on an opaque bar above a `ScrollView` stays the bar color while the scroll moves. The scrolled text does not show inside the glass.
- [x] On Android, a `GlazeView` that overlaps scrolling content, with no opaque view between them, still shows that content.
- [x] On iOS, both of those layouts match the behavior they have today.
- [x] The child of the panel is sharp. Tint, noise, stroke, and corner are unchanged.
- [x] The library and KitTests build for `net10.0` with no warnings. Android compiles. iOS compiles when the shared glaze files change. Windows is not required on the Mac.
