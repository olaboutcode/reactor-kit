using System.Runtime.InteropServices;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using Reactor.Kit.SheetTokens;
using UIKit;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// The presented controller. UIKit draws the card. This type hosts the MAUI body inside it.
/// </summary>
sealed class SheetController : UIViewController
{
    readonly SheetCallbacks _callbacks = new();
    UIView? _hostView;
    View? _maui;
    SheetNative? _native;
    UITapGestureRecognizer? _tap;
    bool _programmatic;
    bool _dismissNotified;
    int _presentRetries;
    string? _appliedDetents;
    string? _appliedSelected;
    string? _appliedUndimmed;
    bool _reportDetent;
    bool _covering;
    bool _edgeArmed;
    int _edgePasses;

    public SheetController()
    {
        _callbacks.Changed = OnDetent;
        _callbacks.Dismissed = OnDismissed;
        _callbacks.Allow = () => _native?.Props.DismissDisabled != true;
    }

    public void Configure(SheetNative native)
    {
        _native = native;
        _maui = native.Host;
        var context = native.Page.Handler?.MauiContext
            ?? throw new InvalidOperationException("The page is not mounted.");
        var platform = native.Host.ToPlatform(context);
        if (!ReferenceEquals(_hostView, platform))
            _hostView?.RemoveFromSuperview();

        _hostView = platform;
        if (IsViewLoaded && View != null && platform.Superview != View)
            View.AddSubview(platform);

        ModalPresentationStyle = native.Props.Style == SheetStyle.Form
            ? UIModalPresentationStyle.FormSheet
            : UIModalPresentationStyle.PageSheet;
        ModalInPresentation = native.Props.DismissDisabled;
        var sheet = SheetPresentationController
            ?? throw new InvalidOperationException("The sheet controller is missing.");
        sheet.Delegate = _callbacks;
        Apply(sheet, native);
    }

    public void Show(UIViewController presenter)
    {
        if (PresentingViewController != null || presenter.PresentedViewController == this)
            return;

        var coordinator = presenter.GetTransitionCoordinator();
        if (coordinator != null && _presentRetries < 4)
        {
            _presentRetries++;
            coordinator.AnimateAlongsideTransition(static _ => { }, _ => Show(presenter));
            return;
        }

        _presentRetries = 0;
        _dismissNotified = false;
        _reportDetent = false;
        _edgePasses = 0;
        presenter.PresentViewController(this, true, null);
    }

    public void Close()
    {
        if (_dismissNotified)
            return;

        _programmatic = true;
        // A programmatic dismiss does not call DidDismiss. The completion does.
        DismissViewController(true, OnDismissed);
    }

    public override void ViewDidLoad()
    {
        base.ViewDidLoad();
        View!.BackgroundColor = UIColor.SystemBackground;
        AttachHost();
    }

    public override void ViewWillAppear(bool animated)
    {
        base.ViewWillAppear(animated);
        AttachHost();
        if (SheetPresentationController is UISheetPresentationController sheet && _appliedSelected != null)
            SheetDetentBridge.SetIdentifier(sheet, SheetDetentBridge.SelectedKey, _appliedSelected);
    }

    public override void ViewDidAppear(bool animated)
    {
        base.ViewDidAppear(animated);
        AttachHost();
        View?.LayoutIfNeeded();
        // UIKit shrinks this card while another sheet is on top. Restore it when that sheet is gone.
        if (SheetPresentationController is UISheetPresentationController sheet && _native != null)
        {
            sheet.PreferredCornerRadius = (NFloat)(_native.Props.CornerRadius ?? 10);
            sheet.PrefersGrabberVisible = _native.Props.Grabber;
        }

        _reportDetent = true;
        if (_native?.Props.BackdropDismiss != true || _tap != null)
            return;

        _tap = new UITapGestureRecognizer(OnBackdrop);
        _tap.CancelsTouchesInView = false;
        PresentationController?.ContainerView?.AddGestureRecognizer(_tap);
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();
        CoverBottomEdge();
        if (View == null || _hostView == null || _maui == null)
            return;

        var grabber = _native?.Props.Grabber == true ? 16 : 0;
        var width = Math.Max(0, View.Bounds.Width);
        var height = Math.Max(0, View.Bounds.Height - grabber);
        // Arrange writes the platform frame at the origin and would leave this view's background in a strip.
        _maui.Measure(width, height);
        _maui.Arrange(new Rect(0, 0, width, height));
        _hostView.Frame = new CGRect(0, grabber, width, height);
        var fill = OpaqueBackground(_hostView);
        if (fill != null)
            View.BackgroundColor = fill;
    }

    void AttachHost()
    {
        if (View == null || _hostView == null)
            return;

        // A reused controller keeps this view. A new platform view has to move in with it.
        if (_hostView.Superview != View)
            View.AddSubview(_hostView);

        View.SetNeedsLayout();
    }

    public override void ViewWillTransitionToSize(CGSize toSize, IUIViewControllerTransitionCoordinator coordinator)
    {
        base.ViewWillTransitionToSize(toSize, coordinator);
        coordinator.AnimateAlongsideTransition(static _ => { }, _ => MetricsChanged?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? MetricsChanged;

    public override UIKeyCommand[] KeyCommands { get; } =
    [
        UIKeyCommand.Create(new NSString("UIKeyInputEscape"), (UIKeyModifierFlags)0, new Selector("rkSheetEscape:")),
    ];

    [Export("rkSheetEscape:")]
    public void SheetEscape(UIKeyCommand command)
    {
        _ = command;
        if (_native?.Props.DismissDisabled == true)
            return;

        DismissViewController(true, OnDismissed);
    }

    void Apply(UISheetPresentationController sheet, SheetNative native)
    {
        var props = native.Props;
        var metrics = native.Layout.Frame;
        _ = metrics;
        sheet.PrefersGrabberVisible = props.Grabber;
        sheet.PrefersScrollingExpandsWhenScrolledToEdge = props.ScrollingExpands;
        sheet.PrefersEdgeAttachedInCompactHeight = props.EdgeAttachedInCompactHeight;
        sheet.PreferredCornerRadius = (NFloat)(props.CornerRadius ?? 10);
        var measured = Measure(native.Page);
        var presented = PresentingViewController != null;
        // iOS 26 floats this card off the bottom edge. Attach it before UIKit places the card.
        if (CoversBottomEdge(props, measured))
            ApplyBottomEdge(sheet);
        ApplyDetents(sheet, props, measured, presented);
        ApplyWidth(sheet, props, native, measured);
        ApplySelection(sheet, props, measured, native.Layout.Frame.ActiveId, presented);
    }

    void CoverBottomEdge()
    {
        if (_covering || _native == null || SheetPresentationController is not UISheetPresentationController sheet)
            return;

        var measured = Measure(_native.Page);
        if (!CoversBottomEdge(_native.Props, measured))
            return;

        _covering = true;
        try
        {
            ApplyBottomEdge(sheet);
            if (SheetIsMoving(sheet))
            {
                ArmEdge();
                return;
            }

            // UIKit draws the rounded card as the drop shadow, then sets its frame after this view lays out.
            var container = sheet.ContainerView;
            if (container != null)
                ExtendBottom(container);

            var card = DropShadow(sheet) ?? PresentationController?.PresentedView;
            if (card?.Window == null)
            {
                ArmEdge();
                return;
            }

            SquareBottom(card);
            var gap = BottomGap(card);
            if (gap <= 0.5 || gap > EdgeGapLimit)
                return;

            Grow(card, gap);
            GrowFlushChildren(card, gap);
            var presented = PresentationController?.PresentedView;
            if (presented != null && !ReferenceEquals(presented, card))
                ExtendBottom(presented);

            SquareBottom(card);
            ArmEdge();
        }
        finally
        {
            _covering = false;
        }
    }

    const double EdgeGapLimit = 96;

    void ArmEdge()
    {
        if (_edgeArmed || _edgePasses >= 8)
            return;

        _edgeArmed = true;
        _edgePasses++;
        // The present animation is still moving the card. Look again after it rests.
        var wait = SheetPresentationController is UISheetPresentationController sheet && SheetIsMoving(sheet)
            ? 0.45
            : 0;
        NSTimer.CreateScheduledTimer(wait, _ =>
        {
            _edgeArmed = false;
            CoverBottomEdge();
        });
    }

    static void ExtendBottom(UIView view)
    {
        var gap = BottomGap(view);
        if (gap <= 0.5 || gap > EdgeGapLimit)
            return;

        view.ClipsToBounds = false;
        Grow(view, gap);
    }

    static bool SheetIsMoving(UISheetPresentationController sheet)
    {
        return ReadBool(sheet, "_isDragging") || ReadBool(sheet, "_isGeneratingAnimations");
    }

    static UIColor? OpaqueBackground(UIView view)
    {
        var color = view.BackgroundColor;
        if (color != null)
        {
            color.GetRGBA(out _, out _, out _, out var alpha);
            if (alpha > 0.99)
                return color;
        }

        foreach (var child in view.Subviews)
        {
            var found = OpaqueBackground(child);
            if (found != null)
                return found;
        }

        return null;
    }

    static UIView? DropShadow(UISheetPresentationController sheet)
    {
        return MessageView(sheet, "dropShadowView");
    }

    static void SquareBottom(UIView card)
    {
        SendBool(card, "setMasksTopCornersOnly:", true);
        SendVoid(card, "updateCornerClippingViews");
    }

    static void Grow(UIView view, nfloat gap)
    {
        var frame = view.Frame;
        frame.Height += gap;
        view.Frame = frame;
    }

    static void GrowFlushChildren(UIView view, nfloat gap)
    {
        if (gap <= 0.5)
            return;

        // Children that already ended at the old bottom have to grow with the card. The grabber does not.
        var oldBottom = view.Bounds.Height - gap;
        foreach (var child in view.Subviews)
        {
            if (child.Frame.GetMaxY() < oldBottom - 1)
                continue;

            Grow(child, gap);
            GrowFlushChildren(child, gap);
        }
    }

    static nfloat BottomGap(UIView view)
    {
        var window = view.Window;
        var superview = view.Superview;
        if (window == null || superview == null)
            return 0;

        var windowBottom = new CGPoint(0, window.Bounds.GetMaxY());
        var bottom = superview.ConvertPointFromView(windowBottom, window).Y;
        return bottom - view.Frame.GetMaxY();
    }

    static bool CoversBottomEdge(SheetProps props, SheetMetrics measured)
    {
        // A phone sheet is a bottom card, including a form sheet that adapts to one.
        // Trait classes are often unspecified before the sheet is on screen.
        if (UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone)
            return true;

        if (props.Style == SheetStyle.Form && !measured.CompactWidth)
            return false;

        return measured.CompactWidth || measured.CompactHeight;
    }

    static void ApplyBottomEdge(UISheetPresentationController sheet)
    {
        SendBool(sheet, "_setDisableSolariumInsets:", true);
        SendBool(sheet, "_setWantsBottomAttached:", true);
        SendBool(sheet, "_setPrefersSymmetricCorners:", false);
        SendBool(sheet, "_setWantsFloatingInCompactHeight:", false);
        SendBool(sheet, "_setWantsFloatingInRegularWidthCompactHeight:", false);
        SendDouble(sheet, "_setMarginInCompactHeight:", 0);
    }

    static bool ReadBool(NSObject target, string name)
    {
        var selector = new Selector(name);
        if (!target.RespondsToSelector(selector))
            return false;

        return Objc.ReadBool(target.Handle, selector.Handle);
    }

    static void SendBool(NSObject target, string setter, bool value)
    {
        var selector = new Selector(setter);
        if (!target.RespondsToSelector(selector))
            return;

        Objc.SendBool(target.Handle, selector.Handle, value);
    }

    static void SendDouble(NSObject target, string setter, double value)
    {
        var selector = new Selector(setter);
        if (!target.RespondsToSelector(selector))
            return;

        Objc.SendDouble(target.Handle, selector.Handle, value);
    }

    static void SendVoid(NSObject target, string name)
    {
        var selector = new Selector(name);
        if (!target.RespondsToSelector(selector))
            return;

        Objc.SendVoid(target.Handle, selector.Handle);
    }

    static UIView? MessageView(NSObject target, string name)
    {
        var selector = new Selector(name);
        if (!target.RespondsToSelector(selector))
            return null;

        var pointer = Objc.SendView(target.Handle, selector.Handle);
        return pointer == IntPtr.Zero ? null : Runtime.GetNSObject<UIView>(pointer);
    }

    /// <summary>
    /// One delegate per signature. Two DllImports of objc_msgSend share a trampoline and drop the argument.
    /// </summary>
    static class Objc
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void BoolFn(IntPtr receiver, IntPtr selector, byte value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DoubleFn(IntPtr receiver, IntPtr selector, double value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void VoidFn(IntPtr receiver, IntPtr selector);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate byte BoolRetFn(IntPtr receiver, IntPtr selector);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr ViewFn(IntPtr receiver, IntPtr selector);

        static readonly IntPtr Send = Dlfcn.dlsym(Dlfcn.dlopen(Constants.ObjectiveCLibrary, 0), "objc_msgSend");
        static readonly BoolFn BoolMsg = Marshal.GetDelegateForFunctionPointer<BoolFn>(Send);
        static readonly DoubleFn DoubleMsg = Marshal.GetDelegateForFunctionPointer<DoubleFn>(Send);
        static readonly VoidFn VoidMsg = Marshal.GetDelegateForFunctionPointer<VoidFn>(Send);
        static readonly BoolRetFn BoolRet = Marshal.GetDelegateForFunctionPointer<BoolRetFn>(Send);
        static readonly ViewFn ViewMsg = Marshal.GetDelegateForFunctionPointer<ViewFn>(Send);

        public static void SendBool(IntPtr receiver, IntPtr selector, bool value) => BoolMsg(receiver, selector, value ? (byte)1 : (byte)0);

        public static void SendDouble(IntPtr receiver, IntPtr selector, double value) => DoubleMsg(receiver, selector, value);

        public static void SendVoid(IntPtr receiver, IntPtr selector) => VoidMsg(receiver, selector);

        public static bool ReadBool(IntPtr receiver, IntPtr selector) => BoolRet(receiver, selector) != 0;

        public static IntPtr SendView(IntPtr receiver, IntPtr selector) => ViewMsg(receiver, selector);
    }

    void ApplyDetents(UISheetPresentationController sheet, SheetProps props, SheetMetrics measured, bool presented)
    {
        var detentKey = DetentKey(props);
        if (presented && string.Equals(_appliedDetents, detentKey, StringComparison.Ordinal))
            return;

        var detents = SheetDetentBridge.Detents(props, measured, detent => Custom(detent, props));
        if (detents.Length > 0)
            sheet.Detents = detents;

        _appliedDetents = detentKey;
    }

    void ApplyWidth(UISheetPresentationController sheet, SheetProps props, SheetNative native, SheetMetrics measured)
    {
        if (props.Style == SheetStyle.Form && !measured.CompactWidth)
        {
            PreferredContentSize = new CGSize(native.Layout.Frame.Card.Width, native.Layout.Frame.Card.Height);
            return;
        }

        if (props.WidthFollowsPreferredSize)
        {
            sheet.WidthFollowsPreferredContentSizeWhenEdgeAttached = true;
            PreferredContentSize = new CGSize(props.PreferredWidth, measured.Height);
            return;
        }

        if (!props.PageSizing || props.Style != SheetStyle.Page)
            return;

        if (OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17))
        {
            sheet.PrefersPageSizing = true;
            return;
        }

        sheet.WidthFollowsPreferredContentSizeWhenEdgeAttached = true;
        PreferredContentSize = new CGSize(SheetGeometry.ReadableWidth, measured.Height);
    }

    void ApplySelection(
        UISheetPresentationController sheet,
        SheetProps props,
        SheetMetrics measured,
        string? activeId,
        bool presented)
    {
        var selected = SheetDetentBridge.SelectedIdentifier(props, measured, activeId);
        var undimmed = props.UndimmedDetent is null
            ? null
            : SheetDetentBridge.SelectedIdentifier(props, measured, props.UndimmedDetent);
        var writeSelected = !presented || !string.Equals(_appliedSelected, selected, StringComparison.Ordinal);
        var writeUndimmed = !presented || !string.Equals(_appliedUndimmed, undimmed, StringComparison.Ordinal);
        if (!writeSelected && !writeUndimmed)
            return;

        // A drag resizes this controller. Writing the previous id on that pass pulls the sheet back.
        void Write()
        {
            if (writeSelected)
            {
                SheetDetentBridge.SetIdentifier(sheet, SheetDetentBridge.SelectedKey, selected);
                _appliedSelected = selected;
            }

            if (writeUndimmed)
            {
                SheetDetentBridge.SetIdentifier(sheet, SheetDetentBridge.UndimmedKey, undimmed);
                _appliedUndimmed = undimmed;
            }
        }

        if (presented && writeSelected)
            sheet.AnimateChanges(Write);
        else
            Write();
    }

    static string DetentKey(SheetProps props)
    {
        if (props.Detents.Count == 0)
            return $"{(int)props.Style}:default";

        var parts = new string[props.Detents.Count];
        for (var i = 0; i < props.Detents.Count; i++)
        {
            var detent = props.Detents[i];
            parts[i] = detent is null
                ? ""
                : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{detent.Id}:{(int)detent.Kind}:{detent.Value}");
        }

        return $"{(int)props.Style}:{string.Join('|', parts)}";
    }

    UISheetPresentationControllerDetent Custom(SheetDetent detent, SheetProps props)
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(16) && !OperatingSystem.IsMacCatalystVersionAtLeast(16))
        {
            return UISheetPresentationControllerDetent.CreateLargeDetent();
        }

        return UISheetPresentationControllerDetent.Create(detent.Id, context =>
        {
            // The sheet's own view is only as tall as the current detent. Measure the page behind it.
            var cap = OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16)
                ? (double)context.MaximumDetentValue
                : 0d;
            var view = PresentingViewController?.View;
            var safe = view?.SafeAreaInsets ?? default;
            var measured = view?.Bounds.Height ?? 0;
            var large = measured > 1
                ? Math.Max(0, measured - Math.Max(safe.Top, 10))
                : cap;
            var shared = SheetGeometry.Target(detent, large);
            var edge = TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact
                || (TraitCollection.VerticalSizeClass == UIUserInterfaceSizeClass.Compact && props.EdgeAttachedInCompactHeight);
            var raw = edge ? shared - safe.Bottom : shared;
            var limit = cap > 0 ? cap : Math.Max(raw, 0);
            return (NFloat)Math.Clamp(raw, 0, limit);
        });
    }

    static SheetMetrics Measure(Page page)
    {
        var context = page.Handler?.MauiContext;
        if (context == null)
            return new SheetMetrics();

        var view = page.ToPlatform(context);
        var traits = (page.Handler as IPlatformViewHandler)?.ViewController?.TraitCollection;
        return new SheetMetrics
        {
            Width = view.Bounds.Width,
            Height = view.Bounds.Height,
            Safe = new Thickness(view.SafeAreaInsets.Left, view.SafeAreaInsets.Top, view.SafeAreaInsets.Right, view.SafeAreaInsets.Bottom),
            CompactWidth = traits?.HorizontalSizeClass == UIUserInterfaceSizeClass.Compact,
            CompactHeight = traits?.VerticalSizeClass == UIUserInterfaceSizeClass.Compact,
        };
    }

    void OnBackdrop(UITapGestureRecognizer recognizer)
    {
        if (_native?.Props.BackdropDismiss != true || _native.Props.DismissDisabled || !_native.Layout.Frame.DimHits)
            return;

        var container = PresentationController?.ContainerView;
        var presented = PresentationController?.PresentedView;
        if (container == null || presented == null)
            return;

        var point = recognizer.LocationInView(container);
        if (presented.Frame.Contains(point))
            return;

        DismissViewController(true, OnDismissed);
    }

    void OnDetent(string? id)
    {
        if (_programmatic || !_reportDetent)
            return;

        _native?.DetentSettled?.Invoke(id ?? "");
    }

    void OnDismissed()
    {
        if (_dismissNotified)
            return;

        _dismissNotified = true;
        _programmatic = false;
        _appliedDetents = null;
        _appliedSelected = null;
        _appliedUndimmed = null;
        _reportDetent = false;
        if (_tap != null)
        {
            _tap.View?.RemoveGestureRecognizer(_tap);
            _tap = null;
        }

        _native?.Dismissed?.Invoke();
    }

    sealed class SheetCallbacks : UISheetPresentationControllerDelegate
    {
        public Action<string?>? Changed { get; set; }

        public Action? Dismissed { get; set; }

        public Func<bool>? Allow { get; set; }

        public override void DidChangeSelectedDetentIdentifier(UISheetPresentationController sheetPresentationController)
        {
            Changed?.Invoke(SheetDetentBridge.ReadIdentifier(sheetPresentationController));
        }

        public override bool ShouldDismiss(UIPresentationController presentationController)
        {
            _ = presentationController;
            return Allow?.Invoke() ?? true;
        }

        public override void DidDismiss(UIPresentationController presentationController)
        {
            _ = presentationController;
            Dismissed?.Invoke();
        }
    }
}
