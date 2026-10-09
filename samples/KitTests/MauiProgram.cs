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
            .UseMauiReactorApp<GlazeTestPage>(app => { },
            unhandledExceptionAction: e =>
            {
                System.Diagnostics.Debug.WriteLine(e.ExceptionObject);
            })
#if DEBUG
            .UseMauiReactorHotReload()
#endif
            .UseReactorKit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        BorderlessEntryHandler.Register();
        return builder.Build();
    }
}
