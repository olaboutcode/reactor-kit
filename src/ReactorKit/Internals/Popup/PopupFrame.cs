using System.Runtime.InteropServices;

namespace Reactor.Kit.Internals.Popup;

[StructLayout(LayoutKind.Auto)]
readonly record struct PopupFrame(Rect Panel, bool Above, bool AnchorVisible, bool Scrolls);
