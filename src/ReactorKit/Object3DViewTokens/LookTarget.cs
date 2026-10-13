using System.Runtime.InteropServices;

namespace Reactor.Kit.Object3DViewTokens;

/// <summary>
/// Look-at point in mesh units. NaN components mean the bounds center.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct LookTarget(double X, double Y, double Z)
{
    public static LookTarget Unset { get; } = new(double.NaN, double.NaN, double.NaN);

    public bool IsUnset
        => double.IsNaN(X) && double.IsNaN(Y) && double.IsNaN(Z);
}
