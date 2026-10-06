using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Reveal;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>
    /// Supplies the live input source for each fighter during a Recording phase. The composition root decides
    /// who is who: the player slot gets the device-backed source, the opponent slot gets the AI bot today and
    /// a second human or a network source later - SimMatch never changes.
    /// </summary>
    public interface ISimRecordingInputProvider
    {
        ISimInputSource CreateSource(FighterSlot slot, SimWorld world, ISimIntelSource intel);
    }
}
