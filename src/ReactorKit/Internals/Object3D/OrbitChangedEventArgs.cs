using Reactor.Kit.Object3DViewTokens;

namespace Reactor.Kit.Internals.Object3D;

sealed class OrbitChangedEventArgs : EventArgs
{
    public OrbitChangedEventArgs(Orbit orbit)
    {
        Orbit = orbit;
    }

    public Orbit Orbit { get; }
}
