using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using Reactor.Kit.SheetTokens;
using UIKit;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>Maps page detents onto <see cref="UISheetPresentationController"/>.</summary>
static class SheetDetentBridge
{
    public static readonly NSString SelectedKey = new("selectedDetentIdentifier");
    public static readonly NSString UndimmedKey = new("largestUndimmedDetentIdentifier");

    public static bool CustomDetents
        => OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16);

    public static bool PageSizing
        => OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17);

    public static UISheetPresentationControllerDetent[] Detents(
        SheetProps props,
        SheetMetrics metrics,
        Func<SheetDetent, UISheetPresentationControllerDetent> custom)
    {
        var compactForm = props.Style == SheetStyle.Form && metrics.CompactWidth;
        var page = props.Style == SheetStyle.Page;
        if (!page && !compactForm)
            return [];

        var source = props.Detents.Count == 0 ? [SheetDetent.Large] : props.Detents.Where(static item => item != null).ToArray();
        if (!CustomDetents)
            return Legacy(source, metrics);

        var list = new List<UISheetPresentationControllerDetent>();
        foreach (var detent in source)
        {
            if (detent.Kind == SheetDetentKind.Medium)
                list.Add(UISheetPresentationControllerDetent.CreateMediumDetent());
            else if (detent.Kind == SheetDetentKind.Large)
                list.Add(UISheetPresentationControllerDetent.CreateLargeDetent());
            else
                list.Add(custom(detent));
        }

        if (list.Count == 0)
            list.Add(UISheetPresentationControllerDetent.CreateLargeDetent());

        return [.. list];
    }

    public static string? SelectedIdentifier(SheetProps props, SheetMetrics metrics, string? activeId)
    {
        if (!CustomDetents)
            return LegacyId(props, metrics, activeId);

        return activeId;
    }

    public static void SetIdentifier(UISheetPresentationController sheet, NSString key, string? id)
    {
        if (id is null)
        {
            // setNilValueForKey: raises for this NSString key. setValue:nil forKey: clears it.
            ClearIdentifier(sheet.Handle, Selector.GetHandle("setValue:forKey:"), IntPtr.Zero, key.Handle);
            return;
        }

        // A new NSString("medium") is not the system constant, so UIKit ignores it and rests on the first detent.
        if (TrySystem(id, out var system))
        {
            if (ReferenceEquals(key, UndimmedKey))
                sheet.LargestUndimmedDetentIdentifier = system;
            else
                sheet.SelectedDetentIdentifier = system;

            return;
        }

        sheet.SetValueForKey(new NSString(id), key);
    }

    static bool TrySystem(string id, out UISheetPresentationControllerDetentIdentifier identifier)
    {
        if (string.Equals(id, SheetIds.Medium, StringComparison.Ordinal))
        {
            identifier = UISheetPresentationControllerDetentIdentifier.Medium;
            return true;
        }

        if (string.Equals(id, SheetIds.Large, StringComparison.Ordinal))
        {
            identifier = UISheetPresentationControllerDetentIdentifier.Large;
            return true;
        }

        identifier = default;
        return false;
    }

    [DllImport(Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
    static extern void ClearIdentifier(IntPtr receiver, IntPtr selector, IntPtr value, IntPtr key);

    public static string? ReadIdentifier(UISheetPresentationController sheet)
    {
        var value = sheet.ValueForKey(SelectedKey);
        return value?.ToString();
    }

    static UISheetPresentationControllerDetent[] Legacy(SheetDetent[] source, SheetMetrics metrics)
    {
        var large = SheetGeometry.Large(metrics);
        var medium = false;
        var full = false;
        foreach (var detent in source)
        {
            var id = SheetGeometry.LegacyId(detent, large);
            if (string.Equals(id, SheetIds.Medium, StringComparison.Ordinal) && !metrics.CompactHeight)
                medium = true;
            else
                full = true;
        }

        if (!medium && !full)
            full = true;

        var list = new List<UISheetPresentationControllerDetent>();
        if (medium)
            list.Add(UISheetPresentationControllerDetent.CreateMediumDetent());
        if (full)
            list.Add(UISheetPresentationControllerDetent.CreateLargeDetent());

        return [.. list];
    }

    static string? LegacyId(SheetProps props, SheetMetrics metrics, string? activeId)
    {
        var large = SheetGeometry.Large(metrics);
        SheetDetent? match = null;
        foreach (var detent in props.Detents)
        {
            if (detent != null && activeId != null && string.Equals(detent.Id, activeId, StringComparison.Ordinal))
                match = detent;
        }

        match ??= props.Detents.FirstOrDefault(static item => item != null) ?? SheetDetent.Large;
        var id = SheetGeometry.LegacyId(match, large);
        if (string.Equals(id, SheetIds.Medium, StringComparison.Ordinal) && metrics.CompactHeight)
            return SheetIds.Large;

        return id;
    }
}
