namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// A per-tick source of SimInputFrame - the single contract the round runner
    /// pulls from whether the frame comes from a played-back recording, the AI or
    /// live device input. SimWorld itself never sees this interface; it only takes
    /// the two resulting frames.
    /// </summary>
    public interface ISimInputSource
    {
        /// <summary>
        /// tickIndex is the number of ticks already completed in the world being
        /// driven (0, 1, 2, ...). Recording frame N lines up with playback tick N
        /// because both are driven by SimRoundRunner, one call per world tick.
        /// May have side effects (a live source consumes its input latches).
        /// </summary>
        SimInputFrame GetInput(int tickIndex);
    }
}
