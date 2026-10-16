using KitTests.Components;

#if DEBUG
using MauiReactor.HotReload;
#endif

namespace KitTests;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiReactorApp<AppShell>(app =>
            {
                app.UseTheme<AppTheme>();
            },
            unhandledExceptionAction: e =>
            {
                System.Diagnostics.Debug.WriteLine(e.ExceptionObject);
            })
#if DEBUG
            .UseMauiReactorHotReload()
#endif
            .UseReactorKit(settings => settings.EnableBorderlessEntry = true)
            .ConfigureFonts(AppTheme.RegisterFonts);

        return builder.Build();
    }
}
