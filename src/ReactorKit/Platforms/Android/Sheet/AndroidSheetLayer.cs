using Android.Views;
using Android.Widget;
using AndroidX.Activity;
using AndroidX.AppCompat.App;
using AndroidX.Core.View;
using AndroidX.Fragment.App;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace Reactor.Kit.Internals.Sheet;

/// <summary>
/// A dim and a card on the window that contains the owner page.
/// Shared code owns the rectangle. A covered card scales down so its grabber stays visible.
/// </summary>
sealed class AndroidSheetLayer : ISheetLayer, IDisposable
{
    static readonly List<AndroidSheetLayer> Live = [];
    static readonly Dictionary<AViewGroup, AndroidSheetLayer> InsetOwners = [];

    AViewGroup? _root;
    AView? _dim;
    SheetCardView? _card;
    Microsoft.Maui.Controls.View? _host;
    Page? _page;
    ComponentDialog? _dialog;
    BackStop? _back;
    InsetsListener? _insets;
    EventHandler? _layout;
    Action? _metrics;
    Action? _backAction;
    Action? _escape;
    bool _measuring;
    bool _applying;
    bool _chromeSet;
    bool _chromeGrabber;
    double _chromeRadius;
    bool _chromeDark;

    public bool PositionsCard => true;

    public bool TryAttach(Page page, Microsoft.Maui.Controls.View host)
    {
        var context = page.Handler?.MauiContext;
        if (context == null)
            return false;

        var platform = page.ToPlatform(context);
        var activity = platform.Context as AppCompatActivity
            ?? Microsoft.Maui.ApplicationModel.Platform.CurrentActivity as AppCompatActivity;
        if (activity == null)
            return false;

        _dialog = null;
        _root = FindRoot(activity, platform, out _dialog);
        if (_root == null)
            return false;

        _page = page;
        _host = host;
        var native = host.ToPlatform(context);
        _dim = new AView(activity);
        _dim.Visibility = ViewStates.Gone;
        _card = new SheetCardView(activity);
        (native.Parent as AViewGroup)?.RemoveView(native);
        native.Visibility = ViewStates.Visible;
        _card.AddView(native, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));
        _root.AddView(_dim, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.MatchParent));
        _root.AddView(_card, new FrameLayout.LayoutParams(0, 0));
        if (!Live.Contains(this))
            Live.Add(this);

        WatchInsets();
        return true;
    }

    public void Detach()
    {
        ReleaseBack();
        Live.Remove(this);
        ReleaseWatch();
        if (_card != null)
        {
            _card.Pointer = null;
            _card.Escape = null;
            // The card is ours. The MAUI view inside it is not. Leave that view alive for the next open.
            _card.ReleaseContent();
            _root?.RemoveView(_card);
            _card.Dispose();
        }

        if (_dim != null)
        {
            _root?.RemoveView(_dim);
            _dim.Dispose();
        }

        _card = null;
        _dim = null;
        _root = null;
        _host = null;
        _page = null;
        _dialog = null;
        _chromeSet = false;
    }

    public void Dispose()
        => Detach();

    public bool TryMeasure(Page page, out SheetMetrics metrics)
    {
        metrics = new SheetMetrics();
        if (_root == null || _root.Width <= 0 || _root.Height <= 0)
            return false;

        _measuring = true;
        try
        {
            var density = Density(_root);
            var window = _dialog?.Window ?? (_root.Context as AppCompatActivity)?.Window;
            var decor = window?.DecorView;
            var insets = decor == null ? null : ViewCompat.GetRootWindowInsets(decor);
            var bars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
            var ime = insets?.GetInsets(WindowInsetsCompat.Type.Ime());
            var imePx = ime?.Bottom ?? 0;
            var overlap = 0d;
            if (imePx > 0 && decor != null && _root.Height + imePx > decor.Height - 8)
                overlap = imePx / density;

            var safe = new Thickness(
                (bars?.Left ?? 0) / density,
                (bars?.Top ?? 0) / density,
                (bars?.Right ?? 0) / density,
                (bars?.Bottom ?? 0) / density);
            var width = _root.Width / density;
            var height = _root.Height / density;
            metrics = new SheetMetrics
            {
                Width = width,
                Height = height,
                Safe = safe,
                KeyboardOverlap = overlap,
                CompactWidth = width < SheetGeometry.CompactWidthDip,
                CompactHeight = height < SheetGeometry.CompactHeightDip,
                FocusedBottom = FocusedBottom(density),
            };
            return true;
        }
        finally
        {
            _measuring = false;
        }
    }

    /// <summary>Places the card, refreshes its chrome, and sets the dim.</summary>
    public void Apply(SheetFrame frame)
    {
        if (_root == null || _card == null || _dim == null)
            return;

        _applying = true;
        try
        {
            Place(frame);
            Scale(frame);
            Chrome(frame);
            _dim.SetBackgroundColor(Android.Graphics.Color.Black);
            _dim.Alpha = (float)Math.Clamp(frame.DimOpacity, 0, 1);
            _dim.Clickable = frame.DimHits;
            _dim.Visibility = frame.DimOpacity <= 0.01 && !frame.DimHits ? ViewStates.Gone : ViewStates.Visible;
            Order();
        }
        finally
        {
            _applying = false;
        }
    }

    /// <summary>Moves the card onto the frame rectangle, in pixels.</summary>
    void Place(SheetFrame frame)
    {
        if (_root == null || _card == null)
            return;

        var density = Density(_root);
        var card = frame.Card;
        var width = Math.Max(0, (int)(card.Width * density));
        var height = Math.Max(0, (int)(card.Height * density));
        var left = (int)(card.X * density);
        var top = (int)(card.Y * density);
        if (_card.LayoutParameters is not FrameLayout.LayoutParams current
            || current.Width != width
            || current.Height != height
            || current.LeftMargin != left
            || current.TopMargin != top)
        {
            _card.LayoutParameters = new FrameLayout.LayoutParams(width, height)
            {
                LeftMargin = left,
                TopMargin = top,
            };
        }

        _card.Layout(left, top, left + width, top + height);
    }

    /// <summary>Widens the corner of a covered card. The sheet in front keeps its own radius.</summary>
    static double CoveredRadius(SheetFrame frame)
    {
        var t = Math.Clamp(frame.Stack, 0, 1);
        const double covered = 34;
        return frame.CornerRadius + ((Math.Max(covered, frame.CornerRadius) - frame.CornerRadius) * t);
    }

    /// <summary>Shrinks a covered card from its top edge. The sheet in front stays full size.</summary>
    void Scale(SheetFrame frame)
    {
        if (_card == null)
            return;

        var scale = (float)Math.Pow(0.94, Math.Max(0, frame.Stack));
        _card.PivotX = _card.Width / 2f;
        _card.PivotY = 0;
        _card.ScaleX = scale;
        _card.ScaleY = scale;
    }

    /// <summary>Keeps each dim under its own card. A hidden dim stays behind the cards.</summary>
    static void Order()
    {
        foreach (var layer in Live)
        {
            if (layer._dim is { Visibility: ViewStates.Visible, Parent: not null })
                layer._dim.BringToFront();

            if (layer._card is { Parent: not null })
                layer._card.BringToFront();
        }
    }

    /// <summary>Updates the fill, grabber, corner, and content padding when they change.</summary>
    void Chrome(SheetFrame frame)
    {
        if (_card == null)
            return;

        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var radius = CoveredRadius(frame);
        var radiusChanged = Math.Abs(_chromeRadius - radius) > 0.2;
        if (!_chromeSet || _chromeGrabber != frame.Grabber || radiusChanged || _chromeDark != dark)
        {
            _chromeSet = true;
            _chromeGrabber = frame.Grabber;
            _chromeRadius = radius;
            _chromeDark = dark;
            _card.ApplyChrome(frame.Grabber, radius, dark);
        }

        _card.SetGrabberAlpha(frame.Grabber ? (float)Math.Clamp(frame.GrabberAlpha, 0, 1) : 0);
        _card.SetCover((float)Math.Clamp(frame.Stack, 0, 1));

        if (_host is not Microsoft.Maui.Controls.Layout layout)
            return;

        var padding = new Thickness(0, frame.Grabber ? 16 : 0, 0, 0);
        if (layout.Padding != padding)
            layout.Padding = padding;
    }

    public void Watch(Action? metricsChanged, Action? back, Action? escape)
    {
        _metrics = metricsChanged;
        _backAction = back;
        _escape = escape;
        if (_card != null)
            _card.Escape = () => _escape?.Invoke();

        ReleaseBack();
        if (back == null)
            return;

        var dispatcher = _dialog?.OnBackPressedDispatcher
            ?? (_root?.Context as ComponentActivity)?.OnBackPressedDispatcher;
        if (dispatcher == null)
            return;

        _back = new BackStop(() => _backAction?.Invoke());
        dispatcher.AddCallback(_back);
    }

    /// <summary>Stores the pointer. The session places the card.</summary>
    public void PresentNative(SheetNative native)
    {
        if (_card == null)
            return;

        _card.Pointer = native.Pointer;
    }

    public void UpdateNative(SheetNative native)
        => PresentNative(native);

    public void DismissNative()
    {
    }

    /// <summary>Listens for inset and layout changes. The first sheet on a root owns the inset callback.</summary>
    void WatchInsets()
    {
        if (_root == null)
            return;

        _insets = new InsetsListener(Notify);
        if (!InsetOwners.ContainsKey(_root))
        {
            InsetOwners[_root] = this;
            ViewCompat.SetOnApplyWindowInsetsListener(_root, _insets);
        }

        _layout = (_, _) => Notify();
        var observer = _root.ViewTreeObserver;
        if (observer != null)
            observer.GlobalLayout += _layout;
    }

    /// <summary>Drops this sheet's layout watch and leaves the inset callback with a sheet that is still open.</summary>
    void ReleaseWatch()
    {
        if (_layout != null && _root?.ViewTreeObserver != null)
            _root.ViewTreeObserver.GlobalLayout -= _layout;

        _layout = null;
        if (_root == null || _insets == null)
        {
            _insets?.Dispose();
            _insets = null;
            return;
        }

        if (InsetOwners.TryGetValue(_root, out var owner) && ReferenceEquals(owner, this))
        {
            InsetOwners.Remove(_root);
            AndroidSheetLayer? next = null;
            foreach (var layer in Live)
            {
                if (ReferenceEquals(layer._root, _root) && layer._insets != null)
                {
                    next = layer;
                    break;
                }
            }

            if (next == null)
            {
                ViewCompat.SetOnApplyWindowInsetsListener(_root, null);
            }
            else
            {
                InsetOwners[_root] = next;
                ViewCompat.SetOnApplyWindowInsetsListener(_root, next._insets);
            }
        }

        _insets.Dispose();
        _insets = null;
    }

    /// <summary>Reports a size or inset change that this layer did not cause.</summary>
    void Notify()
    {
        if (_measuring || _applying)
            return;

        _metrics?.Invoke();
    }

    void ReleaseBack()
    {
        _back?.Remove();
        _back?.Dispose();
        _back = null;
    }

    double? FocusedBottom(double density)
    {
        var focused = _root?.FindFocus();
        if (focused == null || _root == null)
            return null;

        var location = new int[2];
        focused.GetLocationOnScreen(location);
        var root = new int[2];
        _root.GetLocationOnScreen(root);
        var bottom = (location[1] - root[1] + focused.Height) / density;
        return bottom;
    }

    static AViewGroup? FindRoot(AppCompatActivity activity, AView page, out ComponentDialog? dialog)
    {
        dialog = null;
        foreach (var fragment in activity.SupportFragmentManager.Fragments)
        {
            if (fragment is not DialogFragment shown || !shown.IsAdded || shown.Dialog is not { IsShowing: true })
                continue;

            var content = shown.Dialog.Window?.DecorView.FindViewById(Android.Resource.Id.Content) as AViewGroup;
            if (content == null || !Contains(content, page))
                continue;

            dialog = shown.Dialog as ComponentDialog;
            return content;
        }

        return activity.FindViewById(Android.Resource.Id.Content) as AViewGroup;
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

    static double Density(AView view)
    {
        var density = view.Resources?.DisplayMetrics?.Density ?? 0;
        if (density > 0)
            return density;

        var display = DeviceDisplay.MainDisplayInfo.Density;
        return display > 0 ? display : 1;
    }

    sealed class BackStop : OnBackPressedCallback
    {
        readonly Action _onBack;

        public BackStop(Action onBack)
            : base(true)
            => _onBack = onBack;

        public override void HandleOnBackPressed()
            => _onBack();
    }

    sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        readonly Action _changed;

        public InsetsListener(Action changed)
            => _changed = changed;

        public WindowInsetsCompat? OnApplyWindowInsets(AView? v, WindowInsetsCompat? insets)
        {
            _changed();
            return insets;
        }
    }
}
