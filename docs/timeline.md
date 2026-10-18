# Timeline

This document describes `TimelineView`.

A timeline is one vertical list of steps. A step has a label, a marker on a rail, and a content view. The label and the content are optional. The content can cover several steps. A group breaks the list and draws a header above its own steps. The rail between two markers is a solid line, a dashed line, or a gap.

The kit draws that rail and those markers, on iOS, Mac Catalyst, Android, and Windows. There is no hosted native timeline. A calendar, an agenda, and a start-and-end pair are the same view with different parts.

## Pieces

| Type | Role |
|---|---|
| `TimelineView` | The control. Placement, the default rail, and the default marker are properties. |
| `TimelineSteps` | The ordered list the factory builds. Not a control. |
| `TimelineGroup` | One header plus the steps under it. Not a control. |
| `TimelineStep` | One label, one marker, and one content view. Not a control. |

`TimelineSteps`, `TimelineGroup`, and `TimelineStep` are builders in `Reactor.Kit.TimelineViewTokens`. They hold views and return themselves, so the page can chain. They do not render. Callers add `global using Reactor.Kit.TimelineViewTokens` beside the other kit usings.

## TimelineView

The public node is `TimelineView`, a `Component`. The factory takes the list.

```csharp
TimelineView(steps => steps
    .Step(step => step
        .Label("Start")
        .Content("Thu, May 25th at 6:00AM"))
    .Step(step => step
        .Label("End")
        .Content("Thu, May 25th at 7:00AM")
        .MarkerRing()))
    .LabelsInside();
```

Start and End are ordinary steps. There is no start type and no end type. The first step is the top of the list. The last step is the bottom.

The page builds this tree inside `Render`. A content view can close over the page state. The timeline does not own a value, it does not raise a change callback, and it does not call `SetState`. A tap stays on the view that received it. The rail and the markers do not take taps.

The timeline fills the width of its parent and hugs its height. The page scrolls. The timeline does not scroll.

A right-to-left layout mirrors the columns. The rail stays vertical. A marker is not mirrored. A custom view follows its own flow direction.

## Steps

`Step` appends one step. The action configures it. A null action appends a step with the defaults. A later call on the same step replaces the earlier one for that slot.

```csharp
TimelineView(steps => steps
    .Step(step => step
        .Label("8:00")
        .Content(Card()))
    .Step(step => step
        .Label("9:00")));
```

`Label` takes a view. `Label(string)` puts that string in a wrapping label and leaves the text color to the theme. The size is `LabelFontSize` (15). A step's `LabelFontSize` replaces that for its own string. A custom label keeps the font the page set on it. Omitting `Label`, or passing null, leaves that step with no label.

`Content` takes a view. `Content(string)` puts that string in a wrapping label and leaves the text color to the theme. The size is `ContentFontSize` (16). A step's `ContentFontSize` replaces that for its own string. A custom content view keeps the font the page set on it. Omitting `Content`, or passing null, leaves that step with no content.

## Groups

`Group` appends a group. The group's own `Step` calls are the steps under the header. A group contains steps only. It does not contain another group.

```csharp
TimelineView(steps => steps
    .Group(day => day
        .Header("Wednesday 13")
        .Step(step => step
            .Label("8:00")
            .Content("Assignment Deadline"))
        .Step(step => step
            .Label("17:00")
            .Content("Class Cancelled"))));
```

`Header` takes a view. `Header(string)` puts that string in a wrapping bold label, leading aligned, and leaves the text color to the theme. The size is `HeaderFontSize` (13). A group's `HeaderFontSize` replaces that for its own string. A custom header keeps the font the page set on it. The header is the full width of the timeline. Omitting `Header`, or passing null, leaves the group with no header. The group's steps still form their own rail.

Consecutive steps that are not inside a group share one rail. A group ends that rail. The next steps start a new one. The line does not pass through the header.

The first block has no space above it. `GroupSpacing` (28) is the gap above every later block, whether that block is a group or a run of steps. `HeaderGap` (12) is the space between a header and the first step of that group. A group with no header does not add `HeaderGap`. `StepSpacing` (32) is the gap between steps: each row is at least that tall.

## Labels

Three placements. A later call replaces an earlier one. `LabelsLeading` is the default.

| Method | Columns, from the leading edge |
|---|---|
| `LabelsLeading` | label, rail, content |
| `LabelsTrailing` | content, rail, label |
| `LabelsInside` | rail, label, content |

`LabelsInside` is the start-and-end arrangement: the rail is first, the words sit beside it, and the times follow the words.

The label column is one width for the whole timeline, so the rails line up across groups. `LabelWidth` null sizes that column to the widest label. A string label wraps. Once the timeline has a width, a string label stops at half the room left after the rail and the gaps, so the content keeps the other half and wraps there too. A set value fixes the column. The string wraps inside it, and a custom label view is given that width.

When no step has a label, the label column and its gap are left out. When no step has content, the content column and its gap are left out. `LabelGap` (12) sits between the label and the rail. `ContentGap` (12) sits between the rail and the content.

A custom label is any view in the same slot:

```csharp
.Label(VStack(spacing: 0,
    Label("8:00").FontSize(15),
    Label("17:00").FontSize(15)))
```

## Content and span

`Span` is the number of step rows the content occupies, including its own row. The default is 1. A value below 1 is treated as 1. `Span` on a step with no content does nothing.

The content region is the content column across those rows. The region is as tall as the rows. A string is top-aligned. A custom view uses its own vertical options inside the region, so a card can fill the rows or sit at the top.

Spanned rows keep their own labels and markers. The content column is the only part that spans.

`Span` stays inside the current run. It does not cross a group. It also stops before a later step that has its own content, so two contents do not stack. The kit clamps the number. It does not throw.

Five steps in one run, A through E. A has content and `Span(4)`. C has content. A's content covers A and B. C keeps its own content. D and E are free for C's span.

When the content is taller than the sum of the minimum row heights, the extra height is split evenly across the spanned rows. The marker centers stay evenly spaced along that content. A shorter content does not shrink the rows. Each row is at least `StepSpacing` (32).

## Rail

The rail is one vertical line per run, drawn behind the markers. It is shared MAUI geometry. `StrokeDashArray` draws the dashes.

`Line`, `DashedLine`, and `NoLine` on the timeline set the default for every segment. `Line` is the default. The same three methods on a step set only the segment that leaves that step. A step that does not call one keeps the timeline default. The last step of a run has no segment leaving it.

A segment runs from the outside of one marker to the outside of the next. It does not continue above the first marker or below the last. A tall last block therefore has rail only down to its marker. A step with no marker ends the segment on the point where the marker center would be, which is the label's cross-axis center, or half `MarkerSize` from the top of the row when that step also has no label. A dashed rail with no markers is one continuous line from the first of those points to the last.

`NoLine` leaves a gap on that segment. The next segment that has a line starts again at its own marker.

`LineColor` and `LineThickness` exist on the timeline and on a step. The step's value, when set, replaces the timeline's value for the segment that leaves that step. A thickness of 0 draws no line. A negative thickness is treated as 0. A thick colored segment is a duration bar. `Span` does not thicken the line.

`DashLength` and `DashGap` are timeline values, in points. The default is 4 and 4. A step's `DashedLine` uses those values. A length of 0 or a gap of 0 draws a solid line. The shape's dash array counts in multiples of the stroke thickness, and the kit converts points into those multiples. Caps are flat, so a segment ends on the marker instead of extending by half the thickness.

The rail column is as wide as the wider of `LineThickness` and the widest marker in the timeline. Every run uses that width, so the rails line up.

## Markers

The default marker is a filled circle. `MarkerFilled` on the timeline keeps that default. `MarkerRing` uses a transparent fill and a stroke. `NoMarker` draws no marker. A later call replaces an earlier one.

A step has the same three methods, plus `Marker(VisualNode)`. A step that does not call one uses the timeline's marker. `Marker(VisualNode)` is per step, because one view has one parent. Null means no marker. A custom marker is centered on the rail axis and drawn above the line. It can be wider than `MarkerSize`. The rail column grows to fit the widest one.

`MarkerColor` and `MarkerSize` exist on the timeline and on a step. The step's value, when set, replaces the timeline's value for that step. `MarkerStrokeThickness` is a timeline value. A filled marker uses `MarkerColor` as its fill and draws no stroke. A ring uses `MarkerColor` as its stroke, at `MarkerStrokeThickness`, and draws no fill. The line stops at the outside of the ring, so the hole stays empty.

The marker is centered on the label's cross axis. The pair sits at the top of the row. A row that grows does not move them to the row's vertical center. With no label, the marker sits at the top of the row.

## Defaults

The unset view is a solid gray rail and filled ink markers. It is not a copy of one screen.

| Member | Default | Role |
|---|---|---|
| `LabelsLeading` | the default | Label, rail, content. |
| `Line` | the default | Solid segments. |
| `LineColor` | `#C6C6C8` | |
| `LineThickness` | 2 | A value of 0 draws no line. |
| `DashLength` | 4 | Points. Shared by every dashed segment. |
| `DashGap` | 4 | Points. A value of 0 draws a solid line. |
| `MarkerFilled` | the default | A filled circle on each step. |
| `MarkerColor` | `#1C1C1E` | Fill, or the ring stroke. |
| `MarkerSize` | 10 | Diameter. |
| `MarkerStrokeThickness` | 2 | Ring only. |
| `StepSpacing` | 32 | Gap between steps. Minimum row height. |
| `LabelGap` | 12 | Between the label and the rail. |
| `ContentGap` | 12 | Between the rail and the content. |
| `LabelWidth` | none | Widest label. A value fixes the column. |
| `GroupSpacing` | 28 | Gap above every block after the first. |
| `HeaderGap` | 12 | Between a header and its first step. |
| `LabelFontSize` | 15 | String labels. A step can replace it. |
| `ContentFontSize` | 16 | String content. A step can replace it. |
| `HeaderFontSize` | 13 | String headers. A group can replace it. |
| `Span` | 1 | Rows of content, including the step's own row. |

A negative gap, spacing, or font size is treated as 0.

## Arrangements

The same view produces the reference arrangements. The cards, the time pills, and the task rows are views the page builds. The kit does not ship them.

Start and end. The rail is leading, the words follow it, and the times are the content. The end marker is a ring.

```csharp
TimelineView(steps => steps
    .Step(step => step
        .Label("Start")
        .Content("Thu, May 25th at 6:00AM"))
    .Step(step => step
        .Label("End")
        .Content("Thu, May 25th at 7:00AM")
        .MarkerRing()))
    .LabelsInside();
```

Journal. No markers. The rail is dashed. Each label is a pill the page builds. One task row in the page can be a `CheckboxView`. `TimelineView` does not reference it.

```csharp
TimelineView(steps => steps
    .Step(step => step
        .Label(Time("10.30 AM"))
        .Content("Had a great day at work today."))
    .Step(step => step
        .Label(Time("12.30 PM"))
        .Content(Tasks()))
    .Step(step => step
        .Label(Time("13.30 PM"))
        .Content("Lorem ipsum dolor sit amet.")))
    .DashedLine()
    .NoMarker();
```

Grouped days. Each day is a header and its own rail. One segment is a thick colored bar.

```csharp
TimelineView(steps => steps
    .Group(day => day
        .Header("Wednesday 13")
        .Step(step => step
            .Label(Hours("8:00", "17:00"))
            .Content("Assignment Deadline")
            .MarkerColor(Color.FromArgb("#E35D6A")))
        .Step(step => step
            .Label(Hours("17:00", "19:30"))
            .Content("Class Cancelled")
            .MarkerColor(Color.FromArgb("#2BB673"))))
    .Group(day => day
        .Header("Thursday 14")
        .Step(step => step
            .Label(Hours("19:00", "21:00"))
            .Content("Basketball")
            .MarkerColor(Color.FromArgb("#7B61FF"))
            .LineColor(Color.FromArgb("#7B61FF"))
            .LineThickness(4))
        .Step(step => step
            .Label("22:00")
            .Content("Dinner")
            .MarkerColor(Color.FromArgb("#F5C451")))));
```

A block across several labels. The 9:00 and 10:00 labels stay beside the first card. The 11:00 card is its own content, so the first span stops before it.

```csharp
TimelineView(steps => steps
    .Step(step => step
        .Label("8:00")
        .Content(Card("Zoom meet with client"))
        .Span(3))
    .Step(step => step.Label("9:00"))
    .Step(step => step.Label("10:00"))
    .Step(step => step
        .Label("11:00")
        .Content(Card("Explore Design App"))));
```

Plain. The middle step has content and no label. The label column remains, because the other steps have labels.

```csharp
TimelineView(steps => steps
    .Step(step => step.Label("One").Content("Start"))
    .Step(step => step.Content("No label"))
    .Step(step => step.Label("Three").Content("End")));
```

## Files

- `src/ReactorKit/TimelineView.cs` is the public node. The constructor is internal. Label placement is a private enum in that class. The line kind and the marker kind are internal enums in that class, because the step reads them. Callers do not name them.
- `src/ReactorKit/TimelineViewTokens/TimelineSteps.cs`, `TimelineGroup.cs`, and `TimelineStep.cs` are the builders, in `Reactor.Kit.TimelineViewTokens`.
- `TimelineView(Action<TimelineSteps>)` is the factory on `Reactor.Kit.Extensions`, next to `CheckboxView`.
- The rail and the markers are shared MAUI shapes. No platform folder. `UseReactorKit()` does not grow a flag.
- The README gets a short Timeline section: one sentence, one example, and the supported platforms. The part list stays in this document.

Callers set placement with `LabelsLeading`, `LabelsTrailing`, and `LabelsInside`. They set the rail with `Line`, `DashedLine`, and `NoLine`. They set the marker with `MarkerFilled`, `MarkerRing`, `Marker`, and `NoMarker`.

## Sample

`HomePage` opens `TimelineTestPage` with `PushModalAsync(animated: false)`. The page closes with `PopModalAsync(animated: false)` through the shared bar. The new home button sits at the next offset, margin `(16, 458)`.

The page scrolls one timeline for each variation. It does not copy a reference screen.

- Start and end, with `LabelsInside` and a ring on the end step.
- The dashed journal, with pill labels, no markers, and a checkbox in the page content.
- Wednesday and Thursday, with colored markers and one thick segment.
- The 8:00 card spanning three labels, and the 11:00 card on its own row.
- The plain default, including one step with no label.
- `LabelsTrailing`.
- One dashed segment and one gap.
- A custom marker, with rings as the timeline default.
- A custom group header, and a span that stops at the end of the group.
- A fixed `LabelWidth`.
- Content and no labels, so the label column is left out.
- Labels and no content, so the content column is left out.
- A group with no header, so the rail breaks without a title.
- Larger label type, a smaller content size on one step, a larger group header, a shorter step gap, and a taller group gap.
- The plain timeline in a right-to-left layout.

## Out of scope

A horizontal timeline. Positioning a step by time of day, and horizontal hour rules. Drag to reorder. Collapsing a group. Selection. A now-indicator type: a page that wants one puts it in a step. Content that alternates sides of the rail. A nested group. A second timeline type for a platform. Hosting a native control. A package version bump until the control merges.

## Done when

- [ ] `TimelineView` draws the plain timeline from the defaults table.
- [ ] A label is a string or a view. A step can omit it, and the label column stays when another step has one.
- [ ] Content is a string or a view. `Span(3)` covers three rows, and those rows keep their labels and markers.
- [ ] A span stops before a later step that has content, and it stops at the end of the run.
- [ ] A group header is a string or a view, at the full width, and the rail breaks around the group.
- [ ] `DashedLine` draws dashes. `NoLine` leaves a gap. A step's line color and thickness style only the segment that leaves that step.
- [ ] `MarkerRing`, `NoMarker`, and a custom marker replace the filled circle. `MarkerColor` tints that step.
- [ ] `LabelsInside` lays out rail, label, content. `LabelsTrailing` lays out content, rail, label.
- [ ] The line runs from the outside of one marker to the outside of the next, and it stops on the last marker of the run.
- [ ] `TimelineTestPage` shows every variation listed in the sample.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
