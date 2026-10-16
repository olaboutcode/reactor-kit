namespace Reactor.Kit.Internals.Sheet;

static class SheetLayers
{
    public static ISheetLayer Create()
#if ANDROID
        => new AndroidSheetLayer();
#elif IOS || MACCATALYST
        => new AppleSheetLayer();
#elif WINDOWS
        => new WindowsSheetLayer();
#else
        => new NullSheetLayer();
#endif
}
