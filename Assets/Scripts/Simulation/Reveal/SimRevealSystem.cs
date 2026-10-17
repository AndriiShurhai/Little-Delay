using System;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Simulation.Reveal
{
    /// <summary>What one fighter last learned about the other through a reveal window.</summary>
    public readonly struct SimIntel
    {
        public readonly bool IsKnown;
        public readonly Vector3 Position;
        public readonly Vector3 Velocity;
        /// <summary>World ticks completed when this was observed.</summary>
        public readonly int ObservedTick;

        public SimIntel(Vector3 position, Vector3 velocity, int observedTick)
        {
            IsKnown = true;
            Position = position;
            Velocity = velocity;
            ObservedTick = observedTick;
        }
    }

    /// <summary>Everything a fighter is allowed to know about its opponent while recording blind.</summary>
    public interface ISimIntelSource
    {
        SimIntel GetIntelAbout(FighterSlot subject);
    }

    /// <summary>
    /// Pluggable reveal feed. Offline and on the authoritative server, SimRevealSystem reads both fighters;
    /// on a network client in M3, a NetworkRevealSource can publish the same events from server messages.
    /// </summary>
    public interface IRevealSource : ISimIntelSource
    {
        void Tick(int completedTicks);
    }

    /// <summary>
    /// Runs the reveal schedule (GDD v0.1: 3 windows per recording, one every 7s starting at tick 0, each 2s).
    /// While a window is open each fighter is "seen": trail-point events drive the HUD and the same observation
    /// is stored as intel for the AI - so the bot learns exactly what a human would see, and outside a window
    /// only has stale intel to extrapolate from. Recording-mode only.
    /// </summary>
    public sealed class SimRevealSystem : IRevealSource
    {
        private readonly IEventBus _eventBus;
        private readonly SimFighter _player;
        private readonly SimFighter _opponent;
        private readonly int[] _windowStartTicks;
        private readonly int _windowDurationTicks;
        private readonly float _windowDurationSeconds;
        private readonly SimIntel[] _intel = new SimIntel[2];

        public SimRevealSystem(SimMatchConfig config, IEventBus eventBus, SimFighter player, SimFighter opponent)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _opponent = opponent ?? throw new ArgumentNullException(nameof(opponent));

            float dt = config.FixedDeltaTime;
            _windowDurationSeconds = config.RevealDurationSeconds;
            _windowDurationTicks = Mathf.Max(1, Mathf.RoundToInt(config.RevealDurationSeconds / dt));
            _windowStartTicks = new int[Mathf.Max(0, config.RevealsPerRecording)];
            for (int i = 0; i < _windowStartTicks.Length; i++)
            {
                _windowStartTicks[i] = Mathf.RoundToInt(i * config.RevealIntervalSeconds / dt);
            }
        }

        public SimIntel GetIntelAbout(FighterSlot subject) => _intel[(int)subject];

        /// <summary>Called once at the end of every Recording-mode tick, with the number of ticks completed so far.</summary>
        public void Tick(int completedTicks)
        {
            int tickJustRun = completedTicks - 1;

            for (int i = 0; i < _windowStartTicks.Length; i++)
            {
                int start = _windowStartTicks[i];
                if (tickJustRun < start || tickJustRun >= start + _windowDurationTicks) continue;

                if (tickJustRun == start)
                {
                    _eventBus.Publish(new RevealWindowStartedEvent(FighterSlot.Player, i, _windowDurationSeconds));
                    _eventBus.Publish(new RevealWindowStartedEvent(FighterSlot.Opponent, i, _windowDurationSeconds));
                }

                Observe(_player, i, completedTicks);
                Observe(_opponent, i, completedTicks);

                if (tickJustRun == start + _windowDurationTicks - 1)
                {
                    _eventBus.Publish(new RevealWindowEndedEvent(FighterSlot.Player, i));
                    _eventBus.Publish(new RevealWindowEndedEvent(FighterSlot.Opponent, i));
                }

                return; // windows never overlap (interval > duration)
            }
        }

        private void Observe(SimFighter fighter, int revealIndex, int completedTicks)
        {
            _intel[(int)fighter.Slot] = new SimIntel(fighter.Position, fighter.Velocity, completedTicks);
            _eventBus.Publish(new RevealTrailPointEvent(fighter.Slot, revealIndex, fighter.Position));
        }
    }
}
