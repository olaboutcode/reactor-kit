using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using Reactor.Kit.Internals.Sheet;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Reactor.Kit.Internals.Popup;

sealed class AndroidPopupLayer : IPopupLayer, IDisposable
{
    AViewGroup? _pageView;
    AView? _overlayView;
    AndroidBackStop? _back;
    Action? _onEscape;

    public void Insert(Page page, View overlay, VisualElement anchor)
    {
        var context = Context(page);
        _pageView = Host(page, anchor);
        _overlayView = overlay.ToPlatform(context);
        (_overlayView.Parent as AViewGroup)?.RemoveView(_overlayView);
        _overlayView.LayoutParameters = new AViewGroup.LayoutParams(
            AViewGroup.LayoutParams.MatchParent,
            AViewGroup.LayoutParams.MatchParent);
        // The card brings its grabber to the front on every layout. Elevation keeps the panel above that.
        var lift = 24 * (float)Density(_pageView);
        _overlayView.Elevation = lift;
        _overlayView.TranslationZ = lift;
        _pageView.AddView(_overlayView);
        _overlayView.BringToFront();
    }

    public void Remove(View overlay)
    {
        ReleaseBack();
        ReleaseEscape();
        if (_pageView != null && _overlayView != null)
            _pageView.RemoveView(_overlayView);

        _overlayView = null;
        _pageView = null;
        _ = overlay;
    }

    public void Sync(Page page, View overlay)
    {
        if (_pageView == null || _overlayView == null)
            return;

        _overlayView.Layout(0, 0, _pageView.Width, _pageView.Height);
        _overlayView.BringToFront();
        _overlayView.Elevation = 24 * (float)Density(_pageView);
        _overlayView.TranslationZ = _overlayView.Elevation;
        _ = page;
        _ = overlay;
    }

    public void SetClip(View view, bool clip)
    {
        if (view.Handler?.PlatformView is not AViewGroup group)
            return;

        group.SetClipChildren(clip);
        group.SetClipToPadding(clip);

        // MauiScrollView is created with ClipToOutline set. That clips drawing to the
        // view rectangle after clipChildren is off, so a shadow stops where the panel curves.
        if (group is MauiScrollView)
            group.ClipToOutline = clip;
    }

    public void ReleaseShadowClip(View panel)
    {
        if (panel.Handler?.MauiContext is not IMauiContext context)
            return;

        // ToPlatform is the shadow wrapper when the panel has a shadow. Every
        // ancestor up to the page is the panel's size or a scroll view created
        // with an outline clip, and each one cuts the shadow off at the corners.
        var native = panel.ToPlatform(context);
        var current = native.Parent as AView;
        while (current != null && !ReferenceEquals(current, _pageView))
        {
            if (current is AViewGroup group)
            {
                group.SetClipChildren(false);
                group.SetClipToPadding(false);
                group.ClipToOutline = false;
                group.ClipBounds = null;
            }

            current = current.Parent as AView;
        }

        native.Invalidate();
    }

    public Rect PageBounds(Page page, VisualElement anchor)
    {
        var view = _pageView ?? Host(page, anchor);
        var density = Density(view);
        return new Rect(0, 0, view.Width / density, view.Height / density);
    }

    public Rect AnchorBounds(VisualElement anchor, Page page)
    {
        var context = Context(page);
        var anchorView = PlacedAnchor(anchor, context);
        var host = _pageView ?? Host(page, anchor);
        if (!anchorView.IsAttachedToWindow || !host.IsAttachedToWindow)
            return Rect.Zero;

        var density = Density(host);
        var anchorLocation = new int[2];
        var hostLocation = new int[2];
        anchorView.GetLocationOnScreen(anchorLocation);
        host.GetLocationOnScreen(hostLocation);
        return new Rect(
            (anchorLocation[0] - hostLocation[0]) / density,
            (anchorLocation[1] - hostLocation[1]) / density,
            anchorView.Width / density,
            anchorView.Height / density);
    }

    /// <summary>
    /// Dimming the anchor sets <see cref="VisualElement.InputTransparent"/>, and Android wraps the
    /// view for that. The wrap has no size yet: the sheet card laid out the view, and the page will
    /// not lay the wrap out. The view underneath still has the button's size and position.
    /// </summary>
    static AView PlacedAnchor(VisualElement anchor, IMauiContext context)
    {
        var presented = anchor.ToPlatform(context);
        if (presented.Width > 0 && presented.Height > 0)
            return presented;

        if (anchor.Handler?.PlatformView is AView platform && platform.Width > 0 && platform.Height > 0)
            return platform;

        return presented;
    }

    public Thickness SafeInsets(Page page, VisualElement anchor)
    {
        var host = _pageView ?? Host(page, anchor);
        var decor = Platform.CurrentActivity?.Window?.DecorView;
        if (decor == null)
            return new Thickness(0);

        var visible = new Android.Graphics.Rect();
        decor.GetWindowVisibleDisplayFrame(visible);
        var location = new int[2];
        host.GetLocationOnScreen(location);
        var density = Density(host);
        var top = Math.Max(0, visible.Top - location[1]) / density;
        var left = Math.Max(0, visible.Left - location[0]) / density;
        var bottom = Math.Max(0, location[1] + host.Height - visible.Bottom) / density;
        var right = Math.Max(0, location[0] + host.Width - visible.Right) / density;
        return new Thickness(left, top, right, bottom);
    }

    /// <summary>
    /// The sheet card that contains the anchor, or the activity content root when there is no sheet.
    /// A card is drawn above the page, so a layer on the page or on the content root stays behind it.
    /// </summary>
    static AViewGroup Host(Page page, VisualElement anchor)
    {
        if (anchor.Handler != null)
        {
            var current = anchor.ToPlatform(Context(page)).Parent as AView;
            while (current != null)
            {
                if (current is SheetCardView card)
                    return card;

                current = current.Parent as AView;
            }
        }

        return WindowRoot(page);
    }

    static AViewGroup WindowRoot(Page page)
    {
        var platform = page.ToPlatform(Context(page));
        var activity = platform.Context as AppCompatActivity
            ?? Platform.CurrentActivity as AppCompatActivity;
        if (activity != null)
        {
            foreach (var fragment in activity.SupportFragmentManager.Fragments)
            {
                if (fragment is not DialogFragment shown || !shown.IsAdded || shown.Dialog is not { IsShowing: true })
                    continue;

                var content = shown.Dialog.Window?.DecorView.FindViewById(Android.Resource.Id.Content) as AViewGroup;
                if (content != null && Contains(content, platform))
                    return content;
            }

            if (activity.FindViewById(Android.Resource.Id.Content) is AViewGroup root)
                return root;
        }

        return platform as AViewGroup
            ?? throw new InvalidOperationException("The page has no native view.");
    }

    static bool Contains(AViewGroup root, AView target)
    {
        if (ReferenceEquals(root, target))
            return true;

        for (var i = 0; i < root.ChildCount; i++)
        {
            var child = root.GetChildAt(i);
            if (child == null)
                continue;

            if (ReferenceEquals(child, target))
                return true;

            if (child is AViewGroup group && Contains(group, target))
                return true;
        }

        return false;
    }

    public void CaptureBack(Action onBack)
    {
        ReleaseBack();
        if (Platform.CurrentActivity is not AndroidX.Activity.ComponentActivity activity)
            return;

        _back = new AndroidBackStop(onBack);
        activity.OnBackPressedDispatcher.AddCallback(_back);
    }

    public void ReleaseBack()
    {
        var back = _back;
        _back = null;
        if (back == null)
            return;

        back.Remove();
        back.Dispose();
    }

    public void Dispose()
        => ReleaseBack();

    public void CaptureEscape(Action onEscape)
    {
        ReleaseEscape();
        if (_overlayView == null)
            return;

        _onEscape = onEscape;
        _overlayView.Focusable = true;
        _overlayView.FocusableInTouchMode = true;
        _overlayView.KeyPress += OnKeyPress;
        _overlayView.RequestFocus();
    }

    public void ReleaseEscape()
    {
        if (_overlayView != null)
            _overlayView.KeyPress -= OnKeyPress;

        _onEscape = null;
    }

    void OnKeyPress(object? sender, AView.KeyEventArgs e)
    {
        if (e.Event?.Action != Android.Views.KeyEventActions.Down || e.KeyCode != Android.Views.Keycode.Escape)
            return;

        e.Handled = true;
        _onEscape?.Invoke();
    }

    static IMauiContext Context(Page page)
        => page.Handler?.MauiContext ?? throw new InvalidOperationException("The page is not mounted.");

    static double Density(AView view)
    {
        var density = view.Resources?.DisplayMetrics?.Density ?? 0;
        if (density > 0)
            return density;

        var display = DeviceDisplay.MainDisplayInfo.Density;
        return display > 0 ? display : 1;
    }
}
