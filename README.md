# Reactor.Kit

A library of custom controls for [MauiReactor](https://github.com/adospace/reactorui-maui). Each control is a MauiReactor component. The child stays a normal MAUI view.

`GlazeView` is the first control. It is an acrylic panel.

> [!WARNING]
> The library is still experimental. Use it at your own risk. Expect breaking changes as the API settles.

## Table of contents

- [Overview](#overview)
- [Getting started](#getting-started)
  - [Install](#install)
  - [Example](#example)
  - [Namespaces](#namespaces)
  - [Host](#host)
- [Components](#components)
  - [Glaze](#glaze)
    - [Properties](#properties)
    - [Platform](#platform)
- [Handlers](#handlers)
  - [BorderlessEntry](#borderlessentry)
  - [BorderlessEditor](#borderlesseditor)
- [Samples](#samples)
- [Building](#building)
- [License](#license)

## Overview

Controls live in `src/ReactorKit`. The package id is `Reactor.Kit`, and the namespace is `Reactor.Kit`. The library targets .NET 10 and .NET MAUI.

| Control | What it is |
|---|---|
| `GlazeView` | An acrylic panel. The child is not blurred. |

More controls will land beside `GlazeView`. The page for a control is its own section. The host registration is shared.

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

One child becomes the content. Several children are laid out in a grid, because a content view keeps a single child.

```csharp
GlazeView(
    Label("Title"),
    Label("Two children, one grid.")
)
.Padding(16)
.CornerRadius(22)
```

### Namespaces

The namespace is `Reactor.Kit`. You call `GlazeView(...)` and `UseReactorKit()`.

```csharp
global using Reactor.Kit;
global using static Reactor.Kit.Extensions;
```

Add both lines to the app's global usings. `samples/KitTests` already has them.

| Call | Role |
|---|---|
| `GlazeView(...)` | Factory. One child is the content. Several children go in a grid. The panel it returns is `GlazeView`. |
| `UseReactorKit()` | Registers the control handlers on the MAUI app builder. |

`TintColor`, `CornerRadius`, `Padding`, `Shadow`, and the other fluent methods are called on the panel the factory returns. The snippets below assume those usings, plus `MauiReactor`.

### Host

Call `UseReactorKit()` once, on the app builder, after `UseMauiReactorApp`.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit();
```

Without that call the panel has no platform handler.

## Components

List of available components.

### Glaze

`GlazeView` blurs what is behind the panel. The child is a normal MAUI view and is never blurred.

Tint, noise, and the hairline use the same numbers on every OS. The light tint is a thin wash. A dark tint stays heavier at the same setting. Blur is platform-aware, because an 8dp gaussian is not the same backlight as the system ultra-thin material.

A MAUI `Shadow` follows the rounded corner. On Android that shadow is left out of the backdrop blur, so a small button does not pick up its own shadow as a dark fill.

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

#### Platform

| Platform | Backlight |
|---|---|
| iOS 15+, Mac Catalyst 17+ | `SystemUltraThinMaterial`. `BlurRadius` is only an on/off. The material paints its own milk, then the shared wash. |
| Android 12+ (API 31) | A downsampled snapshot, blurred by `BlurRadius` (32dp unless you set it), then a material fill, then the same wash. The light fill is 0.44 of `#F3F3F3`. The night fill is 0.55 of `#1C1C1E`, which is 1.25× the light fill. Apple does not publish the ultra-thin fill, so those two numbers are matched by eye. |
| Android 11 and earlier | No blur. Tint, noise, and the hairline remain. The tint is raised, with a floor of 0.76, so the panel is not a hole. |
| Windows 10.0.17763+ | An acrylic brush on the backdrop. The shared wash is the tint. Luminosity stays at 0.05 so the brush does not add a second veil. |

## Handlers

List of available handlers.

### BorderlessEntry

Strips the platform chrome from every `Entry`. It stays off until you opt in.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit(settings => settings.EnableBorderlessEntry = true);
```

| Platform | What is removed |
|---|---|
| iOS 15+, Mac Catalyst 17+ | The `UITextField` border. |
| Android | The AppCompat underline. |
| Windows 10.0.17763+ | The text-box border, including the focused thickness. |

### BorderlessEditor

Strips the platform chrome from every `Editor`. It stays off until you opt in.

```csharp
builder
    .UseMauiReactorApp<App>()
    .UseReactorKit(settings => settings.EnableBorderlessEditor = true);
```

| Platform | What is removed |
|---|---|
| iOS 15+, Mac Catalyst 17+ | The `UITextView` layer border. |
| Android | The AppCompat underline. |
| Windows 10.0.17763+ | The text-box border, including the focused thickness. |

## Samples

`samples/KitTests` is a MauiReactor app for the controls. The glaze page scrolls text behind a top bar, a round button, a button, cards, and entries, and it can switch the page between light and dark.

## Building

```
dotnet build ReactorKit.slnx
```

Package versions are centralized in `Directory.Packages.props`.

## License

MIT
