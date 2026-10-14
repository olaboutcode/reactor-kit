namespace Reactor.Kit.Internals.Popup;

static class PopupLayers
{
    public static IPopupLayer Create()
#if ANDROID
        => new AndroidPopupLayer();
#elif IOS || MACCATALYST
        => new ApplePopupLayer();
#elif WINDOWS
        => new WindowsPopupLayer();
#else
        => new NullPopupLayer();
#endif
}
