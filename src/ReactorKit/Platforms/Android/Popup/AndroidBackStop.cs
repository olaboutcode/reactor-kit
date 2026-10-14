using AndroidX.Activity;

namespace Reactor.Kit.Internals.Popup;

sealed class AndroidBackStop : OnBackPressedCallback
{
    readonly Action _onBack;

    public AndroidBackStop(Action onBack)
        : base(enabled: true)
    {
        _onBack = onBack;
    }

    public override void HandleOnBackPressed()
        => _onBack();
}
