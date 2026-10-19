# Rich text

This document describes the split-column layout on `RichTextView`. The view already draws one continuous column with SkiaSharp. This stays on that view.

The guide for the reading surface is a book spread: two pages side by side, text filling the left page and then the right page. Issue #17 is this improvement.

## Pieces

| Type | Role |
|---|---|
| `RichTextView` | The control. The split is a property on this view. |

There is no second text view. Taps, highlights, ranges, appearance, and the font stay on this view.

## Split column

`SplitColumn` lays the text out as two columns. A later call replaces an earlier one.

```csharp
RichTextView(chapter)
    .SplitColumn()
    .ColumnGap(32);
```

```csharp
RichTextView(chapter)
    .SplitColumn(when: () => State.Wide)
    .ColumnGap(48);
```

`when` is optional. The kit calls it before laying the text out. An omitted `when`, or a null function, is true, so `SplitColumn()` always splits. A function that returns false keeps today's one-column layout: the view hugs its text and the page scrolls. `Spread` and `ColumnGap` are ignored in that layout.

`ColumnGap` is the space between the two columns, in points. The default is 24. A later call replaces an earlier one. A negative value is treated as 0. Each column is half of the width that remains.

The state overload tracks the component the same way `Text` and `FontSize` do.

```csharp
public RichTextView SplitColumn(Func<bool>? when, IComponentWithState? state = null)
```

The function receives no size. The page closes over the state it already owns.

## Pages

While `when` is true, the view fills the width and the height it is given. That box is two columns.

The first column of the spread sits on the leading side, so a right-to-left layout puts it on the right.

A line stays on one page. The left page fills from the top. The next line starts at the top of the right page. A line that is taller than the page still occupies that page, so a short page cannot loop. `PreserveLineBreaks`, `FirstLineIndent`, `RunAt`, and the font apply inside a page the same way they do in the one-column layout.

The existing padding around the text stays. It is inside each page, not in the gap.

When the parent does not bound the height, there is one spread. The leading column takes about half the stacked line height, and at least one line. The other column takes the rest. The view's height is the taller column. There is nothing to turn.

When the parent bounds the height, a page holds as many lines as fit. Text past the right page is the next spread. The view does not grow to the whole document, and it does not set its height to that document.

## Spread

`Spread` is the zero-based spread the page owns. The default is 0. Spread 0 shows the first two pages, spread 1 the next two, and so on. A value below 0 is 0. A value past the last spread is the last spread. When that clamp changes the value, the view raises `OnSpreadChanged` once so the page can store it. Setting `Spread` to the value already shown does not raise the callback.

A horizontal drag follows the finger. Releasing past the midpoint of one column commits the next or previous spread and raises `OnSpreadChanged`. A release before the midpoint settles back. The drag does not raise the callback until it commits. In a left-to-right layout, dragging left turns to the next spread. A right-to-left layout mirrors that, so dragging right turns to the next spread. A tap still hits a word, a sentence, or a range. A tap does not turn the spread.

`OnSpreadChanged` is not raised when `when` is false.

Highlights paint on the page where the word sits. A range that crosses the gap paints on both pages. Hit testing uses that same position. `OnTokenTapped` and `OnTokenLongPressed` are unchanged.

## Defaults

| Member | Default | Role |
|---|---|---|
| `SplitColumn` | off | No call keeps the one column. |
| `when` | true | Omitted or null. False keeps the one column. |
| `ColumnGap` | 24 | Points between the two columns. Any non-negative value. |
| `Spread` | 0 | The spread the page owns. |
| `OnSpreadChanged` | none | Raised when a drag commits a new spread, or when a stored spread is clamped. |

## Files

- `src/ReactorKit/RichTextView.cs` gains `SplitColumn`, `ColumnGap`, `Spread`, and `OnSpreadChanged`. The constructor stays internal.
- `src/ReactorKit/Internals/RichText/RichTextCanvas.cs` lays out the two pages and keeps the one-column path when `when` is false.
- No new public type. No tokens namespace. No platform folder. `UseReactorKit()` does not grow a flag.
- The README rich text section gains `SplitColumn`, `ColumnGap`, `Spread`, and `OnSpreadChanged`. The rest of the behavior stays in this document.

## Sample

`RichTextTestPage` keeps the John 3 chapter. The chapter calls `SplitColumn(when: () => State.Split).ColumnGap(32)` with `State.Split` starting true. The Split button sets that condition false, and its label then reads One column. The parameterless `SplitColumn()` is the same layout as a `when` that returns true. The first eight verses sit in a row 220 points tall, call `SplitColumn().ColumnGap(48)`, and show the spread number. No new home button.

## Out of scope

A second public text view. A single-page mode with its own method. More than two columns. A page-curl drawing. Applying the split to `MarkdownView`. A package version bump.

## Done when

- [ ] `SplitColumn()` shows the chapter as two columns. An omitted `when` is true.
- [ ] `SplitColumn(when: () => false)` keeps the one continuous column.
- [ ] `ColumnGap(48)` widens the space between the columns. The default gap is 24.
- [ ] A bounded height fills the left page and then the right page, and the next lines are the next spread.
- [ ] An unbounded height is one spread, as tall as the taller column.
- [ ] A drag past the midpoint changes `Spread` and raises `OnSpreadChanged` once. Setting `Spread` to the current value does not.
- [ ] A tap still hits a word, a sentence, or a range on the page where that text is drawn.
- [ ] The library and KitTests build for `net10.0` with no warnings. iOS and Android compile. Windows is not required for the first compile on the Mac.
