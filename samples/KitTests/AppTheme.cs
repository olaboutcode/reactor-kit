using MauiAppTheme = Microsoft.Maui.ApplicationModel.AppTheme;

namespace KitTests;

public class AppTheme : Theme
{
    internal const string FontFamily = "Lexend";
    const string FontFile = "Lexend-Regular.ttf";

    internal static void RegisterFonts(IFontCollection fonts)
        => fonts.AddFont(FontFile, FontFamily);

    public static void ToggleCurrentAppTheme()
    {
        Application.Current?.UserAppTheme = IsDarkTheme
            ? MauiAppTheme.Light
            : MauiAppTheme.Dark;
    }

    internal static void Apply(bool dark)
    {
        if (Application.Current is { } app)
            app.UserAppTheme = dark ? MauiAppTheme.Dark : MauiAppTheme.Light;
    }

    protected override void OnApply()
    {
        var labels = LabelStyles.Default;
        LabelStyles.Default = label =>
        {
            labels?.Invoke(label);
            label
                .FontFamily(FontFamily)
                .FontSize(16);
        };

        var buttons = ButtonStyles.Default;
        ButtonStyles.Default = button =>
        {
            buttons?.Invoke(button);
            button
                .FontFamily(FontFamily)
                .FontSize(16);
        };

        var entries = EntryStyles.Default;
        EntryStyles.Default = entry =>
        {
            entries?.Invoke(entry);
            entry
                .FontFamily(FontFamily)
                .FontSize(16);
        };

        var editors = EditorStyles.Default;
        EditorStyles.Default = editor =>
        {
            editors?.Invoke(editor);
            editor
                .FontFamily(FontFamily)
                .FontSize(16);
        };
    }
}
