# ReactorKit

Custom controls for [MauiReactor](https://github.com/adospace/reactorui-maui).

`GlazeView` is an acrylic container. The child is a normal MAUI view and is not blurred. Call `UseReactorKit()` at startup, then `global using static Reactor.Kit.GlazeComponent`.

```csharp
GlazeView(Button("Test Button").Center())
    .Padding(16, 16)
    .CornerRadius(22)
```

The shared recipe is a 12dp blur, a very fine dusting of noise, and a 0.7pt hairline of white at 80%. The light tint is a thin Mica wash so content behind the panel stays visible. A dark tint stays heavier at the same setting. Android 11 and earlier keep the tint, noise, and stroke, and raise the tint so the panel is not a hole.

Controls live in `src/ReactorKit`. The package id is `Reactor.Kit`, and the namespace is `Reactor.Kit`. `samples/KitTests` is a MauiReactor app for trying them.

## Layout

- `src/ReactorKit` is the packable library. It targets .NET 10 and .NET MAUI.
- `samples/KitTests` is the sample app.
- Package versions are centralized in `Directory.Packages.props`.

## Building

```bash
dotnet build ReactorKit.slnx
```

## License

MIT
