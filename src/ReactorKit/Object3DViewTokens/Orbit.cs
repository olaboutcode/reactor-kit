using System.Runtime.InteropServices;

namespace Reactor.Kit.Object3DViewTokens;

/// <summary>
/// Settled camera pose, in degrees and mesh units.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct Orbit(
    double Yaw,
    double Pitch,
    double Distance,
    double TargetX,
    double TargetY,
    double TargetZ);
