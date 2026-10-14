namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Shared sizes for the pop-up list. The pull-down menu uses the same numbers.
/// </summary>
internal static class MenuChrome
{
    public const double PanelRadius = 14;
    public const double PanelPadding = 6;
    public const double RowHeight = 44;
    public const double PressedRadius = 10;
    public const double TitleSize = 17;
    public const double RowPadding = 16;
    public const double MarkColumn = 22;
    public const double MarkGap = 8;
    public const double Separator = 1;
    public const double ButtonHeight = 36;
    public const double ButtonRadius = 14;
    public const double ButtonPadding = 14;
    public const string Placeholder = "Choose";
    public const string Checkmark = "M4,12 L9,17 L18,7";

    public static Color PressedWash()
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return dark
            ? Color.FromRgba(255, 255, 255, 41)
            : Color.FromRgba(0, 0, 0, 31);
    }

    public static Color Hairline()
    {
        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        return dark
            ? Color.FromRgba(255, 255, 255, 40)
            : Color.FromRgba(60, 60, 67, 74);
    }
}
