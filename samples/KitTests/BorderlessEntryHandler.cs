using Microsoft.Maui.Handlers;

namespace KitTests;

static class BorderlessEntryHandler
{
    public static void Register()
        => EntryHandler.Mapper.AppendToMapping(nameof(BorderlessEntryHandler), static (handler, _) =>
        {
#if ANDROID
            // The AppCompat background is the underline.
            if (handler.PlatformView is { } field)
            {
                field.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
                field.Background = null;
            }
#elif IOS || MACCATALYST
            if (handler.PlatformView is { } field)
                field.BorderStyle = UIKit.UITextBorderStyle.None;
#elif WINDOWS
            if (handler.PlatformView is { } field)
            {
                var none = new Microsoft.UI.Xaml.Thickness(0);
                field.BorderThickness = none;
                field.Resources["TextControlBorderThemeThickness"] = none;
                field.Resources["TextControlBorderThemeThicknessFocused"] = none;
            }
#endif
        });
}
