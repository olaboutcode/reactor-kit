# Reactor.Kit

A library of custom controls for [MauiReactor](https://github.com/adospace/reactorui-maui). Each control is a MauiReactor component.

> [!WARNING]
> The library is still experimental. Use it at your own risk. Expect breaking changes as the API settles.

## Table of contents

- [Getting started](#getting-started)
  - [Install](#install)
  - [Example](#example)
  - [Namespaces](#namespaces)
  - [Host](#host)
- [Components](#components)
  - [Glaze](#glaze)
    - [Properties](#properties)
    - [Supported platforms](#supported-platforms)
  - [Rich text](#rich-text)
    - [Properties](#properties-1)
    - [Supported platforms](#supported-platforms-1)
  - [Markdown](#markdown)
    - [Properties](#properties-2)
    - [Supported platforms](#supported-platforms-2)
  - [Object3D](#object3d)
    - [Properties](#properties-3)
    - [Supported platforms](#supported-platforms-3)
  - [Popup](#popup)
    - [Properties](#properties-4)
    - [Supported platforms](#supported-platforms-4)
- [Handlers](#handlers)
  - [BorderlessEntry](#borderlessentry)
  - [BorderlessEditor](#borderlesseditor)
- [Samples](#samples)
- [Building](#building)
- [License](#license)

## Getting started

Reactor.Kit is meant to work in a MauiReactor app.

### Install

1. Follow the [MauiReactor instructions](https://github.com/adospace/reactorui-maui#setting-up-mauireactor-from-cli) to create a MauiReactor app.
2. Install Reactor.Kit.

```
dotnet add package Reactor.Kit --prerelease
```

### Example

Call `UseReactorKit()` at startup, then build the panel with `GlazeView`.

```csharp
class App : Component
{
    public override VisualNode Render()
        => ContentPage(
            GlazeView(
                Button("Test Button").Center()
            )
            .Padding(16, 16)
            .CornerRadius(22)
        );
}
```

You can pass several children. The last one is the content.

Pass a callback first to capture the element. It is a `ContentView`. The callback receives null when the panel unmounts.

```csharp
Microsoft.Maui.Controls.ContentView? _panel;

GlazeView(panel => _panel = panel,
    Label("Title").Center())
```

### Namespaces

The namespace is `Reactor.Kit`. Rich text metrics and hits are in `Reactor.Kit.RichTextViewTokens`. Mesh and camera types are in `Reactor.Kit.Object3DViewTokens`. Add these lines to the app's global usings.

```csharp
global using Reactor.Kit;
global using Reactor.Kit.Object3DViewTokens;
global using Reactor.Kit.PopupMenuTokens;
global using Reactor.Kit.RichTextViewTokens;
global using Reactor.Kit.SwitchTokens;
global using static Reactor.Kit.Extensions;
```

### Host

Call `UseReactorKit()` once, on the app builder, after `UseMauiReactorApp`.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit();
```

## Components

List of available components.

### Glaze

`GlazeView` blurs what is behind the panel. The child is a normal MAUI view and is never blurred.

> [!IMPORTANT]
> On Android the glass is a snapshot of the page behind it, so that page has to stay attached. `PushModalAsync` leaves it in place. A `NavigationPage`, or a Shell `PushAsync`, takes the page down, and the panel draws flat for a frame when the page comes back.

#### Properties

| Member | Default | Role |
|---|---|---|
| `TintColor` | `#F3F3F3` | Wash color. This is the mica base, not pure white. |
| `TintOpacity` | `0.28` | Requested wash. A light color is eased down, so `#F3F3F3` lands near 6% where a material already veils the backlight. A dark color stays near 25%. |
| `BlurRadius` | `8`, or `32` on Android | On iOS and Mac Catalyst, any value above 0 turns the system material on. `0` turns it off. On Android this is the blur in dp. Setting it yourself overrides the platform default. |
| `NoiseOpacity` | `0.015` | Fine dusting over the wash. |
| `Stroke` | white at 18% | Hairline color. |
| `StrokeThickness` | `1` | Hairline width, in dp. |
| `CornerRadius` | `0` | Rounds the panel. The shadow follows this corner. |

```csharp
GlazeView(Label("Title").Center())
    .TintColor(Color.FromArgb("#1C1C1E"))
    .CornerRadius(22)
    .Stroke(Colors.White)
    .StrokeThickness(1)
```

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+
- Windows 10.0.17763+

### Rich text

`RichTextView` draws paragraph text with SkiaSharp. A tap hits a word, a sentence, or a character range you supply. A long-press on text that is already highlighted raises its own callback and leaves the highlight in place. Highlights are rectangles behind the text. Characters the body font does not contain, including emoji and other writing systems, are taken from the system fonts. Each word is shaped, so connected scripts stay connected.

#### Properties

| Member | Default | Role |
|---|---|---|
| `Text` | empty | The paragraph. |
| `FontSize` | `17` | Size in device-independent pixels. |
| `FontFamily` | system | Registered font name. Missing glyphs still come from the system fonts. |
| `LineSpacing` | `4` | Extra pixels between lines. |
| `Appearance` | none | Page metrics, or a callback that builds them from the text. `RunAt` styles one word. An unset size or spacing keeps `FontSize` and `LineSpacing`. |
| `TextColor` | black | Glyph color. |
| `HighlightColor` | amber at about 47% | Color of the rectangles behind highlighted words. |
| `ToggleHighlightOnTap` | `true` | A tap toggles that word or sentence. The tap callback still runs. |
| `TapTarget` | `Word` | `Sentence` reports and toggles the whole sentence. A word inside a `TapRanges` entry uses that range instead. |
| `TapRanges` | none | Character ranges on `Text`. A tap selects the shortest range that overlaps the word. |
| `HighlightedWords` | none | Word indexes to paint. |
| `HighlightedSentences` | none | Sentence indexes to paint. |
| `HighlightedRanges` | none | Character ranges. A word is highlighted when it overlaps a range. |
| `OnTokenTapped` | none | Receives the tapped word, sentence, or range. |
| `OnTokenLongPressed` | none | Receives highlighted text after a half-second hold. The tap does not also run. |

```csharp
RichTextView()
    .Text("In the beginning God created the heaven and the earth. And the earth was without form.")
    .FontSize(20)
    .TextColor(Color.FromArgb("#1C1C1E"))
    .HighlightColor(Color.FromRgba(245, 196, 40, 120))
    .HighlightedSentences([0])
    .OnTokenTapped(hit => System.Diagnostics.Debug.WriteLine(hit.Text))
    .OnTokenLongPressed(hit => System.Diagnostics.Debug.WriteLine(hit.Text))
```

`TapRanges` uses indexes in that same string. The first range below ends in the middle of the second sentence, so a tap there reports more than `TapTarget.Sentence` would.

```csharp
const string Chapter =
    "In the beginning God created the heaven and the earth. And the earth was without form, and void.";

RichTextView()
    .Text(Chapter)
    .TapRanges([
        new TextRange(0, 68),
        new TextRange(69, 27),
    ])
    .OnTokenTapped(hit => System.Diagnostics.Debug.WriteLine(hit.Text))
```

A new list with the same indexes keeps tap toggles. Set `ToggleHighlightOnTap(false)` and pass `HighlightedWords` when the page owns the highlights.

`Appearance` describes the page. `RunAt` describes one word and receives the character index where that word starts. The default result leaves the word on the page style. A fixed value and a callback are the same metrics.

```csharp
RichTextView(chapter).Appearance(new TextAppearance
{
    LineSpacing = 6,
    FirstLineIndent = 24,
    PreserveLineBreaks = false,
});

RichTextView(chapter).Appearance(text => new TextAppearance
{
    PreserveLineBreaks = true,
    RunAt = (start, length) => new RunMetrics
    {
        Scale = 0.65f,
        BaselineShift = 0.35f,
    },
});
```

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+
- Windows 10.0.17763+

### Markdown

`MarkdownView` draws a markdown document with SkiaSharp. The source is a string, or a UTF-8 file path. It draws CommonMark plus the usual GitHub extensions: headings, quotes, nested lists, task lists, definition lists, fenced and indented code, pipe and grid tables, footnotes, alerts, strikethrough, subscript, superscript, highlight, autolinks, and emoji shortcodes. Math is drawn as its source. A fenced block tagged `diagram`, `mermaid`, `flowchart`, or `graph` is drawn as a flowchart when it uses the node syntax below. Any other diagram stays as code.

A picture written as `![alt](url)` is not loaded. The alt text is shown instead. HTML is shown as text.

The diagram is a flowchart. A line can set the direction: `TB`, `TD`, `BT`, `LR`, or `RL`. `flowchart LR` and `graph TD` set it too. Nodes are `id[label]`, `id(label)`, or `id{label}`. Edges are `A --> B`, `A --- B`, and `A -->|yes| B`.

#### Properties

| Member | Default | Role |
|---|---|---|
| `Markdown` | empty | The document. |
| `File` | empty | UTF-8 path. When set, this replaces `Markdown`. |
| `FontSize` | `16` | Body size in device-independent pixels. |
| `FontFamily` | system | Registered font name for the prose. Code stays monospace. |
| `TextColor` | black | Text, rules, table grid, and diagram strokes. |

```csharp
MarkdownView()
    .File("notes.md")
    .FontSize(16)
    .TextColor(Color.FromArgb("#1C1C1E"))
```

`samples/KitTests/Resources/Raw/notes.md` is a short document with a table and a diagram. The sample page embeds that same text. `File` reads a path on disk. A Maui asset name is not a file path.

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+
- Windows 10.0.17763+

### Object3D

`Object3DView` draws one rigid triangle mesh. A `.glb` includes every mesh in its default scene. Drag to orbit, pinch to zoom, and drag with two fingers to pan. `Color` is a tint multiplied over the mesh colors, including a `.glb` base-color image. `alphaMode` `MASK` cuts texels away, and `doubleSided` draws both faces. Metallic, roughness, normal maps, and blended transparency are not read. `Source` is a file path. The view reads that file and checks the glTF 2.0 header. A missing file throws the IO exception from the read. A file that is not a `.glb` throws `InvalidDataException`. `PackagedSource` is a Maui asset name and wins over `Source`. `Mesh` wins over both. A missing packaged asset, or one that is not a `.glb`, throws on the UI thread.

#### Properties

| Member | Default | Role |
|---|---|---|
| `Mesh` | empty | Triangle mesh. Copied when set. |
| `Source` | empty | Filesystem path to a `.glb`. Ignored when `Mesh` or `PackagedSource` is set. A missing file or a non-glb throws. |
| `PackagedSource` | empty | Maui asset name of a `.glb`. Ignored when `Mesh` is set. A missing asset or a non-glb throws on the UI thread. |
| `Yaw` | `30` | Camera yaw around world up, in degrees. |
| `Pitch` | `20` | Camera pitch, in degrees. |
| `Distance` | unset | Camera distance. Unset frames the mesh. |
| `Target` | unset | Look-at point. Unset uses the bounds center. |
| `Rotation` | `0, 0, 0` | Root yaw, pitch, and roll, in degrees. |
| `Scale` | `1` | Uniform scale about the mesh origin. |
| `Color` | `#FFFFFF` | sRGB tint multiplied over the mesh. |
| `Background` | transparent | Clear color. Does not tint the mesh. |
| `Gestures` | `true` | Orbit, pinch zoom, and two-finger pan. |
| `AutoRotate` | `false` | Advances yaw while the view is on screen. |
| `OnOrbitChanged` | none | Settled `Orbit` after the last pointer lifts. Auto-rotate does not call it. |

```csharp
Object3DView()
    .Mesh(Mesh.Sphere(1f, slices: 48, stacks: 32))
    .Color(Color.FromArgb("#C8C2B8"))
```

How `Color` combines with a `.glb` is in [docs/object-view.md](docs/object-view.md).

Pass a callback to capture the element. It is a `ContentView`. The callback receives null when the view unmounts.

```csharp
Microsoft.Maui.Controls.ContentView? _view;

Object3DView(view => _view = view)
    .PackagedSource("industrial_microscope.glb")
```

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+

### Popup

`PopupView` floats a panel from an anchor. The panel can hold any content. The page sets `IsOpen`. A tap outside the panel, a second tap on the anchor, or the platform back or Escape key runs `OnDismiss`. Placement is described in [docs/popup-menu.md](docs/popup-menu.md).

#### Properties

| Member | Default | Role |
|---|---|---|
| `Content` | empty | The floating node. An empty panel still opens. |
| `IsOpen` | `false` | The page owns this. The anchor's tap does not open the panel. |
| `Alignment` | `Trailing` | Lines the panel up with the anchor's trailing edge, leading edge, or center. |
| `Gap` | `8` | Points between the anchor and the panel. The same inset keeps the panel inside the safe area. |
| `MinWidth` | `220` | Smallest panel width, in points. `0` leaves the measured width, so the panel hugs the content. A narrower window uses the space that remains. |
| `MinHeight` | `44` | Smallest panel height, in points. |
| `OpenScaleX` | `0.96` | Horizontal scale the panel starts from, and returns to when it closes. |
| `OpenScaleY` | `0.96` | Vertical scale the panel starts from, and returns to when it closes. |
| `OpenScale` | `0.96` | Sets both axes to the same scale. |
| `Duration` | `180` | Open and close animation length, in milliseconds. |
| `Easing` | `CubicOut` | Curve for the fade and both scales. |
| `DimAnchor` | `false` | Fades the anchor to 40% opacity while the panel is open and ignores taps on it. |
| `OnDismiss` | none | Runs once when the panel is dismissed. The kit does not change the page state. |

```csharp
PopupView(Button("Repeat"))
    .Content(GlazeView(Label("Anything").Center()).Padding(16).CornerRadius(14))
    .IsOpen(State.Open)
    .OnDismiss(() => SetState(s => s.Open = false))
```

Pass a callback to capture the anchor. It is a `ContentView`. The callback receives null when the view unmounts.

`PopupMenu` is that panel with title rows or any view. The page owns `IsOpen`. A tap closes the menu, then runs the row callback.

```csharp
PopupMenu()
    .Anchor(Button("Custom Menu", Open))
    .Items(menu => menu
        .Item("Custom Menu Item 1")
        .Rule()
        .Item(Label("Custom")))
    .Panel(panel => panel.CornerRadius(18))
    .Stack(stack => stack.Spacing(4))
    .Popup(popup => popup.OpenScaleX(0.85).OpenScaleY(0.5))
    .TitleColor(Colors.Black)
    .RuleColor(Color.FromRgba(60, 60, 67, 74))
    .DisableAnchorWhenOpen()
    .IsOpen(State.Open)
    .OnDismiss(() => SetState(s => s.Open = false))
```

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+
- Windows 10.0.17763+

### Switch

`SwitchView` is one on/off pill. The page owns `IsOn`. Track, thumb, stroke, hole, and thumb content are properties, so the same view can take a Material, Cupertino, or Fluent look. The label is any view. Part values are in [docs/switch.md](docs/switch.md).

```csharp
SwitchView()
    .Label("Wi-Fi")
    .IsOn(State.Wifi)
    .OnChanged(on => SetState(s => s.Wifi = on));
```

#### Supported platforms

- iOS 15+
- Mac Catalyst 17+
- Android 21+
- Windows 10.0.17763+

## Handlers

List of available handlers.

### BorderlessEntry

Strips the platform chrome from every `Entry`. It stays off until you opt in.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit(settings => settings.EnableBorderlessEntry = true);
```

### BorderlessEditor

Strips the platform chrome from every `Editor`. It stays off until you opt in.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit(settings => settings.EnableBorderlessEditor = true);
```

## Samples

`samples/KitTests` is a MauiReactor app for the controls. `AppShell` hosts `HomePage`, a content page, with the flyout disabled and the shell navigation bar hidden. It scrolls text behind a top bar, a round button, a button, cards, and entries, and it can switch the page between light and dark. Text, Markdown, Reading, Object, Model, Menu, Sheet, and Switch open with `PushModalAsync` and close with `PopModalAsync`. Model passes the packaged `industrial_microscope.glb` to `PackagedSource`. Sample shadows go through `ShadowElevation`. Level 1, the fallback, is offset `(0.3, 0.3)`, radius 4, opacity 0.28. Level 2 is offset `(0.5, 0.5)`, radius 5, opacity 0.30. Level 3 is offset `(0.7, 0.7)`, radius 6, opacity 0.32. The home buttons and the back chevron use level 1. The menu panel uses level 2.

## Building

```
dotnet build ReactorKit.slnx
```

Package versions are centralized in `Directory.Packages.props`.

CI builds `net10.0` and `net10.0-android` on Ubuntu and `net10.0-ios` on macOS. A tag such as `v0.1.0-alpha.6` packs on macOS and publishes `Reactor.Kit` to NuGet and GitHub Packages.

## License

MIT
