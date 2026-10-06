using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.StateMachines;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using LittleDelay.Simulation.Flow.States;
using NUnit.Framework;
using UnityEngine;

namespace LittleDelay.Tests.EditMode
{
    [TestFixture]
    public class SimMatchTests
    {
        private static SimMatch NewMatch(SimMatchConfig cfg, Drewio.UnityToolkit.Core.IEventBus bus, Func<FighterSlot, ISimInputSource> sources)
        {
            return new SimMatch(cfg, bus, new ScriptedProvider((slot, w, i) => sources(slot)), Vector3.zero,
                Rig.Spawn(0, -5, 0, 1), Rig.Spawn(0, 5, 0, -1));
        }

        private static void PlayRound(SimMatch match)
        {
            SimRoundRunner rec = match.BeginRound();
            while (rec.Step()) { }
            match.CompleteRecording();

            SimRoundRunner battle = match.BeginBattle();
            while (battle.Step()) { }
            match.CompleteBattle();
        }

        [Test]
        public void NoKillRoundsCarryHealthAndPositionThenTheMatchIsADrawAfterMaxRounds()
        {
            var cfg = Rig.Config(c => c.RecordingDurationSeconds = 10f);
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var match = NewMatch(cfg, bus, _ => ScriptedSource.Idle);
            match.StartMatch();

            PlayRound(match);
            Assert.IsFalse(match.IsOver);
            float healthAfterRound1 = match.CurrentWorld.Player.CurrentHealth;
            Assert.Less(healthAfterRound1, cfg.MaxHealth, "idle fighters take passivity damage");

            match.BeginRound();
            Assert.AreEqual(2, match.RoundNumber);
            Assert.AreEqual(healthAfterRound1, match.CurrentWorld.Player.CurrentHealth, 0.001f, "health carries into the next round");
            Assert.AreEqual(13f, match.CurrentWorld.ArenaRadius, 0.001f, "arena shrinks each round");

            // finish round 2 and 3
            var rec = match.CurrentRunner; while (rec.Step()) { } match.CompleteRecording();
            var battle = match.BeginBattle(); while (battle.Step()) { } match.CompleteBattle();
            PlayRound(match);

            Assert.IsTrue(match.IsOver);
            Assert.AreEqual(RoundOutcome.Draw, match.MatchOutcome.Value);
            Assert.AreEqual(3, match.RoundNumber);
            Assert.AreEqual(3, log.Count("RoundEndedEvent"));
        }

        [Test]
        public void AKillEndsTheMatchImmediately()
        {
            var cfg = Rig.Config(c => c.RecordingDurationSeconds = 10f);
            var bus = Rig.Bus();
            var match = NewMatch(cfg, bus, slot => slot == FighterSlot.Player
                ? new ScriptedSource(_ => Rig.Fire(0, 1))
                : ScriptedSource.Idle);
            match.StartMatch();

            PlayRound(match);

            Assert.IsTrue(match.IsOver);
            Assert.AreEqual(RoundOutcome.PlayerWin, match.MatchOutcome.Value);
            Assert.AreEqual(1, match.RoundNumber);
            Assert.Throws<InvalidOperationException>(() => match.BeginRound());
        }

        [Test]
        public void RecordingCannotBeCompletedEarly()
        {
            var cfg = Rig.Config();
            var match = NewMatch(cfg, Rig.Bus(), _ => ScriptedSource.Idle);
            match.StartMatch();
            match.BeginRound();

            Assert.Throws<InvalidOperationException>(() => match.CompleteRecording());
        }

        [Test]
        public void RematchResetsRoundCounterSpawnsAndHealth()
        {
            var cfg = Rig.Config(c => c.RecordingDurationSeconds = 10f);
            var match = NewMatch(cfg, Rig.Bus(), _ => ScriptedSource.Idle);
            match.StartMatch();
            PlayRound(match);

            match.StartMatch();
            match.BeginRound();

            Assert.AreEqual(1, match.RoundNumber);
            Assert.AreEqual(cfg.MaxHealth, match.CurrentWorld.Player.CurrentHealth, 0.001f);
            Assert.AreEqual(15f, match.CurrentWorld.ArenaRadius, 0.001f);
        }
    }

    [TestFixture]
    public class MatchFlowTests
    {
        private static MatchFlow Build(out EventLog log, out Drewio.UnityToolkit.Core.IEventBus bus, Func<FighterSlot, ISimInputSource> sources = null)
        {
            var cfg = Rig.Config(c =>
            {
                c.RecordingDurationSeconds = 1f; c.ShowdownPreviewSeconds = 0.1f; c.PreRecordingCountdownSeconds = 0.1f;
                c.PreBattleCountdownSeconds = 0.1f; c.RoundResolveSeconds = 0.1f;
            });
            bus = Rig.Bus();
            log = new EventLog(bus);
            sources = sources ?? (_ => ScriptedSource.Idle);
            var match = new SimMatch(cfg, bus, new ScriptedProvider((s, w, i) => sources(s)), Vector3.zero,
                Rig.Spawn(0, -5, 0, 1), Rig.Spawn(0, 5, 0, -1));
            return new MatchFlow(match, cfg, bus, new FakeCamera(), new SimTickClock(cfg.FixedDeltaTime));
        }

        private static void RunUntil(MatchFlow flow, Func<bool> done, int maxFrames = 20000)
        {
            for (int i = 0; i < maxFrames && !done(); i++) flow.Tick(1f / 60f);
            Assert.IsTrue(done(), "flow did not reach the expected state in time");
        }

        [Test]
        public void ThreeNoKillRoundsWalkEveryStateInOrderAndEndInMatchOver()
        {
            MatchFlow flow = Build(out EventLog log, out var bus);
            var visited = new List<string>();
            flow.Machine.StateChanged += (from, to) => visited.Add(to.GetType().Name);

            flow.Start();
            RunUntil(flow, () => flow.Machine.CurrentState == flow.MatchOver);

            string[] round = { "PlayerCameraTransitionState", "CountdownState", "RecordingState", "ArenaCameraTransitionState", "PreBattleCountdownState", "BattlePhaseState", "RoundResolveState" };
            var expected = new List<string> { "ShowdownPreviewState" };
            for (int r = 0; r < 3; r++) expected.AddRange(round);
            expected.Add("MatchOverState");

            Assert.AreEqual(string.Join(",", expected), string.Join(",", visited));
            Assert.AreEqual(3, log.Count("RoundStartedEvent"));
            Assert.AreEqual(1, log.Count("MatchEndedEvent"));
            Assert.AreEqual(1, log.Count("MatchStartedEvent"));
        }

        [Test]
        public void AKillSkipsStraightFromTheFirstResolveToMatchOver()
        {
            MatchFlow flow = Build(out EventLog log, out var bus,
                slot => slot == FighterSlot.Player ? new ScriptedSource(_ => Rig.Fire(0, 1)) : ScriptedSource.Idle);
            // Longer recording so the 13 hits fit.
            flow.Config.RecordingDurationSeconds = 10f;

            flow.Start();
            RunUntil(flow, () => flow.Machine.CurrentState == flow.MatchOver);

            Assert.AreEqual(1, log.Count("RoundStartedEvent"));
            Assert.IsTrue(log.Lines.Exists(l => l.StartsWith("MatchEndedEvent(Outcome=PlayerWin")));
        }

        [Test]
        public void RematchReturnsToTheShowdownAndStartsAFreshMatch()
        {
            MatchFlow flow = Build(out EventLog log, out var bus);
            flow.Start();
            RunUntil(flow, () => flow.Machine.CurrentState == flow.MatchOver);

            flow.RequestRematch();
            Assert.AreEqual(flow.ShowdownPreview, flow.Machine.CurrentState);

            RunUntil(flow, () => flow.Machine.CurrentState == flow.MatchOver);
            Assert.AreEqual(2, log.Count("MatchStartedEvent"));
        }

        [Test]
        public void TheTransitionTableRejectsIllegalJumps()
        {
            MatchFlow flow = Build(out EventLog log, out var bus);
            flow.Start();

            Assert.Throws<InvalidOperationException>(() => flow.Machine.ChangeState(flow.BattlePhase));
        }

        [Test]
        public void RematchRequestsAreIgnoredOutsideTheResultScreen()
        {
            MatchFlow flow = Build(out EventLog log, out var bus);
            flow.Start();
            flow.RequestRematch();

            Assert.AreEqual(flow.ShowdownPreview, flow.Machine.CurrentState);
            Assert.AreEqual(0, log.Count("MatchStartedEvent"));
        }

        [Test]
        public void CountdownPublishesEachWholeSecondOnce()
        {
            MatchFlow flow = Build(out EventLog log, out var bus);
            flow.Config.PreRecordingCountdownSeconds = 3f;
            var seconds = new List<int>();
            bus.Subscribe<CountdownTickEvent>(e => seconds.Add(e.SecondsRemaining));

            flow.Start();
            RunUntil(flow, () => flow.Machine.CurrentState == flow.Recording);

            Assert.AreEqual("3,2,1", string.Join(",", seconds));
        }

        private static int RecordedTicks(MatchFlow flow, float frameDelta)
        {
            flow.Start();
            SimRoundRunner recordingRunner = null;

            for (int i = 0; i < 20000 && flow.Machine.CurrentState != flow.ArenaCameraTransition; i++)
            {
                flow.Tick(frameDelta);
                if (flow.Machine.CurrentState == flow.Recording) recordingRunner = flow.Match.CurrentRunner;
            }

            Assert.IsNotNull(recordingRunner);
            return recordingRunner.World.TickCount;
        }

        [Test]
        public void RecordingIsDrivenByTheFixedClockNotTheFrameRate()
        {
            MatchFlow slow = Build(out _, out _);
            MatchFlow fast = Build(out _, out _);

            Assert.AreEqual(slow.Config.RecordingTicks, RecordedTicks(slow, 1f / 20f));
            Assert.AreEqual(fast.Config.RecordingTicks, RecordedTicks(fast, 1f / 240f));
        }
    }
}
