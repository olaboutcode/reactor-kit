using Foundation;
using UIKit;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Closes the panel when a touch begins outside it. The touch keeps going to the view underneath.
/// </summary>
sealed class OutsideMonitor : UIGestureRecognizer
{
    public Action? Outside { get; set; }

    public UIView? Panel { get; set; }

    public OutsideMonitor()
    {
        CancelsTouchesInView = false;
        DelaysTouchesBegan = false;
        DelaysTouchesEnded = false;
        ShouldRecognizeSimultaneously = (_, _) => true;
    }

    public override void TouchesBegan(NSSet touches, UIEvent evt)
    {
        base.TouchesBegan(touches, evt);
        if (touches.AnyObject is UITouch touch && !Inside(touch, evt))
            Outside?.Invoke();

        State = UIGestureRecognizerState.Failed;
    }

    bool Inside(UITouch touch, UIEvent evt)
    {
        if (Panel == null)
            return false;

        return Panel.PointInside(touch.LocationInView(Panel), evt);
    }
}
