# Checkbox

This document describes `CheckboxView`.

A checkbox is one box and one mark. People select a statement, and the result can wait for a later submit. The guides for the three familiar looks are [Checkboxes](https://m3.material.io/components/checkbox/overview), [Toggles](https://developer.apple.com/design/human-interface-guidelines/toggles), and [Check boxes](https://learn.microsoft.com/en-us/windows/apps/design/controls/checkbox).

The kit draws one checkbox, on iOS, Mac Catalyst, Android, and Windows. A macOS `NSButton` checkbox, the Material `CheckBox`, and the Windows `CheckBox` stay unused. There is no Material type, no Cupertino type, and no Fluent type. Those looks are the same view with different part values.

iOS has no system checkbox. The Apple sample below uses the macOS checkbox measures. A fully rounded box is still this checkbox: the mark is a check. It is not a radio button.

## CheckboxView

The public node is `CheckboxView`, a `Component`. The label is any view. The mounted element is the row.

```csharp
CheckboxView()
    .Label("Terms")
    .IsChecked(State.Terms)
    .OnChanged(on => SetState(s => s.Terms = on));
```

The page owns `IsChecked`. A tap on the box toggles it. `OnChanged` runs once, with the new value. It does not run when the new value equals the current one, and it does not run when the page sets `IsChecked`. Setting `IsChecked` animates the fill, the stroke, and the check.

A tap on the label does not toggle the checkbox. The label is a custom view and keeps its own taps.

`IsEnabled` is true. False ignores the tap, and the row draws at `DisabledOpacity` (0.38).

The box's hit box is at least 44 by 44. The box is centered in that hit box. The row is as tall as the label when the label is taller, and the box stays vertically centered. A right-to-left layout mirrors the row. The check is not mirrored.

The fill, the stroke, and the check change over `Duration` milliseconds (200). `Easing` is `CubicOut`. The check's opacity follows that progress. There is no drag.

## Parts

`Label` takes a view. `Label(string)` puts that string in a 17pt label and leaves the text color to the theme. One slot: a later call replaces the earlier one. `LabelSpacing` is 12. `LabelSide` is `Leading`, so the label is at the start and the box is at the end. `Trailing` puts the box first. With a width on the row, the label takes the remaining space. With no width, the row hugs the label, the gap, and the box.

The box is a square. `BoxSize` is the side length. `BoxCornerRadius` rounds every corner by the same amount.

- `0` is a square.
- A value of half `BoxSize`, or any larger value, is a circle. The kit clamps the radius to `BoxSize / 2`.
- A value between those two is a rounded square.
- A negative value is treated as `0`.

Each of fill, stroke color, and stroke thickness has a checked value and an unchecked value. A stroke thickness of zero draws no stroke.

The kit draws the check: two round-capped strokes through the points `(0.22, 0.52)`, `(0.42, 0.72)`, and `(0.78, 0.30)`, as fractions of `BoxSize`. `CheckColor` and `CheckThickness` style that path. The check is clipped to the rounded box.

`CheckedContent` is a view centered in the box. Null draws the kit check. A view replaces the kit check, and it is clipped to the same rounded box.

## Defaults

The unset view is a plain rounded square. It is not a copy of one platform.

| Member | Default | Role |
|---|---|---|
| `IsChecked` | false | The page's value. |
| `OnChanged` | none | Raised once when a person commits a new value. |
| `IsEnabled` | true | False blocks input. |
| `DisabledOpacity` | 0.38 | Opacity of the row while disabled. |
| `Label` | none | Any view, or a string. |
| `LabelSpacing` | 12 | Gap between the label and the box. |
| `LabelSide` | Leading | Label at the start, box at the end. |
| `BoxSize` | 20 | Side length. |
| `BoxCornerRadius` | 4 | Clamped to `0` ... `BoxSize / 2`. |
| `BoxOnColor` | `#1C1C1E` | Fill when checked. |
| `BoxOffColor` | transparent | Fill when unchecked. |
| `BoxOnStroke` | `#1C1C1E` | Unused while the thickness is zero. |
| `BoxOffStroke` | `#C6C6C8` | |
| `BoxOnStrokeThickness` | 0 | |
| `BoxOffStrokeThickness` | 1.5 | |
| `CheckColor` | `#FFFFFF` | |
| `CheckThickness` | 2 | |
| `CheckedContent` | none | Replaces the kit check. |
| `Duration` | 200 | Milliseconds. |
| `Easing` | `CubicOut` | |

## Corner radius

The same box, with only the radius changed:

```csharp
CheckboxView()
    .BoxCornerRadius(0)
    .Label("Backup");

CheckboxView()
    .BoxCornerRadius(10)
    .Label("Agree");
```

`10` is half of the default `BoxSize`, so "Agree" is a circle. A larger number stays a circle.

## Three looks

The same properties produce the three reference boxes.

Material. The container is 18, the corner is 2, and the unchecked stroke is 2. The published target is 48. This kit uses 44, the same hit box as `SwitchView`.

```csharp
CheckboxView()
    .BoxSize(18)
    .BoxCornerRadius(2)
    .BoxOffColor(Colors.Transparent)
    .BoxOnColor(Color.FromArgb("#6750A4"))
    .BoxOffStroke(Color.FromArgb("#79747E"))
    .BoxOffStrokeThickness(2)
    .BoxOnStroke(Color.FromArgb("#6750A4"))
    .BoxOnStrokeThickness(0)
    .CheckColor(Colors.White)
    .Label("Sync")
    .IsChecked(State.Sync)
    .OnChanged(on => SetState(s => s.Sync = on));
```

macOS. The box is 14 with a corner of 4. Checked fill is `#007AFF`.

```csharp
CheckboxView()
    .BoxSize(14)
    .BoxCornerRadius(4)
    .BoxOffColor(Colors.White)
    .BoxOnColor(Color.FromArgb("#007AFF"))
    .BoxOffStroke(Color.FromArgb("#C6C6C8"))
    .BoxOffStrokeThickness(1)
    .BoxOnStrokeThickness(0)
    .CheckColor(Colors.White)
    .Label("iCloud")
    .IsChecked(State.ICloud)
    .OnChanged(on => SetState(s => s.ICloud = on));
```

Fluent. Windows 11 uses a 20 box and a 4 corner on in-page controls such as `CheckBox`.

```csharp
CheckboxView()
    .BoxSize(20)
    .BoxCornerRadius(4)
    .BoxOffColor(Colors.Transparent)
    .BoxOnColor(Color.FromArgb("#0078D4"))
    .BoxOffStroke(Color.FromArgb("#616161"))
    .BoxOffStrokeThickness(1)
    .BoxOnStrokeThickness(0)
    .CheckColor(Colors.White)
    .Label("Mail")
    .IsChecked(State.Mail)
    .OnChanged(on => SetState(s => s.Mail = on));
```

A custom label is any view in the same slot:

```csharp
CheckboxView()
    .Label(HStack(spacing: 8,
        Image("bell"),
        Label("Notifications").FontSize(17)))
    .IsChecked(State.Notifications)
    .OnChanged(on => SetState(s => s.Notifications = on));
```

A custom mark replaces the kit check:

```csharp
CheckboxView()
    .CheckedContent(Check())
    .Label("Custom");
```

## Files

- `src/ReactorKit/CheckboxView.cs` is the public node. The constructor is internal.
- `src/ReactorKit/CheckboxTokens/CheckboxLabelSide.cs` holds `Leading` and `Trailing`. Namespace `Reactor.Kit.CheckboxTokens`.
- `CheckboxView()` is the factory on `Reactor.Kit.Extensions`, next to `SwitchView`.
- The box and the check are shared MAUI shapes. No platform folder. `UseReactorKit()` does not grow a flag.
- The README gets a short Checkbox section: one sentence, one example, and the supported platforms. The part list stays in this document.

Callers add `global using Reactor.Kit.CheckboxTokens` beside the other kit usings.

## Sample

`HomePage` opens `CheckboxTestPage` with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through the shared bar.

The page shows eight `CheckboxView`s.

- The plain default, labeled "Terms".
- The plain default with `BoxCornerRadius` 0, labeled "Backup".
- The plain default with `BoxCornerRadius` 10, labeled "Agree".
- The Material values above, labeled "Sync".
- The macOS values above, labeled "iCloud".
- The Fluent values above, labeled "Mail".
- The plain default with an icon and the text "Notifications" as the label.
- The plain default, checked, and disabled.

## Out of scope

A radio button. An indeterminate or mixed state. A separate width and height, or a different radius on each corner. A second checkbox type for a platform. Hosting an `NSButton` checkbox, a Material `CheckBox`, or a Windows `CheckBox`. Replacing `Microsoft.Maui.Controls.CheckBox`. A press ripple, a hover grow, or a drag. A package version bump until the control merges.

## Done when

- [ ] `CheckboxView()` draws the plain rounded square from the defaults table.
- [ ] `BoxCornerRadius` 0 is a square, and half `BoxSize` is a circle. A larger radius stays a circle.
- [ ] The Material, macOS, and Fluent samples are that same view with the part values above.
- [ ] A custom view label lays out beside the box, and a tap on that label does not toggle the checkbox.
- [ ] `CheckedContent` replaces the kit check and is clipped to the rounded box.
- [ ] The page's `IsChecked` moves the check without `OnChanged`. A tap raises `OnChanged` once.
- [ ] A disabled checkbox ignores input and draws at `DisabledOpacity`.
- [ ] `CheckboxTestPage` shows the eight checkboxes.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
