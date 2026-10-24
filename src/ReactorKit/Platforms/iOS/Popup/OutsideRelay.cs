using CoreGraphics;
using UIKit;

namespace Reactor.Kit.Internals.Popup;

/// <summary>
/// Hosts the popup layer. Points outside the panel fall through to the sheet.
/// Points on the panel stay with the panel.
/// </summary>
sealed class OutsideRelay : UIView
{
    public UIView? Panel { get; set; }

    public override UIView? HitTest(CGPoint point, UIEvent? uievent)
    {
        if (Panel == null)
            return null;

        var local = ConvertPointToView(point, Panel);
        if (!Panel.PointInside(local, uievent))
            return null;

        return base.HitTest(point, uievent);
    }
}
