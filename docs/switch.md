# Switch

This document describes `SwitchView`.

A switch is one pill and one thumb. People turn a single setting on or off, and the result is immediate. The guides for the three familiar looks are [Toggles](https://developer.apple.com/design/human-interface-guidelines/toggles), [Switch](https://m3.material.io/components/switch/overview), and [Toggle switches](https://learn.microsoft.com/en-us/windows/apps/design/controls/toggles).

The kit draws one switch, on iOS, Mac Catalyst, Android, and Windows. `UISwitch`, the Material `Switch`, and the Windows `ToggleSwitch` stay unused. There is no Material type, no Cupertino type, and no Fluent type. Those looks are the same view with different part values.

## SwitchView

The public node is `SwitchView`, a `Component`. The label is any view. The mounted element is the row.

```csharp
SwitchView()
    .Label("Wi-Fi")
    .IsOn(State.Wifi)
    .OnChanged(on => SetState(s => s.Wifi = on));
```

The page owns `IsOn`. A tap on the pill toggles it. A horizontal drag follows the finger and, on release, turns on when the thumb is past the midpoint. `OnChanged` runs once, with the new value. It does not run when the new value equals the current one, and it does not run when the page sets `IsOn`. Setting `IsOn` animates the thumb to that side.

A tap on the label does not toggle the switch. The label is a custom view and keeps its own taps.

`IsEnabled` is true. False ignores the tap and the drag, and the row draws at `DisabledOpacity` (0.38).

The on position is the trailing side. A right-to-left layout mirrors the pill. The thumb travels to the leading side to turn on.

The pill's hit box is at least 44 by 44. The track is centered in that box. The row is as tall as the label when the label is taller, and the pill stays vertically centered.

The thumb moves, grows, and the track color changes over `Duration` milliseconds (200). `Easing` is `CubicOut`.

## Parts

`Label` takes a view. `Label(string)` puts that string in a 17pt label and leaves the text color to the theme. One slot: a later call replaces the earlier one. `LabelSpacing` is 12. `LabelLeading` puts the label at the start and the pill at the end. That is the default. `LabelTrailing` puts the pill first. A later side call replaces an earlier one. With a width on the row, the label takes the remaining space. With no width, the row hugs the label, the gap, and the track.

The track is a capsule. `TrackCornerRadius` defaults to half `TrackHeight`. Each of fill, stroke color, and stroke thickness has an on value and an off value. A stroke thickness of zero draws no stroke.

The thumb is a circle, vertically centered on the track. Off, it sits `ThumbInset` from the leading edge. On, it sits `ThumbInset` from the trailing edge. `ThumbInset` defaults to `(TrackHeight - thumbSize) / 2`, using the thumb size for the side it rests on. While a finger is down, the diameter is `ThumbPressedSize`.

`ThumbOnInnerSize` and `ThumbOffInnerSize` cut a hole in the thumb. Zero draws a solid disc. `ThumbInnerColor` defaults to the current track color, so the hole matches the pill.

`ThumbOnContent` and `ThumbOffContent` are views centered in the thumb and clipped to the circle. Null draws no content. A check mark, a ring, or any other view is supplied by the caller.

## Defaults

The unset view is a plain pill. It is not a copy of one platform.

| Member | Default | Role |
|---|---|---|
| `IsOn` | false | The page's value. |
| `OnChanged` | none | Raised once when a person commits a new value. |
| `IsEnabled` | true | False blocks input. |
| `DisabledOpacity` | 0.38 | Opacity of the row while disabled. |
| `Label` | none | Any view, or a string. |
| `LabelSpacing` | 12 | Gap between the label and the pill. |
| `LabelLeading` | the default | Label at the start, pill at the end. |
| `TrackWidth` | 52 | |
| `TrackHeight` | 32 | |
| `TrackOnColor` | `#1C1C1E` | |
| `TrackOffColor` | `#E9E9EB` | |
| `TrackOnStroke` | `#1C1C1E` | Unused while the thickness is zero. |
| `TrackOffStroke` | `#C6C6C8` | Unused while the thickness is zero. |
| `TrackOnStrokeThickness` | 0 | |
| `TrackOffStrokeThickness` | 0 | |
| `TrackCornerRadius` | 16 | Half the track height. |
| `ThumbOnSize` | 24 | Diameter. |
| `ThumbOffSize` | 24 | Diameter. |
| `ThumbPressedSize` | 24 | Diameter while a finger is down. |
| `ThumbOnColor` | `#FFFFFF` | |
| `ThumbOffColor` | `#FFFFFF` | |
| `ThumbOnStroke` | `#FFFFFF` | Unused while the thickness is zero. |
| `ThumbOffStroke` | `#C6C6C8` | Unused while the thickness is zero. |
| `ThumbOnStrokeThickness` | 0 | |
| `ThumbOffStrokeThickness` | 0 | |
| `ThumbInset` | `(TrackHeight - thumbSize) / 2` | |
| `ThumbOnInnerSize` | 0 | Hole diameter. Zero is a solid disc. |
| `ThumbOffInnerSize` | 0 | |
| `ThumbInnerColor` | the current track color | |
| `ThumbOnContent` | none | View drawn in the on thumb. |
| `ThumbOffContent` | none | View drawn in the off thumb. |
| `Duration` | 200 | Milliseconds. |
| `Easing` | `CubicOut` | |

## Three looks

The same properties produce the three reference pills. `Check()` in the Material sample is a view the page builds. The kit does not ship it.

Material:

```csharp
SwitchView()
    .TrackWidth(52)
    .TrackHeight(32)
    .TrackOffColor(Color.FromArgb("#E6E0E9"))
    .TrackOnColor(Color.FromArgb("#6750A4"))
    .TrackOffStroke(Color.FromArgb("#79747E"))
    .TrackOffStrokeThickness(2)
    .ThumbOffSize(16)
    .ThumbOffColor(Color.FromArgb("#79747E"))
    .ThumbOnSize(24)
    .ThumbPressedSize(28)
    .ThumbOnColor(Colors.White)
    .ThumbOnContent(Check())
    .Label("Wi-Fi")
    .IsOn(State.Wifi)
    .OnChanged(on => SetState(s => s.Wifi = on));
```

Cupertino:

```csharp
SwitchView()
    .TrackWidth(51)
    .TrackHeight(31)
    .TrackOffColor(Colors.White)
    .TrackOnColor(Color.FromArgb("#007AFF"))
    .TrackOffStroke(Color.FromArgb("#C6C6C8"))
    .TrackOffStrokeThickness(1.5)
    .ThumbOffSize(27)
    .ThumbOnSize(27)
    .ThumbOffColor(Colors.White)
    .ThumbOnColor(Colors.White)
    .ThumbOffStroke(Color.FromArgb("#C6C6C8"))
    .ThumbOffStrokeThickness(1.5)
    .Label("Bluetooth")
    .IsOn(State.Bluetooth)
    .OnChanged(on => SetState(s => s.Bluetooth = on));
```

`#34C759` in `TrackOnColor` is the `UISwitch` green.

Fluent:

```csharp
SwitchView()
    .TrackWidth(40)
    .TrackHeight(20)
    .TrackOffColor(Color.FromArgb("#F3F3F3"))
    .TrackOnColor(Color.FromArgb("#0078D4"))
    .ThumbOffSize(10)
    .ThumbOnSize(10)
    .ThumbOffColor(Color.FromArgb("#616161"))
    .ThumbOnColor(Colors.White)
    .ThumbOnInnerSize(4)
    .Label("Airplane")
    .IsOn(State.Airplane)
    .OnChanged(on => SetState(s => s.Airplane = on));
```

A custom label is any view in the same slot:

```csharp
SwitchView()
    .Label(HStack(spacing: 8,
        Image("bell"),
        Label("Notifications").FontSize(17)))
    .IsOn(State.Notifications)
    .OnChanged(on => SetState(s => s.Notifications = on));
```

## Files

- `src/ReactorKit/SwitchView.cs` is the public node. `SwitchLabelSide` is a private enum in that class.
- `SwitchView()` is the factory on `Reactor.Kit.Extensions`, next to `GlazeView`.
- The track, the thumb, and the hole are shared MAUI shapes. No platform folder. `UseReactorKit()` does not grow a flag.
- The README gets a short Switch section: one sentence, one example, and the supported platforms. The part list stays in this document.

Callers set the side with `LabelLeading` and `LabelTrailing`. They do not name `SwitchLabelSide`.

## Sample

`HomePage` opens `SwitchTestPage` with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through the shared bar.

The page shows six `SwitchView`s.

- The plain default, labeled "Wi-Fi".
- The Material values above, labeled "Sync".
- The Cupertino values above, labeled "Bluetooth".
- The Fluent values above, labeled "Airplane".
- The plain default with an icon and the text "Notifications" as the label.
- The plain default, on, and disabled.

## Out of scope

A checkbox, a radio, or a segmented control. A second switch type for a platform. On and off words inside the pill. Hosting `UISwitch`, a Material `Switch`, or a Windows `ToggleSwitch`. Replacing `Microsoft.Maui.Controls.Switch`. A package version bump until the control merges.

## Done when

- [ ] `SwitchView()` draws the plain pill from the defaults table.
- [ ] The Material, Cupertino, and Fluent samples are that same view with the part values above.
- [ ] A custom view label lays out beside the pill, and a tap on that label does not toggle the switch.
- [ ] The page's `IsOn` moves the thumb without `OnChanged`. A tap and a committed drag raise `OnChanged` once.
- [ ] A disabled switch ignores input and draws at `DisabledOpacity`.
- [ ] `SwitchTestPage` shows the six switches.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
