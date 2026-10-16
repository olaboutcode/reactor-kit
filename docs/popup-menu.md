# Popup menu

This document describes `PopupView` and `PopupMenu`.

iOS draws a pop-up button and a pull-down button from the same menu. `PopupMenu` is that menu. The page supplies the anchor, and a row is a title or any view. The guide is [Pop-up buttons](https://developer.apple.com/design/human-interface-guidelines/pop-up-buttons).

The kit draws the panel itself. Apple platforms do not host `UIMenu`. Android does not host a `Picker` or a spinner dialog. The same MAUI content is what every platform shows.

## Pieces

| Type | Role |
|---|---|
| `PopupView` | Anchors a floating `ContentView` to a control. The content can be anything. |
| `PopupMenu` | Pop-up menu on a `PopupView`. Rows are titles or any view. The page owns open. |

`PopupMenu` uses `PopupView`. A page that wants another panel uses `PopupView` directly and supplies its own content.

## PopupView

The public node is `PopupView`, a `View<Microsoft.Maui.Controls.ContentView>`. The mounted element is the anchor host. The ref callback receives that `ContentView`, and null when it unmounts. The floating panel is a second `ContentView` and is not part of the public surface type.

The anchor is the child. It stays in normal layout. The panel is `Content`.

```csharp
PopupView(Button("Repeat"))
    .Content(GlazeView(Label("Anything").Center()).Padding(16).CornerRadius(14))
    .IsOpen(State.Open)
    .OnDismiss(() => SetState(s => s.Open = false));
```

The page owns `IsOpen`. A tap on the anchor does not open the panel by itself. The page opens it. `OnDismiss` runs when the page taps outside the panel, or when the platform back gesture or the Escape key closes it. The kit does not call `SetState`.

`Content` accepts any MauiReactor node. An empty content still shows the open chrome, so the shell can be tried before a menu exists.

### Placement

The panel prefers to open below the anchor. `Gap` is that space, in points, and the default is 8. The same value insets the panel inside the safe area. When the space below is shorter than the panel, it opens above the anchor. It then shifts horizontally so it stays inside the window.

```csharp
PopupView(Button("Open")).Gap(12)
```

`Alignment` is `MenuAlignment.Trailing`, `Leading`, or `Center`. The default is trailing: the panel's trailing edge lines up with the anchor's trailing edge, which is how the Notes overflow menu sits. Center matches a menu that opens over a value in the middle of a row. Leading lines up the leading edges.

The panel width is the measured content width. `MinWidth` raises that width, and the default is 220. `MinWidth(0)` leaves the measured width, so the panel hugs the content. Either way the width stops at the window minus the insets. The height hugs the content, down to `MinHeight` (44pt). Past the space that remains on screen, the content scrolls and the panel stays inside the insets.

While the panel is open it tracks the anchor. A scroll that moves the anchor moves the panel. If the anchor is no longer on screen, the panel dismisses and `OnDismiss` runs.

### Presentation

The panel is parented above the current page, including a modal page, so a `ScrollView`, a glaze corner, or another parent cannot clip it. A clear layer covers that page and takes the outside tap. The page behind does not dim.

The open animation is a fade plus a scale to 1 over `Duration` milliseconds (180), originating at the anchor edge the panel grows away from. `OpenScaleX` and `OpenScaleY` are the starting scales, both 0.96. `OpenScale` sets both. The curve is `Easing`, and the default is `CubicOut`. Close fades the panel out and returns each axis to its starting scale, then removes the layer.

Hit testing belongs to the clear layer and the panel. Touches on the anchor while the panel is open dismiss it, the same way a second tap on an iOS menu button closes the menu.

Shared code decides the rectangle. Each platform only inserts the layer into the page's native root and reports the anchor's window location:

| Platform | Where the layer is added |
|---|---|
| iOS 15+ and Mac Catalyst 17+ | The current page's `UIView` |
| Android 21+ | The current activity content root |
| Windows 10.0.17763+ | The current page's native root, in the tree, not compiled on the Mac |

No new package. Android's `PopupWindow` and Apple's `UIMenu` stay unused.

## PopupMenu

A pop-up menu built on `PopupView`. The page supplies the anchor and owns `IsOpen`. A row is a title, any view, or a hairline.

```csharp
PopupMenu()
    .Anchor(Button("Custom Menu", Open))
    .Items(menu => menu
        .Item("Custom Menu Item 1")
        .Item("Custom Menu Item 2", text => { })
        .Rule()
        .Item(Label("Custom")))
    .Panel(panel => panel.CornerRadius(18).Padding(12))
    .Stack(stack => stack.Spacing(4))
    .Popup(popup => popup.OpenScaleX(0.85).OpenScaleY(0.5))
    .IsOpen(State.Open)
    .OnDismiss(() => SetState(s => s.Open = false))
    .TitleColor(Colors.Black)
    .RuleColor(Color.FromRgba(60, 60, 67, 74))
    .DisableAnchorWhenOpen();
```

`Items` takes a list in `Reactor.Kit.PopupMenuTokens`. `Item(text)` and `Item(text, onTap)` are title buttons. `Item(view)` and `Item(view, onTap)` show that view. `Rule()` is a hairline. Each call returns the list, so the rows are one chain. A tap runs `OnDismiss`, then the row callback. `TitleColor` styles title rows and defaults to black. `RuleColor` styles the hairline and defaults to `rgba(60, 60, 67, 74)`. A custom view keeps the colors the page set on it.

The panel is a `GlazeView`, padding 8, corner radius 14, with a black shadow at offset `(0.3, 0.3)`, radius 4, and opacity 0.28. The row stack spacing is 8. `Panel` receives that glaze view and `Stack` receives that vertical stack, so any of their setters can replace a default. `Popup` receives the `PopupView` after the anchor, the panel, the anchor dim, and the open state are set, so it can change the scales, the easing, the duration, the gap, and the other popup settings. Title rows are 44pt, corner radius 12, on a transparent background. The anchor is the page's own control. `DisableAnchorWhenOpen` leaves that control in place. While the panel opens it fades to 40% opacity, and while the panel closes it fades back, using the popup duration and easing. It does not take taps while the panel is showing. The default leaves the anchor enabled.

## Menu chrome

| Piece | Value |
|---|---|
| Panel corner radius | 14 |
| Panel padding | 8 |
| Row stack spacing | 8 |
| Title row height | 44 |
| Title row corner radius | 12 |
| Title color | black |
| Custom row minimum height | 44 |
| Custom row corner radius | 12 |
| Custom row padding | 4 |
| Hairline | 1pt, `rgba(60, 60, 67, 74)`, horizontal margin 16 |
| Panel shadow | black, offset `(0.3, 0.3)`, radius 4, opacity 0.28 |

Glaze supplies the material. The default tint is the glaze default, so a light page gets a light panel and a dark page gets a dark panel. A page that wants another tint sets `TintColor` on the glaze through `Panel`. The menu does not add a tint prop of its own.

## Files

- `src/ReactorKit/PopupView.cs` and `PopupMenu.cs` are the public nodes.
- `src/ReactorKit/PopupMenuTokens/PopupMenuItem.cs` and `PopupMenuItems.cs`. `MenuAlignment` can live beside them.
- `src/ReactorKit/Internals/Popup/` holds the presenter and the placement math. Namespace `Reactor.Kit.Internals.Popup`.
- `Platforms/iOS/Popup/`, `Platforms/Android/Popup/`, and `Platforms/Windows/Popup/` insert the layer. The iOS folder is shared with Mac Catalyst the same way glaze is.
- Factories go on `Reactor.Kit.Extensions`, next to `GlazeView`. `UseReactorKit()` does not need a new flag when the presenter is created by the view rather than a handler.
- The README gets a short Popup section: one sentence, the menu example, and the supported platforms. Placement math stays in this document.

Callers add `global using Reactor.Kit.PopupMenuTokens` beside the other kit usings.

## Sample

`HomePage` opens `MenuTestPage` with `PushModalAsync`, the same way it opens the other sample pages. The page shows a `PopupMenu` with the title "Plain text", an icon row, a profile row, and one custom view, separated by hairlines, and one `PopupView` whose content is a glaze panel labeled "Anything". The menu calls `Panel` to keep padding 8 and set `ShadowElevation` level 2, offset `(0.5, 0.5)`, radius 5, opacity 0.30. The Anything panel shadow is offset `(0, 1)`, radius 4, opacity 0.28. The labels are the sample's own words.

## Out of scope

Nested submenus and the trailing chevron. Multi-select. Long-press context menus. Keyboard arrowing through rows. A horizontal icon strip. Swapping MAUI's `Picker` or `MenuFlyout` out of the framework. A `UIMenu` backend on Apple platforms. A package version bump until the controls merge.

## Done when

- [ ] `PopupView` shows caller content above the current page, including a modal page, and a parent `ScrollView` does not clip it.
- [ ] The panel opens below the anchor, flips above when the space is short, and stays inside the safe area.
- [ ] An outside tap, a second tap on the anchor, and the Android back gesture dismiss it and raise `OnDismiss` once.
- [ ] `PopupMenu` shows title rows and custom views, and a tap closes the menu and runs the row callback.
- [ ] A parent render that does not change the items leaves an open menu open.
- [ ] `MenuTestPage` shows the pop-up menu and one free-form `PopupView`.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
