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

The package id is `Reactor.Kit`. The version in this repo is `0.1.0-alpha.1`. The package is not on NuGet yet. Until it is, reference `src/ReactorKit/ReactorKit.csproj`, or run `samples/KitTests`.

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

The namespace is `Reactor.Kit`. Add these lines to the app's global usings.

```csharp
global using Reactor.Kit;
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

`RichTextView` draws paragraph text with SkiaSharp. A tap hits a word or a sentence. A long-press on text that is already highlighted raises its own callback and leaves the highlight in place. Highlights are rectangles behind the text. Characters the body font does not contain, including emoji and other writing systems, are taken from the system fonts. Each word is shaped, so connected scripts stay connected.

#### Properties

| Member | Default | Role |
|---|---|---|
| `Text` | empty | The paragraph. |
| `FontSize` | `17` | Size in device-independent pixels. |
| `LineSpacing` | `4` | Extra pixels between lines. |
| `TextColor` | black | Glyph color. |
| `HighlightColor` | amber at about 47% | Color of the rectangles behind highlighted words. |
| `ToggleHighlightOnTap` | `true` | A tap toggles that word or sentence. The tap callback still runs. |
| `TapTarget` | `Word` | `Sentence` reports and toggles the whole sentence. |
| `HighlightedWords` | none | Word indexes to paint. |
| `HighlightedSentences` | none | Sentence indexes to paint. |
| `HighlightedRanges` | none | Character ranges. A word is highlighted when it overlaps a range. |
| `OnTokenTapped` | none | Receives the tapped word or sentence. |
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

A new list with the same indexes keeps tap toggles. Set `ToggleHighlightOnTap(false)` and pass `HighlightedWords` when the page owns the highlights.

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

`samples/KitTests` is a MauiReactor app for the controls. The glaze page scrolls text behind a top bar, a round button, a button, cards, and entries, and it can switch the page between light and dark. Text opens `RichTextTestPage`.

## Building

```
dotnet build ReactorKit.slnx
```

Package versions are centralized in `Directory.Packages.props`.

CI builds `net10.0` and `net10.0-android` on Ubuntu and `net10.0-ios` on macOS. A tag such as `v0.1.0-alpha.2` packs on macOS and publishes `Reactor.Kit` to NuGet and GitHub Packages.

## License

MIT
