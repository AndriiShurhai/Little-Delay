using System;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Recording;
using LittleDelay.Simulation.Reveal;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>
    /// Drives exactly one SimWorld one tick at a time: asks each fighter's input source for this tick's frame,
    /// (when recording) captures it, ticks the world, then decides whether the phase is over. Recording and
    /// playback go through the same loop, which is what keeps recorded frame N aligned with playback tick N.
    /// </summary>
    public sealed class SimRoundRunner
    {
        private readonly ISimInputSource _player;
        private readonly ISimInputSource _opponent;
        private readonly SimInputRecorder _playerRecorder;
        private readonly SimInputRecorder _opponentRecorder;
        private readonly IRevealSource _reveal;
        private readonly int _phaseTicks;
        private readonly int _settleTicks;
        private int _settleStartTick = -1;

        public SimWorld World { get; }
        public SimWorldMode Mode => World.Mode;
        public bool IsFinished { get; private set; }

        /// <summary>Battle only. NoKill when the recordings (and the projectiles still in flight) ran out with both fighters alive.</summary>
        public RoundOutcome Outcome { get; private set; } = RoundOutcome.NoKill;

        public float SecondsRemaining => Math.Max(0, _phaseTicks - World.TickCount) * _secondsPerTick;
        public float Progress01 => _phaseTicks <= 0 ? 1f : Math.Min(1f, (float)World.TickCount / _phaseTicks);

        private readonly float _secondsPerTick;

        private SimRoundRunner(
            SimWorld world, float secondsPerTick, ISimInputSource player, ISimInputSource opponent,
            SimInputRecorder playerRecorder, SimInputRecorder opponentRecorder, SimRevealSystem reveal,
            int phaseTicks, int settleTicks)
        {
            World = world;
            _secondsPerTick = secondsPerTick;
            _player = player;
            _opponent = opponent;
            _playerRecorder = playerRecorder;
            _opponentRecorder = opponentRecorder;
            _reveal = reveal;
            _phaseTicks = phaseTicks;
            _settleTicks = settleTicks;
        }

        public static SimRoundRunner ForRecording(
            SimWorld world, SimMatchConfig config, ISimInputSource player, ISimInputSource opponent,
            SimInputRecorder playerRecorder, SimInputRecorder opponentRecorder, SimRevealSystem reveal)
        {
            playerRecorder.Clear();
            opponentRecorder.Clear();
            return new SimRoundRunner(world, config.FixedDeltaTime, player, opponent, playerRecorder, opponentRecorder, reveal, config.RecordingTicks, 0);
        }

        public static SimRoundRunner ForBattle(
            SimWorld world, SimMatchConfig config, SimInputRecording player, SimInputRecording opponent)
        {
            int phaseTicks = Math.Max(player.FrameCount, opponent.FrameCount);
            int settleTicks = (int)Math.Ceiling(config.ProjectileLifeTimeSeconds / config.FixedDeltaTime);
            return new SimRoundRunner(
                world, config.FixedDeltaTime,
                new SimRecordedInputSource(player), new SimRecordedInputSource(opponent),
                null, null, null, phaseTicks, settleTicks);
        }

        /// <summary>Runs one tick. Returns true while there is more to run, false once finished (the shape SimTickClock.Advance wants).</summary>
        public bool Step()
        {
            if (IsFinished) return false;

            int tick = World.TickCount;

            // Once the recordings are spent, only let leftover projectiles resolve; nobody gets punished for standing still.
            if (Mode == SimWorldMode.Battle && tick >= _phaseTicks)
            {
                World.PassivityEnabled = false;
            }

            SimInputFrame playerFrame = _player.GetInput(tick);
            SimInputFrame opponentFrame = _opponent.GetInput(tick);

            if (_playerRecorder != null)
            {
                _playerRecorder.RecordFrame(playerFrame);
                _opponentRecorder.RecordFrame(opponentFrame);
            }

            World.Tick(playerFrame, opponentFrame);
            _reveal?.Tick(World.TickCount);

            if (Mode == SimWorldMode.Recording)
            {
                IsFinished = World.TickCount >= _phaseTicks;
            }
            else if (World.IsEnded)
            {
                Outcome = World.Outcome.Value;
                IsFinished = true;
            }
            else if (World.TickCount >= _phaseTicks)
            {
                if (_settleStartTick < 0) _settleStartTick = World.TickCount;

                if (World.Projectiles.Count == 0 || World.TickCount - _settleStartTick >= _settleTicks)
                {
                    Outcome = RoundOutcome.NoKill;
                    IsFinished = true;
                }
            }

            return !IsFinished;
        }
    }
}
