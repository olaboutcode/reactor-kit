# KitTests home

This document describes the KitTests home list.

`HomePage` is a list of links. Each row opens one component page. The list follows the Inema gallery: a title, one sentence, a row for each page, and an appearance row at the end. A row is 44 points tall, the standard hit target.

`AppShell` hosts `HomePage`. The flyout stays disabled. The shell navigation bar stays hidden. The page is not wrapped in a `NavigationPage`.

## Pieces

| Type | Role |
|---|---|
| `HomePage` | The list. It stays the shell content. |
| `GlazeTestPage` | The glaze sample that used to live on `HomePage`. |

There is no new public control. The component pages that already exist stay.

## Home

The page background is `#F7F5F2` in the light theme and `#111110` in the dark theme. The content is a `ScrollView`. The scroll bar is hidden.

The title is `KitTests`, 22 points, `#1C1C1E` in the light theme and `#F7F5F2` in the dark theme. Under it, 8 points down, one sentence at 16 points in `#8E8E93`:

`Glaze, mica, menu, sheet, switch, checkbox, timeline, rich text, reading, markdown, object, and model.`

The rows start 24 points below that sentence. The list padding is 16 points on the left and right.

A row is 44 points tall and the full width of the list. The title is 16 points, in the same ink as the page title, and it is leading aligned. The trailing mark is the Heroicons chevron, drawn as a path in a 20 point square. The path is `M 6.875 3.75 L 13.125 10 L 6.875 16.25`, which is `M8.25 4.5 L15.75 12 L8.25 19.5` scaled from the 24 point icon into 20 points. The stroke is `#8E8E93`, thickness 1.25, with round caps and joins. The row draws the path because `HeroIcons.ChevronRight` is a fixed `#0F172A` image. A tap opens that page with `PushModalAsync(animated: false)`. The row has no fill and no corner radius.

The rows, in order:

| Row | Page |
|---|---|
| Glaze | `GlazeTestPage` |
| Mica | `MicaTestPage` |
| Menu | `MenuTestPage` |
| Sheet | `SheetTestPage` |
| Switch | `SwitchTestPage` |
| Checkbox | `CheckboxTestPage` |
| Timeline | `TimelineTestPage` |
| Rich text | `RichTextTestPage` |
| Reading | `ReadingTestPage` |
| Markdown | `MarkdownTestPage` |
| Object | `ObjectTestPage` |
| Model | `ModelTestPage` |

The appearance row is 24 points below the last page row. It is 44 points tall, leading aligned, 16 points, and it has no chevron. Its label is `Light` when the theme is dark and `Dark` when the theme is light. The color is `#1C1C1E` in the light theme and `#F7F5F2` in the dark theme. A tap calls `AppTheme.ToggleCurrentAppTheme`.

The floating pills and their stacked margins go away.

## Glaze page

`GlazeTestPage` keeps the glaze sample that `HomePage` shows today.

`NavigationBar` titles it `Glaze` and closes with `PopModalAsync(animated: false)`. The shell navigation bar stays hidden. The page background follows the theme, the same two colors as home.

Under the bar, the page scrolls colored lines. A glaze banner at the top overlaps that scroll and has no opaque plate of its own, so the lines stay visible in the banner while they move. An opaque bar, the page background color, sits above the scroll and does not overlap it. A fully rounded `GlazeView` sits on that bar. Scrolling the lines does not paint them into that circle. The circle stays the bar color, plus the wash, the noise, and the hairline.

The centered samples stay: the round plus, the glaze text button, the entry panel, and the dark title panel. The mica buttons that sit in that stack today do not move with it. `MicaTestPage` already shows the mica samples.

## Files

- `samples/KitTests/Components/HomePage.cs` becomes the list.
- `samples/KitTests/Components/GlazeTestPage.cs` receives the glaze sample.
- `docs/glaze.md` names `GlazeTestPage` as the sample. The layouts in that section stay.
- The KitTests paragraph in `README.md` says home is the list, that a row opens its page with `PushModalAsync`, and that the last row switches the theme. The shadow levels in that paragraph stay.

`AppShell` still constructs `HomePage`. No other page changes.

## Out of scope

A new public component. A theme button on every component page. A `NavigationPage`, or opening a page with `PushAsync`. Tokens, icons, button, input, and navigation pages. A package version bump.

## Done when

- [ ] Home is a scrolling list of the twelve rows above, in that order, with a chevron on each row and no filled pills.
- [ ] Glaze opens `GlazeTestPage`. The opaque bar, the overlapping banner, and the centered glaze samples are on that page.
- [ ] Mica, menu, sheet, switch, checkbox, timeline, rich text, reading, markdown, object, and model open the pages they open today.
- [ ] Each of those pages still closes with `PopModalAsync(animated: false)` through `NavigationBar`.
- [ ] The last row switches the theme. Its label reads `Dark` in the light theme and `Light` in the dark theme.
- [ ] `AppShell` still hosts `HomePage`. The app does not wrap that page in a `NavigationPage` and does not open a sample with `PushAsync`.
- [ ] The library and KitTests build for `net10.0` with no warnings. Android compiles. iOS compiles when a shared file changes. Windows is not required on the Mac.
