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
            .UseReactorKit(settings => settings.EnableBorderlessEntry = true)
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Lexend-Regular.ttf", AppFont.Family);
            });

        UseLexend();
        return builder.Build();
    }

    static void UseLexend()
    {
        var labels = LabelStyles.Default;
        LabelStyles.Default = label =>
        {
            labels?.Invoke(label);
            label
                .FontFamily(AppFont.Family)
                .FontSize(16);
        };

        var buttons = ButtonStyles.Default;
        ButtonStyles.Default = button =>
        {
            buttons?.Invoke(button);
            button
                .FontFamily(AppFont.Family)
                .FontSize(16);
        };

        var entries = EntryStyles.Default;
        EntryStyles.Default = entry =>
        {
            entries?.Invoke(entry);
            entry
                .FontFamily(AppFont.Family)
                .FontSize(16);
        };

        var editors = EditorStyles.Default;
        EditorStyles.Default = editor =>
        {
            editors?.Invoke(editor);
            editor
                .FontFamily(AppFont.Family)
                .FontSize(16);
        };
    }
}
