# Sheet

This document describes `SheetView`, and the `PageSheet` and `FormSheet` factories.

iOS does not have a separate bottom-sheet type. `UISheetPresentationController` is the sheet behind both `UIModalPresentationStyle.pageSheet` and `UIModalPresentationStyle.formSheet`. A bottom sheet is that same sheet with more than one detent and, usually, a grabber. SwiftUI's `.sheet` is the page sheet. `.presentationDetents` and `.presentationDragIndicator` turn it into a bottom sheet. The guide is [Sheets](https://developer.apple.com/design/human-interface-guidelines/sheets).

The kit draws the sheet itself, on iOS, Mac Catalyst, Android, and Windows, so the same MAUI content is what every platform shows. Apple's `UISheetPresentationController` and Android's `BottomSheetBehavior` stay unused.

## Pieces

| Type | Role |
|---|---|
| `SheetView` | One floating sheet. Style, detents, grabber, and dimming are properties. |
| `PageSheet` | `SheetView` in the page style. One large detent. No grabber. |
| `FormSheet` | `SheetView` in the form style. A centered card on a regular width. A page sheet on a compact width. |

`PageSheet` and `FormSheet` are not separate controls. They set `Style` and the defaults below. A page that wants a bottom sheet uses either factory and then sets detents.

## SheetView

The public node is `SheetView`, a `Component`. The child is the sheet content and can be anything. The mounted element is an internal host, not a MAUI `Page`.

```csharp
PageSheet(
    VStack(
        Label("New message").FontSize(17),
        Editor()))
    .IsOpen(State.Open)
    .OnDismiss(Close);
```

```csharp
PageSheet(Player())
    .Detents(SheetDetent.Fraction("peek", 0.3), SheetDetent.Medium, SheetDetent.Large)
    .SelectedDetent(State.Detent)
    .OnDetentChanged(id => SetState(s => s.Detent = id))
    .Grabber(true)
    .UndimmedDetent("medium")
    .IsOpen(State.Open)
    .OnDismiss(Close);
```

The page owns `IsOpen` and `SelectedDetent`. The kit does not call `SetState`. `OnDismiss` runs when a swipe, a backdrop tap that is allowed to dismiss, the platform back gesture, or Escape closes the sheet. `OnDetentChanged` runs when a drag or a scroll settles on a different detent. Changing `SelectedDetent` while the sheet is open animates to that detent.

### Style

`Style` is `SheetStyle.Page` or `SheetStyle.Form`.

A page sheet on a phone is a card attached to the bottom edge. The large detent leaves the status bar visible. On a regular width, such as an iPad, the page style is an inset page: it uses a readable width (`PageSizing` true) and the large detent.

A form sheet on a regular width is a centered card. It uses `PreferredWidth` and `PreferredHeight`, default 540 by 620 points, and it is not attached to the bottom edge. On a compact width it adapts to the page sheet, which is what UIKit does with `formSheet` on an iPhone. Setting `Detents` on a form sheet keeps it edge-attached on that compact width and uses those detents.

`PageSheet()` is `Style(SheetStyle.Page)`. `FormSheet()` is `Style(SheetStyle.Form)`.

### Detents

A detent is a height where the sheet rests. The list is smallest to largest. It must contain at least one detent. The default is `SheetDetent.Large` only, which is the classic rigid page sheet.

| Detent | Rest height |
|---|---|
| `SheetDetent.Medium` | About half the sheet's full height. Inactive when the height size class is compact, such as a phone in landscape. The sheet then uses the next taller detent. |
| `SheetDetent.Large` | The fully expanded sheet. On a phone this stops under the status bar. |
| `SheetDetent.Fraction(id, 0.3)` | That fraction of the large detent's height, not of the screen. |
| `SheetDetent.Height(id, 120)` | That many points, clamped to the large detent. |

`Medium` and `Large` use the ids `"medium"` and `"large"`. A fraction or a height requires an id so `SelectedDetent` and `OnDetentChanged` can name it. Ids are case-sensitive. Two detents in one list cannot share an id.

`SelectedDetent` is the id. When it is null or missing from the list, the sheet opens at the largest detent. When the page removes the selected detent, the sheet animates to the nearest remaining one and raises `OnDetentChanged`.

The user can drag the sheet between detents. The drag tracks the finger, and the release snaps to the nearest detent. Pulling down past the smallest detent dismisses the sheet unless `DismissDisabled(true)`.

### Grabber, corner, and width

`Grabber` shows the drag pill at the top of the card. The default is false, which is the classic page and form sheet. A bottom sheet sets it true. The pill follows the page's light and dark theme.

`CornerRadius` is the card's top corners when the sheet is edge-attached, and every corner when it is a centered form card. The default is 10. Null keeps that default. A stacked sheet under another sheet uses the same radius.

`PageSizing` makes a regular-width sheet use a readable width instead of the full safe area. The default is true for the page style and false for the form style.

`EdgeAttachedInCompactHeight` attaches the sheet to the bottom edge when the height size class is compact. The default is true. False expands that sheet to the full screen.

`WidthFollowsPreferredSize` uses `PreferredWidth` for an edge-attached sheet on a regular width. The default is false, so the sheet is as wide as the safe area. It has no effect on a compact width, or when `EdgeAttachedInCompactHeight` is false. The form style's centered card always uses `PreferredWidth` and `PreferredHeight`.

### Dimming and the page behind

`UndimmedDetent` is the largest detent that does not dim the page behind the sheet. The default is null: every detent dims, and the dimmed page does not receive touches.

Set it to a detent id and every detent at or below that height leaves the page bright and interactive. A taller detent dims and locks it. This is `largestUndimmedDetentIdentifier`. A media player uses it so the page still works while the sheet rests at `medium`, and locks once the sheet reaches `large`.

`BackdropDismiss` is false. A tap on the dim does not close the sheet, which matches UIKit. True closes it and raises `OnDismiss`. A backdrop tap does nothing while that detent is undimmed, because the tap belongs to the page behind.

`DismissDisabled(true)` ignores the swipe, the backdrop, the platform back gesture, and Escape. The page closes the sheet by setting `IsOpen` false.

### Scrolling

`ScrollingExpands` is true. Scrolling the content to its top edge continues into a drag and expands the sheet to the next detent. False leaves scrolling inside the content. The user then resizes the sheet by dragging the grabber or the top of the card. This is `prefersScrollingExpandsWhenScrolledToEdge`.

### Keyboard and stacking

A focused field raises the sheet so the field stays above the keyboard. The selected detent does not change.

A sheet can open another sheet. The top sheet takes the drag. The sheet under it stays at its detent and keeps its corner radius. The kit does not add a navigation bar. The content can.

### Presentation

The sheet is a layer above the current page, including a modal page and an open `PopupView`. A `ScrollView` or a glaze corner cannot clip it. Opening a sheet does not push a route and does not detach the page, so an Android `GlazeView` on that page keeps its snapshot.

Shared code decides the rectangle from the style, the detents, the size class, the safe area, and the keyboard frame. Each platform only inserts the layer and reports those measurements:

| Platform | Where the layer is added |
|---|---|
| iOS 15+ and Mac Catalyst 17+ | The current page's `UIView` |
| Android 21+ | The current activity content root |
| Windows 10.0.17763+ | The current page's native root, in the tree, not compiled on the Mac |

The move between detents and the present and dismiss animations use a spring. `Duration` is 350 milliseconds. `Easing` is `CubicOut`.

## Defaults

| | Page | Form |
|---|---|---|
| Style | `SheetStyle.Page` | `SheetStyle.Form` |
| Detents | `Large` | none until the compact width adapts, then `Large` |
| Grabber | hidden | hidden |
| Corner radius | 10 | 10 |
| Undimmed detent | none | none |
| Backdrop dismiss | false | false |
| Dismiss disabled | false | false |
| Scrolling expands | true | true |
| Edge attached in compact height | true | true |
| Width follows preferred size | false | the centered card always uses the preferred size |
| Page sizing | true | false |
| Preferred size | unused | 540 by 620 |

## Files

- `src/ReactorKit/SheetView.cs` is the public node. `PageSheet` and `FormSheet` are factories on `Reactor.Kit.Extensions`, next to `GlazeView`.
- `src/ReactorKit/SheetTokens/` holds `SheetStyle`, `SheetDetent`, and the detent id constants. Namespace `Reactor.Kit.SheetTokens`.
- `src/ReactorKit/Internals/Sheet/` holds the presenter and the detent math. Namespace `Reactor.Kit.Internals.Sheet`.
- `Platforms/iOS/Sheet/`, `Platforms/Android/Sheet/`, and `Platforms/Windows/Sheet/` insert the layer. The iOS folder is shared with Mac Catalyst the same way glaze is.
- `UseReactorKit()` does not need a new flag when the presenter is created by the view rather than a handler.
- The README gets a short Sheet section: one sentence, the page-sheet example, the bottom-sheet example, and the supported platforms. Detent math stays in this document.

Callers add `global using Reactor.Kit.SheetTokens` beside the other kit usings.

## Sample

`HomePage` opens `SheetTestPage` with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through the shared bar. The sheets themselves are overlays on that page. They do not push.

The page has three buttons.

- "Page" opens a `PageSheet` whose content is a short form. One large detent, no grabber, dimmed page.
- "Form" opens a `FormSheet` at 540 by 620 on a regular width, and the adapted page sheet on a phone.
- "Player" opens a `PageSheet` with detents `Fraction("peek", 0.3)`, `Medium`, and `Large`, the grabber visible, and `UndimmedDetent("medium")`.

## Out of scope

A popover. A full-screen cover, which is `PushModalAsync`. Replacing MAUI modal presentation. A `UISheetPresentationController` or `BottomSheetBehavior` backend. A navigation bar supplied by the sheet. A package version bump until the control merges.

## Done when

- [ ] `PageSheet` shows caller content above the current page, including a modal page, at the large detent, with the page dimmed and not interactive.
- [ ] A swipe down, the platform back gesture, and Escape dismiss it and raise `OnDismiss` once. A backdrop tap does not.
- [ ] `FormSheet` is a centered 540 by 620 card on a regular width and a page sheet on a compact width.
- [ ] A sheet with `Fraction`, `Medium`, and `Large` snaps between those heights. `Medium` is skipped when the height size class is compact.
- [ ] `Grabber(true)` draws the pill. `UndimmedDetent` leaves the page behind interactive at that detent and below, and dims it above.
- [ ] `ScrollingExpands(false)` scrolls the content instead of growing the sheet. Dragging the grabber still changes detents.
- [ ] The page can set `SelectedDetent` and the sheet animates there. A user drag raises `OnDetentChanged` and does not call `SetState`.
- [ ] `SheetTestPage` shows the page sheet, the form sheet, and the player sheet.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
