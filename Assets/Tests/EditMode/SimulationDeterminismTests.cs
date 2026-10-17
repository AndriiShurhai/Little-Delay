using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Drewio.UnityToolkit.EventBus;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using NUnit.Framework;
using UnityEngine;

namespace LittleDelay.Tests.EditMode
{
    public sealed class SimulationDeterminismTests
    {
        [Test]
        public void Recording_PlayerTrace_IgnoresOpponentInput()
        {
            Assert.AreEqual(Trace(opponentFires: false), Trace(opponentFires: true));
        }

        private static string Trace(bool opponentFires)
        {
            var cfg = new SimMatchConfig();
            var world = new SimWorld(
                cfg, new EventBus(), SimWorldMode.Recording, 1, Vector3.zero,
                new SimSpawn(new Vector3(-5f, 0f, 0f), Vector3.right),
                new SimSpawn(new Vector3(5f, 0f, 0f), Vector3.left));

            var sb = new StringBuilder();
            for (int t = 0; t < cfg.RecordingTicks; t++)
            {
                var player = new SimInputFrame(
                    t % 100 < 50 ? Vector3.forward : Vector3.back,
                    Vector3.right,
                    firePrimary: true);
                var opponent = new SimInputFrame(
                    Vector3.zero,
                    Vector3.left,
                    firePrimary: opponentFires);

                world.Tick(player, opponent);
                sb.Append(world.Player.Position.ToString("F5"));
                foreach (var p in world.Projectiles)
                {
                    if (p.Owner == FighterSlot.Player)
                    {
                        sb.Append(p.Position.ToString("F5")).Append(p.Velocity.ToString("F5"));
                    }
                }
            }
            return sb.ToString();
        }

        [Test]
        public void Passivity_AdTappingAndFiring_FlagsAllFiveIntervals()
        {
            var cfg = new SimMatchConfig();
            var bus = new EventBus();
            int passivityFlags = 0;
            bus.Subscribe<PassivityDetectedEvent>(e =>
            {
                if (e.Fighter == FighterSlot.Player) passivityFlags++;
            });

            var world = new SimWorld(
                cfg, bus, SimWorldMode.Battle, 1, Vector3.zero,
                new SimSpawn(new Vector3(-5f, 0f, 0f), Vector3.back),
                new SimSpawn(new Vector3(5f, 0f, 0f), Vector3.forward));

            for (int t = 0; t < cfg.RecordingTicks; t++)
            {
                // Alternate left/right every 5 ticks (0.1s) while firing away from opponent
                Vector3 move = (t / 5) % 2 == 0 ? Vector3.left : Vector3.right;
                var player = new SimInputFrame(move, Vector3.back, firePrimary: true);
                world.Tick(player, SimInputFrame.None);
            }

            Assert.AreEqual(5, passivityFlags);
        }

        [Test]
        public void Shooting3D_AimingUpward_HitsAirborneJumpingOpponent()
        {
            var cfg = new SimMatchConfig();
            var world = new SimWorld(
                cfg, new EventBus(), SimWorldMode.Battle, 1, Vector3.zero,
                new SimSpawn(new Vector3(0f, 0f, 0f), Vector3.forward),
                new SimSpawn(new Vector3(0f, 0f, 4f), Vector3.back));

            // Opponent jumps on tick 0; reaches near peak (~1.14m) around tick 14 (0.28s).
            // Player waits until tick 4 then fires upward toward (0, 1.1m, 4m) so projectile arrives at tick 14.
            Vector3 upwardAim = new Vector3(0f, 1.1f, 4f).normalized;

            for (int t = 0; t < 30; t++)
            {
                var opponent = new SimInputFrame(Vector3.zero, Vector3.back, firePrimary: false, useAbility1: false, jump: t == 0);
                var player = new SimInputFrame(Vector3.zero, upwardAim, firePrimary: t == 4);
                world.Tick(player, opponent);
            }

            Assert.Less(world.Opponent.CurrentHealth, cfg.MaxHealth, "3D upward shot should hit the jumping opponent.");
        }

        [Test]
        public void BattleReplay_IdenticalInputs_ProduceIdenticalStateHash()
        {
            uint RunSimulation()
            {
                var cfg = new SimMatchConfig();
                var world = new SimWorld(
                    cfg, new EventBus(), SimWorldMode.Battle, 1, Vector3.zero,
                    new SimSpawn(new Vector3(-6f, 0f, 0f), Vector3.right),
                    new SimSpawn(new Vector3(6f, 0f, 0f), Vector3.left));

                for (int t = 0; t < 300; t++)
                {
                    var p1 = new SimInputFrame(
                        new Vector3(0f, 0f, (t % 60 < 30) ? 1f : -1f),
                        new Vector3(1f, 0.15f, 0f),
                        firePrimary: t % 25 == 0,
                        useAbility1: t == 50,
                        jump: t % 40 == 0);

                    var p2 = new SimInputFrame(
                        new Vector3(0f, 0f, (t % 40 < 20) ? -1f : 1f),
                        new Vector3(-1f, -0.1f, 0f),
                        firePrimary: t % 30 == 0,
                        useAbility1: t == 70,
                        jump: t % 50 == 0);

                    world.Tick(p1, p2);
                }

                return world.ComputeStateHash();
            }

            Assert.AreEqual(RunSimulation(), RunSimulation());
        }

        [Test]
        public void Simulation_UsesNoForbiddenApi()
        {
            var bad = new Regex(@"\b(MonoBehaviour|GameObject|Transform|Rigidbody|Physics\d?D?|Debug\.|UnityEngine\.Random|Time\.(deltaTime|time|fixedDeltaTime|unscaledTime)|Mathf\.(Sin|Cos|Tan|Asin|Acos|Atan2?|Pow|Exp|Log\d*))");
            string root = Path.Combine(Application.dataPath, "Scripts", "Simulation");
            var hits = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "AI" + Path.DirectorySeparatorChar))
                .SelectMany(f => File.ReadLines(f).Select((l, i) => (f, i + 1, l)))
                .Where(t => !t.l.TrimStart().StartsWith("//") && !t.l.TrimStart().StartsWith("///") && bad.IsMatch(t.l))
                .Select(t => $"{Path.GetFileName(t.f)}:{t.Item2}: {t.l.Trim()}")
                .ToList();

            Assert.IsEmpty(hits, string.Join("\n", hits));
        }
    }
}