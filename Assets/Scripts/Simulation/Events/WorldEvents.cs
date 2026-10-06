using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;

namespace LittleDelay.Simulation.Events
{
    /// <summary>
    /// A new SimWorld exists (recording or battle). Views subscribe to this to (re)bind to
    /// the live simulation state. The payload is a read-only handle by convention: views read, never mutate.
    /// </summary>
    public readonly struct SimWorldCreatedEvent : IGameEvent
    {
        public readonly SimWorld World;
        public readonly SimWorldMode Mode;

        public SimWorldCreatedEvent(SimWorld world, SimWorldMode mode)
        {
            World = world;
            Mode = mode;
        }
    }

    /// <summary>A world is being replaced; views should drop whatever they were showing for it.</summary>
    public readonly struct SimWorldRetiredEvent : IGameEvent
    {
        public readonly SimWorld World;

        public SimWorldRetiredEvent(SimWorld world)
        {
            World = world;
        }
    }
}
