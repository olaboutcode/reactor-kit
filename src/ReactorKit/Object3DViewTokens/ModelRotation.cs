using System.Runtime.InteropServices;

namespace Reactor.Kit.Object3DViewTokens;

/// <summary>
/// Root yaw, pitch, and roll, in degrees. Applied yaw, then pitch, then roll.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ModelRotation(double Yaw, double Pitch, double Roll);
