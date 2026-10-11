using System.Runtime.InteropServices;

namespace Reactor.Kit;

/// <summary>
/// A character range.
/// <see cref="Start"/> is inclusive and the end is <see cref="Start"/> + <see cref="Length"/>.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct TextRange(int Start, int Length);
