using Microsoft.Maui.Handlers;

namespace Reactor.Kit.Handlers;

/// <summary>Removes the platform chrome from every <see cref="Microsoft.Maui.Controls.Editor"/>.</summary>
internal static class BorderlessEditorHandler
{
    /// <summary>Adds the chrome removal to the editor handler.</summary>
    public static void Register()
    {
        EditorHandler.Mapper.AppendToMapping(nameof(BorderlessEditorHandler), Strip);
#if ANDROID
        // These maps set InputType. Android restores the underline when that happens.
        EditorHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IEditor.Background), Strip);
        EditorHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IEditor.Keyboard), Strip);
        EditorHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IEditor.IsTextPredictionEnabled), Strip);
        EditorHandler.Mapper.AppendToMapping(nameof(Microsoft.Maui.IEditor.IsSpellCheckEnabled), Strip);
#endif
    }

    static void Strip(IEditorHandler handler, Microsoft.Maui.IEditor _)
    {
#if ANDROID
        if (handler.PlatformView is { } field)
        {
            field.ViewAttachedToWindow -= OnAttached;
            field.ViewAttachedToWindow += OnAttached;
            Clear(field);
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
    }

#if ANDROID
    /// <summary>Clears the underline again after the editor is attached to a new parent.</summary>
    static void OnAttached(object? sender, Android.Views.View.ViewAttachedToWindowEventArgs args)
    {
        _ = args;
        if (sender is AndroidX.AppCompat.Widget.AppCompatEditText field)
            Clear(field);
    }

    /// <summary>Replaces the material underline with a transparent background and keeps the text padding.</summary>
    static void Clear(AndroidX.AppCompat.Widget.AppCompatEditText field)
    {
        var left = field.PaddingLeft;
        var top = field.PaddingTop;
        var right = field.PaddingRight;
        var bottom = field.PaddingBottom;
        // A null background lets MAUI put abc_edit_text_material back. A transparent color stays.
        field.SetBackgroundColor(Android.Graphics.Color.Transparent);
        field.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
        field.SetPadding(left, top, right, bottom);
    }
#endif
}
