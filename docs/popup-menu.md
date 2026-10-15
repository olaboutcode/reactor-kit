# Popup menu and pull-down menu

This is the plan for issue 11. `PopupView` and `PopupMenu` are in the library. `PullDownMenu` is not. After the pull-down menu ships, this document describes the behavior that shipped.

iOS is the reference for what the controls do. UIKit builds both from a button and a `UIMenu`. A pop-up button turns on `showsMenuAsPrimaryAction` and `changesSelectionAsPrimaryAction`, marks one action `on`, and shows that action as the button title. A pull-down button turns on `showsMenuAsPrimaryAction`, leaves `changesSelectionAsPrimaryAction` off, and keeps the button's own title while the menu runs commands. The guides are [Pop-up buttons](https://developer.apple.com/design/human-interface-guidelines/pop-up-buttons) and [Pull-down buttons](https://developer.apple.com/design/human-interface-guidelines/pull-down-buttons).

The kit draws the panel itself. Apple platforms do not host `UIMenu`. Android does not host a `Picker` or a spinner dialog. The same MAUI content is what every platform shows.

## Pieces

| Type | Role |
|---|---|
| `PopupView` | Anchors a floating `ContentView` to a control. The content can be anything. |
| `PopupMenu` | Pop-up menu on a `PopupView`. Rows are titles or any view. The page owns open. |
| `PullDownMenu` | Pull-down button. The button label stays put. Rows run actions. |

`PopupMenu` and `PullDownMenu` use `PopupView`. A page that wants another panel uses `PopupView` directly and supplies its own content.

Ship `PopupView`, then `PopupMenu`. `PullDownMenu` comes after the pop-up menu is in place. Both menus are in this issue.

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

The panel width hugs the content, down to `MinWidth` (220pt) and up to the window width minus the insets. The height hugs the content, down to `MinHeight` (44pt). Past the space that remains on screen, the content scrolls and the panel stays inside the insets.

While the panel is open it tracks the anchor. A scroll that moves the anchor moves the panel. If the anchor is no longer on screen, the panel dismisses and `OnDismiss` runs.

### Presentation

The panel is parented above the current page, including a modal page, so a `ScrollView`, a glaze corner, or another parent cannot clip it. A clear layer covers that page and takes the outside tap. The page behind does not dim.

The open animation is a fade plus a scale from `OpenScale` (0.96) to 1 over `Duration` milliseconds (180), originating at the anchor edge the panel grows away from. Close reverses it, then removes the layer.

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
    .IsOpen(State.Open)
    .OnDismiss(() => SetState(s => s.Open = false))
    .TitleColor(Colors.Black)
    .RuleColor(Color.FromRgba(60, 60, 67, 74))
    .DisableAnchorWhenOpen(true);
```

`Items` takes a list in `Reactor.Kit.PopupMenuTokens`. `Item(text)` and `Item(text, onTap)` are title buttons. `Item(view)` and `Item(view, onTap)` show that view. `Rule()` is a hairline. Each call returns the list, so the rows are one chain. A tap runs `OnDismiss`, then the row callback. `TitleColor` styles title rows and defaults to black. `RuleColor` styles the hairline and defaults to `rgba(60, 60, 67, 74)`. A custom view keeps the colors the page set on it.

The panel is a `GlazeView`, padding 8, corner radius 14, with the same small shadow as the sample's free-form panel. The row stack spacing is 8. `Panel` receives that glaze view and `Stack` receives that vertical stack, so any of their setters can replace a default. Title rows are 44pt, corner radius 12, on a transparent background. The anchor is the page's own control. `DisableAnchorWhenOpen` leaves that control in place and disables it only while the panel is showing. It draws at 40% opacity and does not take taps. The default leaves the anchor enabled.

## PullDownMenu

A pull-down button. The child is the anchor and keeps whatever label or icon the page gave it. Choosing a row does not change that child.

```csharp
PullDownMenu(Button("More"))
    .Sections(new[]
    {
        new[]
        {
            new PullDownMenuItem("find", "Find in note"),
            new PullDownMenuItem("move", "Move note"),
        },
        new[]
        {
            new PullDownMenuItem("delete", "Delete", Destructive: true),
        },
    })
    .OnInvoked(id => SetState(s => s.Last = id));
```

`PullDownMenuItem` lives in the same token namespace: `Id`, `Title`, optional `ImageSource` `Icon`, `Destructive` (default false), and `Enabled` (default true). A duplicate id throws. Sections are lists of lists. An empty section is skipped.

`OnInvoked` receives the id, then the menu closes. A disabled row does neither. There is no selected id and no checkmark column.

A destructive row draws its title in `#FF3B30`. An icon, when set, is 20pt on the leading side. Rows without an icon leave that column empty so the titles still line up inside one menu.

The top icon cluster in the Notes screenshot (Scan, Pin Note, Lock in one row) is custom `PopupView` content. `PullDownMenu` does not grow a horizontal action strip in this issue.

## Shared menu look

This table is the later checkmark list and the pull-down list. The current `PopupMenu` uses the simpler chrome in the PopupMenu section.

| Piece | Value |
|---|---|
| Panel corner radius | 14 |
| Panel padding | 6 |
| Row height | 44 |
| Row corner radius while pressed | 10 |
| Title size | 17 |
| Horizontal row padding | 16 |
| Checkmark column | 22pt, leading, pop-up rows only |
| Icon column | 22pt, leading, pull-down rows only |
| Separator | 1pt hairline between sections |
| Panel shadow | offset `(0, 8)`, radius 24, opacity 0.18 |

The checkmark is a stroked path, the same idea as the sample back chevron, so Android does not need a symbol font. The pressed row is a 12% black wash in light appearance and a 16% white wash in dark appearance. Glaze supplies the material. The default tint is the glaze default, so a light page gets a light panel and a dark page gets a dark panel. A page that wants the Calendar black panel sets `TintColor` later. The first menus do not add a tint prop of their own. They use the glaze default.

## Files

- `src/ReactorKit/PopupView.cs`, `PopupMenu.cs`, and `PullDownMenu.cs` are the public nodes.
- `src/ReactorKit/PopupMenuTokens/PopupMenuItem.cs`, `PopupMenuItems.cs`, and `PullDownMenuItem.cs`. `MenuAlignment` can live beside them.
- `src/ReactorKit/Internals/Popup/` holds the presenter and the placement math. Namespace `Reactor.Kit.Internals.Popup`.
- `Platforms/iOS/Popup/`, `Platforms/Android/Popup/`, and `Platforms/Windows/Popup/` insert the layer. The iOS folder is shared with Mac Catalyst the same way glaze is.
- Factories go on `Reactor.Kit.Extensions`, next to `GlazeView`. `UseReactorKit()` does not need a new flag when the presenter is created by the view rather than a handler.
- The README gets a short Popup section when the controls exist: one sentence, the two menu examples, and the supported platforms. Placement math stays in this document.

Callers add `global using Reactor.Kit.PopupMenuTokens` beside the other kit usings.

## Sample

`MenuTestPage` opens from the glaze page the same way the text pages open. The page shows a `PopupMenu` with a title, an icon row, a profile row, and one custom view, and one `PopupView` whose content is a glaze panel with a single label. The labels are the sample's own words. The pull-down menu is not on the page yet.

## Out of scope

Nested submenus and the trailing chevron. Multi-select. Long-press context menus. Keyboard arrowing through rows. A horizontal icon strip inside `PullDownMenu`. Swapping MAUI's `Picker` or `MenuFlyout` out of the framework. A `UIMenu` backend on Apple platforms. A package version bump until the controls merge.

## Done when

- [ ] `PopupView` shows caller content above the current page, including a modal page, and a parent `ScrollView` does not clip it.
- [ ] The panel opens below the anchor, flips above when the space is short, and stays inside the safe area.
- [ ] An outside tap, a second tap on the anchor, and the Android back gesture dismiss it and raise `OnDismiss` once.
- [ ] `PopupMenu` shows title rows and custom views, and a tap closes the menu and runs the row callback.
- [ ] `PullDownMenu` leaves the anchor label unchanged, runs `OnInvoked`, and draws a destructive row in red.
- [ ] A parent render that does not change the items leaves an open menu open.
- [ ] `MenuTestPage` shows the pop-up menu and one free-form `PopupView`.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
