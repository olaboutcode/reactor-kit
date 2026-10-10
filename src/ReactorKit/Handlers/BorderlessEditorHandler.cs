using Microsoft.Maui.Handlers;

namespace Reactor.Kit.Handlers;

internal static class BorderlessEditorHandler
{
    public static void Register()
        => EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditorHandler), static (handler, _) =>
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
                field.Layer.BorderWidth = 0;
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
