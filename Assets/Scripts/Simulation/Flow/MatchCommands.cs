using Drewio.UnityToolkit.Core;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>Published by the result screen. UI never references the match flow; the composition root listens.</summary>
    public readonly struct RematchRequestedEvent : IGameEvent
    {
    }

    public readonly struct QuitToMenuRequestedEvent : IGameEvent
    {
    }
}
