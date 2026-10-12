using Reactor.Kit.ObjectViewTokens;

namespace Reactor.Kit.Internals.Object3D;

sealed class OrbitChangedEventArgs : EventArgs
{
    public OrbitChangedEventArgs(Orbit orbit)
    {
        Orbit = orbit;
    }

    public Orbit Orbit { get; }
}
