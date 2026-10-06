using LittleDelay.Simulation.Combat;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using NUnit.Framework;
using UnityEngine;

namespace LittleDelay.Tests.EditMode
{
    [TestFixture]
    public class SimTickClockTests
    {
        [Test]
        public void RunsWholeTicksAndExposesInterpolationAlpha()
        {
            var clock = new SimTickClock(0.02f);
            int steps = 0;

            int ran = clock.Advance(0.05f, () => { steps++; return true; });

            Assert.AreEqual(2, ran);
            Assert.AreEqual(2, steps);
            Assert.AreEqual(0.5f, clock.Alpha, 0.01f);
        }

        [Test]
        public void DropsBacklogInsteadOfSpiralling()
        {
            var clock = new SimTickClock(0.02f, 5);
            int ran = clock.Advance(10f, () => true);

            Assert.AreEqual(5, ran);
            Assert.AreEqual(0, clock.Advance(0f, () => true));
        }

        [Test]
        public void StopsAndFreezesWhenStepReportsFinished()
        {
            var clock = new SimTickClock(0.02f);
            int ran = clock.Advance(1f, () => false);

            Assert.AreEqual(1, ran);
            Assert.AreEqual(1f, clock.Alpha, 0.0001f);
        }
    }

    [TestFixture]
    public class SimMovementTests
    {
        [Test]
        public void MovesAtConfiguredSpeedPerSecond()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);
            Vector3 start = world.Player.Position;

            Rig.Run(world, cfg.TicksPerSecond, _ => Rig.Move(1, 0), Rig.Idle);

            Assert.AreEqual(cfg.FighterMoveSpeed, world.Player.Position.x - start.x, 0.01f);
        }

        [Test]
        public void DiagonalInputIsNotFasterThanStraight()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);
            Vector3 start = world.Player.Position;

            Rig.Run(world, cfg.TicksPerSecond, _ => Rig.Move(1, 1), Rig.Idle);

            Assert.AreEqual(cfg.FighterMoveSpeed, (world.Player.Position - start).magnitude, 0.01f);
        }

        [Test]
        public void CannotLeaveTheArena()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);

            Rig.Run(world, cfg.TicksPerSecond * 10, _ => Rig.Move(1, 0), Rig.Idle);

            Assert.AreEqual(cfg.ArenaInitialRadius, world.Player.Position.magnitude, 0.01f);
        }

        [Test]
        public void ArenaRadiusShrinksPerRoundAndNeverBelowMinimum()
        {
            var cfg = Rig.Config();
            Assert.AreEqual(15f, SimMovement.ComputeRoundStartRadius(cfg, 1), 0.0001f);
            Assert.AreEqual(13f, SimMovement.ComputeRoundStartRadius(cfg, 2), 0.0001f);
            Assert.AreEqual(cfg.ArenaMinRadius, SimMovement.ComputeRoundStartRadius(cfg, 99), 0.0001f);
        }
    }

    [TestFixture]
    public class SimAbilityTests
    {
        [Test]
        public void HeldFireRespectsCooldownAndDoesNotSpamFailureEvents()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Recording);

            Rig.Run(world, 60, _ => Rig.Fire(0, 1), Rig.Idle);

            Assert.AreEqual(3, log.Count("AbilityUsedEvent"));
            Assert.AreEqual(0, log.Count("AbilityFailedEvent"));
        }

        [Test]
        public void FreshPressWhileOnCooldownReportsExactlyOneFailure()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Recording);

            // fire, release, press again while the 0.5s cooldown is still running
            Rig.Run(world, 6, t => t == 0 || t >= 3 ? Rig.Fire(0, 1) : SimInputFrame.None, Rig.Idle);

            Assert.AreEqual(1, log.Count("AbilityUsedEvent"));
            Assert.AreEqual(1, log.Count("AbilityFailedEvent"));
        }

        [Test]
        public void CooldownProgressIsPullableForUi()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);
            world.Player.TryGetAbility("Blast", out var blast);

            Assert.IsTrue(blast.IsReady);
            Assert.AreEqual(0f, blast.NormalizedRemaining, 0.0001f);

            Rig.Run(world, 1, _ => Rig.Fire(0, 1), Rig.Idle);
            Assert.IsFalse(blast.IsReady);
            Assert.Greater(blast.NormalizedRemaining, 0.9f);

            Rig.Run(world, 30, Rig.Idle, Rig.Idle);
            Assert.IsTrue(blast.IsReady);
        }

        [Test]
        public void DashMovesFurtherThanWalkingAndHasItsOwnCooldown()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Recording);
            Vector3 start = world.Player.Position;

            Rig.Run(world, 25, t => new SimInputFrame(new Vector3(1, 0, 0), Vector3.zero, false, t == 0), Rig.Idle);
            float dashed = world.Player.Position.x - start.x;

            var control = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);
            Rig.Run(control, 25, _ => Rig.Move(1, 0), Rig.Idle);

            Assert.Greater(dashed, control.Player.Position.x - start.x + 1f);
            Assert.AreEqual(1, log.Count("AbilityUsedEvent"));
        }
    }

    [TestFixture]
    public class SimCombatTests
    {
        [Test]
        public void BattleProjectileDamagesAndKnocksBackTheOpponent()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Battle);

            Rig.Run(world, 40, t => t == 0 ? Rig.Fire(0, 1) : SimInputFrame.None, Rig.Idle);

            Assert.AreEqual(cfg.MaxHealth - cfg.ProjectileDamage, world.Opponent.CurrentHealth, 0.001f);
            Assert.AreEqual(cfg.MaxHealth, world.Player.CurrentHealth, 0.001f);
            Assert.Greater(world.Opponent.Position.z, 5f, "knocked back along the shot");
            Assert.AreEqual(1, log.Count("FighterDamagedEvent"));
            Assert.AreEqual(0, world.Projectiles.Count);
        }

        [Test]
        public void RecordingWorldNeverDamagesAnyone()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);

            Rig.Run(world, 60, t => t == 0 ? Rig.Fire(0, 1) : SimInputFrame.None, Rig.Idle);

            Assert.AreEqual(cfg.MaxHealth, world.Opponent.CurrentHealth, 0.001f);
        }

        [Test]
        public void FastProjectilesCannotTunnelThroughAFighter()
        {
            var cfg = Rig.Config(c => c.ProjectileSpeed = 400f); // 8 units per tick
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Battle);

            Rig.Run(world, 10, t => t == 0 ? Rig.Fire(0, 1) : SimInputFrame.None, Rig.Idle);

            Assert.AreEqual(cfg.MaxHealth - cfg.ProjectileDamage, world.Opponent.CurrentHealth, 0.001f);
        }

        [Test]
        public void HeadOnProjectilesBounceOnceAndNeverHurtTheirOwner()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Battle);

            Rig.Run(world, 120, t => t == 0 ? Rig.Fire(0, 1) : SimInputFrame.None,
                                 t => t == 0 ? Rig.Fire(0, -1) : SimInputFrame.None);

            Assert.AreEqual(2, log.Count("ProjectileDeflectedEvent"));
            Assert.AreEqual(0, log.Count("FighterDamagedEvent"));
            Assert.AreEqual(cfg.MaxHealth, world.Player.CurrentHealth, 0.001f);
            Assert.AreEqual(cfg.MaxHealth, world.Opponent.CurrentHealth, 0.001f);
        }

        [Test]
        public void ProjectilesLeavingTheArenaAreRemoved()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Recording);

            Rig.Run(world, 10, t => t == 0 ? Rig.Fire(1, 0) : SimInputFrame.None, Rig.Idle);
            Assert.AreEqual(1, world.Projectiles.Count);

            Rig.Run(world, 100, Rig.Idle, Rig.Idle);
            Assert.AreEqual(0, world.Projectiles.Count, "expired/out-of-bounds projectiles must not accumulate");
        }

        [Test]
        public void DeflectionIsAPureReflectionWithNoRandomComponent()
        {
            Vector3 a = SimCombat.ComputeDeflection(new Vector3(0, 0, 20), new Vector3(0, 0, -1));
            Vector3 b = SimCombat.ComputeDeflection(new Vector3(0, 0, 20), new Vector3(0, 0, -1));

            Assert.AreEqual(a, b);
            Assert.AreEqual(-20f, a.z, 0.0001f);
        }
    }

    [TestFixture]
    public class SimPassivityTests
    {
        [Test]
        public void StandingStillIsPunishedOnTheCheckInterval()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var log = new EventLog(bus);
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Battle);

            Rig.Run(world, 215, Rig.Idle, Rig.Idle);

            Assert.AreEqual(2, log.Count("PassivityDetectedEvent"));
            Assert.AreEqual(cfg.MaxHealth - cfg.PassivityDamage, world.Player.CurrentHealth, 0.001f);
        }

        [Test]
        public void MovingWithoutActingIsStillPassive()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Battle);

            Rig.Run(world, 215, t => Rig.Move(t / 50 % 2 == 0 ? 1 : -1, 0), Rig.Idle);

            Assert.Less(world.Player.CurrentHealth, cfg.MaxHealth);
        }

        [Test]
        public void ActingWithoutMovingIsStillPassive()
        {
            var cfg = Rig.Config();
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Battle);

            Rig.Run(world, 215, _ => Rig.Fire(1, 0), Rig.Idle);

            Assert.Less(world.Player.CurrentHealth, cfg.MaxHealth);
        }

        [Test]
        public void MovingAndFiringIsNeverPunished()
        {
            var cfg = Rig.Config();
            var bus = Rig.Bus();
            var world = Rig.FacingWorld(cfg, bus, SimWorldMode.Battle);

            Rig.Run(world, 450, t => new SimInputFrame(new Vector3(t / 50 % 2 == 0 ? 1 : -1, 0, 0), new Vector3(1, 0, 0), t % 30 == 0), Rig.Idle);

            Assert.AreEqual(cfg.MaxHealth, world.Player.CurrentHealth, 0.001f);
        }

        [Test]
        public void BothDyingOnTheSameTickIsADrawNotAnArbitraryWinner()
        {
            var cfg = Rig.Config(c => c.PassivityDamage = c.MaxHealth);
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Battle);

            Rig.Run(world, 260, Rig.Idle, Rig.Idle);

            Assert.AreEqual(RoundOutcome.Draw, world.Outcome.Value);
        }

        [Test]
        public void OnlyTheIdleFighterDiesWhenTheOtherStaysActive()
        {
            var cfg = Rig.Config(c => c.PassivityDamage = c.MaxHealth);
            var world = Rig.FacingWorld(cfg, Rig.Bus(), SimWorldMode.Battle);

            Rig.Run(world, 260, t => new SimInputFrame(new Vector3(t / 50 % 2 == 0 ? 1 : -1, 0, 0), new Vector3(1, 0, 0), t % 30 == 0), Rig.Idle);

            Assert.AreEqual(RoundOutcome.PlayerWin, world.Outcome.Value);
        }
    }
}
