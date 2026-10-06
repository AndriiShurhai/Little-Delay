using System;
using System.Text;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using LittleDelay.Simulation.Recording;
using LittleDelay.Simulation.Reveal;
using NUnit.Framework;
using UnityEngine;

namespace LittleDelay.Tests.EditMode
{
    [TestFixture]
    public class DeterminismTests
    {
        private static readonly SimMatchConfig Cfg = Rig.Config(c => c.RecordingDurationSeconds = 20f);

        /// <summary>A busy, seeded "player": strafes, re-aims at the other fighter with jitter, fires and dashes.</summary>
        private static ISimInputSource Busy(int seed, Func<Vector3> otherPosition, Func<Vector3> selfPosition)
        {
            var rng = new System.Random(seed);
            Vector3 move = Vector3.zero;
            return new ScriptedSource(tick =>
            {
                if (tick % 20 == 0) move = new Vector3((float)(rng.NextDouble() * 2 - 1), 0, (float)(rng.NextDouble() * 2 - 1));
                Vector3 toOther = otherPosition() - selfPosition();
                float jitter = (float)(rng.NextDouble() * 0.4 - 0.2);
                Vector3 aim = Quaternion_Y(toOther, jitter);
                return new SimInputFrame(move, aim, rng.NextDouble() < 0.5, rng.NextDouble() < 0.02);
            });
        }

        private static Vector3 Quaternion_Y(Vector3 v, float radians)
        {
            float c = Mathf.Cos(radians), s = Mathf.Sin(radians);
            return new Vector3(v.x * c - v.z * s, 0, v.x * s + v.z * c);
        }

        private sealed class Capture
        {
            public SimInputRecording Player, Opponent;
            public Vector3 PlayerEnd, OpponentEnd;
        }

        private static Capture Record(int seedP, int seedO)
        {
            var bus = Rig.Bus();
            var world = new SimWorld(Cfg, bus, SimWorldMode.Recording, 1, Vector3.zero, Rig.Spawn(-6, 0, 1, 0), Rig.Spawn(6, 0, -1, 0));
            var reveal = new SimRevealSystem(Cfg, bus, world.Player, world.Opponent);
            var pRec = new SimInputRecorder();
            var oRec = new SimInputRecorder();

            var runner = SimRoundRunner.ForRecording(world, Cfg,
                Busy(seedP, () => world.Opponent.Position, () => world.Player.Position),
                Busy(seedO, () => world.Player.Position, () => world.Opponent.Position),
                pRec, oRec, reveal);

            while (runner.Step()) { }

            return new Capture { Player = pRec.ToRecording(), Opponent = oRec.ToRecording(), PlayerEnd = world.Player.Position, OpponentEnd = world.Opponent.Position };
        }

        private static string ReplayBattle(Capture c, out SimRoundRunner runnerOut)
        {
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = new SimWorld(Cfg, bus, SimWorldMode.Battle, 1, Vector3.zero, Rig.Spawn(-6, 0, 1, 0), Rig.Spawn(6, 0, -1, 0));
            var runner = SimRoundRunner.ForBattle(world, Cfg, c.Player, c.Opponent);
            while (runner.Step()) { }
            runnerOut = runner;

            var sb = new StringBuilder();
            sb.AppendLine($"ticks={world.TickCount} outcome={runner.Outcome}");
            sb.AppendLine($"P pos={world.Player.Position} hp={world.Player.CurrentHealth:F4}");
            sb.AppendLine($"O pos={world.Opponent.Position} hp={world.Opponent.CurrentHealth:F4}");
            foreach (string line in log.Lines) sb.AppendLine(line);
            return sb.ToString();
        }

        [Test]
        public void ReplayingTheSameRecordingsTwiceProducesIdenticalResultsAndEvents()
        {
            Capture c = Record(11, 22);

            string first = ReplayBattle(c, out var r1);
            string second = ReplayBattle(c, out var r2);

            Assert.AreEqual(first, second);
            Assert.IsTrue(first.Contains("FighterDamagedEvent"), "the scenario must actually exercise combat, or this test proves nothing");
            Assert.AreEqual(r1.World.TickCount, r2.World.TickCount);
        }

        [Test]
        public void RecordingTheSameSeededInputsTwiceProducesIdenticalFrames()
        {
            Capture a = Record(5, 6);
            Capture b = Record(5, 6);

            Assert.AreEqual(a.Player.FrameCount, b.Player.FrameCount);
            for (int i = 0; i < a.Player.FrameCount; i++)
            {
                Assert.AreEqual(a.Player.GetFrame(i).MoveDirection, b.Player.GetFrame(i).MoveDirection);
                Assert.AreEqual(a.Opponent.GetFrame(i).FirePrimary, b.Opponent.GetFrame(i).FirePrimary);
            }
        }

        [Test]
        public void RecordedFrameNIsExactlyWhatPlaybackTickNConsumes()
        {
            Capture c = Record(3, 4);
            Assert.AreEqual(Cfg.RecordingTicks, c.Player.FrameCount);

            // Replay the recordings in a Recording-mode world (same rules as when they were captured):
            // if frames are aligned, both fighters must end exactly where the live run left them.
            var world = new SimWorld(Cfg, Rig.Bus(), SimWorldMode.Recording, 1, Vector3.zero, Rig.Spawn(-6, 0, 1, 0), Rig.Spawn(6, 0, -1, 0));
            var p = new SimRecordedInputSource(c.Player);
            var o = new SimRecordedInputSource(c.Opponent);
            for (int i = 0; i < Cfg.RecordingTicks; i++) world.Tick(p.GetInput(i), o.GetInput(i));

            Assert.AreEqual(c.PlayerEnd, world.Player.Position);
            Assert.AreEqual(c.OpponentEnd, world.Opponent.Position);
            Assert.AreEqual(c.PlayerEnd.x, world.Player.Position.x);
            Assert.AreEqual(c.OpponentEnd.z, world.Opponent.Position.z);
        }

        [Test]
        public void BattleOutlivesShortRecordingsUntilLeftoverProjectilesResolve()
        {
            var shortRecording = new SimInputRecorder();
            for (int i = 0; i < 10; i++) shortRecording.RecordFrame(i == 9 ? Rig.Fire(1, 0) : SimInputFrame.None);
            var idle = new SimInputRecorder();
            for (int i = 0; i < 10; i++) idle.RecordFrame(SimInputFrame.None);

            var world = Rig.FacingWorld(Cfg, Rig.Bus(), SimWorldMode.Battle);
            var runner = SimRoundRunner.ForBattle(world, Cfg, shortRecording.ToRecording(), idle.ToRecording());
            while (runner.Step()) { }

            Assert.Greater(world.TickCount, 10, "the last shot must be allowed to land or leave");
            Assert.AreEqual(0, world.Projectiles.Count);
            Assert.AreEqual(RoundOutcome.NoKill, runner.Outcome);
        }
    }

    [TestFixture]
    public class RevealAndAITests
    {
        [Test]
        public void ThreeTwoSecondWindowsOpenEverySevenSecondsStartingAtTickZero()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Recording);
            var reveal = new SimRevealSystem(cfg, bus, world.Player, world.Opponent);

            var openedAtTick = new System.Collections.Generic.List<int>();
            int trailPoints = 0;
            bus.Subscribe<RevealWindowStartedEvent>(e => { if (e.RevealedFighter == FighterSlot.Opponent) openedAtTick.Add(world.TickCount - 1); });
            bus.Subscribe<RevealTrailPointEvent>(e => { if (e.RevealedFighter == FighterSlot.Opponent) trailPoints++; });

            for (int i = 0; i < cfg.RecordingTicks; i++) { world.Tick(SimInputFrame.None, SimInputFrame.None); reveal.Tick(world.TickCount); }

            Assert.AreEqual(3, openedAtTick.Count);
            Assert.AreEqual(0, openedAtTick[0]);
            Assert.AreEqual(350, openedAtTick[1]);
            Assert.AreEqual(700, openedAtTick[2]);
            Assert.AreEqual(3 * 100, trailPoints);
        }

        [Test]
        public void IntelOnlyUpdatesInsideRevealWindows()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Recording);
            var reveal = new SimRevealSystem(cfg, bus, world.Player, world.Opponent);

            Assert.IsFalse(reveal.GetIntelAbout(FighterSlot.Player).IsKnown);

            // Walk the player for 2.5s: the first window (0-2s) is open, then it closes.
            for (int i = 0; i < 125; i++) { world.Tick(Rig.Move(1, 0), SimInputFrame.None); reveal.Tick(world.TickCount); }

            SimIntel intel = reveal.GetIntelAbout(FighterSlot.Player);
            Assert.IsTrue(intel.IsKnown);
            Assert.AreEqual(100, intel.ObservedTick, "last observation is the final tick of the window");
            Assert.Less(intel.Position.x, world.Player.Position.x - 1f, "the player kept moving after the window; intel is stale by design");
        }

        private sealed class FixedIntel : ISimIntelSource
        {
            public SimIntel Value;
            public SimIntel GetIntelAbout(FighterSlot subject) => Value;
        }

        [Test]
        public void AIDoesNotReadTheOpponentsLiveState()
        {
            var cfg = Rig.Config();
            var world = new SimWorld(cfg, Rig.Bus(), SimWorldMode.Recording, 1, Vector3.zero, Rig.Spawn(0, -3, 0, 1), Rig.Spawn(0, 8, 0, -1));
            var intel = new FixedIntel { Value = new SimIntel(new Vector3(0, 0, -3), Vector3.zero, 0) };
            var ai = new LittleDelay.Simulation.AI.SimAIInputSource(world.Opponent, FighterSlot.Player, intel, new LittleDelay.Simulation.AI.SimAIConfig(), cfg);

            SimInputFrame before = ai.GetInput(0);

            // The real player runs far away; the AI was told nothing, so its decision must not change.
            Rig.Run(world, 100, _ => Rig.Move(1, 0), Rig.Idle);
            SimInputFrame after = ai.GetInput(0);

            Assert.AreEqual(before.AimDirection, after.AimDirection);
            Assert.AreEqual(before.MoveDirection, after.MoveDirection);
            Assert.IsTrue(before.AimDirection.z < -0.9f, "aims at where the player was last seen");
        }

        [Test]
        public void AIWithNoIntelDoesNothing()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);
            var ai = new LittleDelay.Simulation.AI.SimAIInputSource(world.Opponent, FighterSlot.Player, new FixedIntel(), new LittleDelay.Simulation.AI.SimAIConfig(), cfg);

            Assert.AreEqual(SimInputFrame.None.FirePrimary, ai.GetInput(0).FirePrimary);
            Assert.AreEqual(Vector3.zero, ai.GetInput(0).MoveDirection);
        }

        [Test]
        public void AIIsADeterministicFunctionOfTickAndIntel()
        {
            var cfg = Rig.Config();
            var world = new SimWorld(cfg, Rig.Bus(), SimWorldMode.Recording, 1, Vector3.zero, Rig.Spawn(0, -3, 0, 1), Rig.Spawn(0, 9, 0, -1));
            var intel = new FixedIntel { Value = new SimIntel(new Vector3(0, 0, -3), new Vector3(1, 0, 0), 0) };
            var ai = new LittleDelay.Simulation.AI.SimAIInputSource(world.Opponent, FighterSlot.Player, intel, new LittleDelay.Simulation.AI.SimAIConfig(), cfg);

            for (int tick = 0; tick < 300; tick += 7)
            {
                SimInputFrame a = ai.GetInput(tick);
                SimInputFrame b = ai.GetInput(tick);
                Assert.AreEqual(a.MoveDirection, b.MoveDirection);
                Assert.AreEqual(a.FirePrimary, b.FirePrimary);
            }
        }
    }
}
