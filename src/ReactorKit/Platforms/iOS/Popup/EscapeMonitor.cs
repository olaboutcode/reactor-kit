using Foundation;
using UIKit;

namespace Reactor.Kit.Internals.Popup;

sealed class EscapeMonitor : UIView
{
    public Action? Escaped { get; set; }

    public override bool CanBecomeFirstResponder => true;

    public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
    {
        foreach (var press in presses)
        {
            if (press.Key?.KeyCode == UIKeyboardHidUsage.KeyboardEscape)
            {
                Escaped?.Invoke();
                return;
            }
        }

        base.PressesBegan(presses, evt);
    }
}
