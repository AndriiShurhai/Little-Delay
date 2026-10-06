using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Flow;
using LittleDelay.Simulation.Reveal;
using UnityEngine;

namespace LittleDelay.Tests.EditMode
{
    internal sealed class ScriptedSource : ISimInputSource
    {
        private readonly Func<int, SimInputFrame> _script;
        public ScriptedSource(Func<int, SimInputFrame> script) => _script = script;
        public SimInputFrame GetInput(int tickIndex) => _script(tickIndex);
        public static ScriptedSource Idle => new ScriptedSource(_ => SimInputFrame.None);
    }

    internal sealed class ScriptedProvider : ISimRecordingInputProvider
    {
        private readonly Func<FighterSlot, SimWorld, ISimIntelSource, ISimInputSource> _factory;
        public ScriptedProvider(Func<FighterSlot, SimWorld, ISimIntelSource, ISimInputSource> factory) => _factory = factory;
        public ISimInputSource CreateSource(FighterSlot slot, SimWorld world, ISimIntelSource intel) => _factory(slot, world, intel);
    }

    /// <summary>Camera stand-in that reports "blending" for a fixed number of polls after each ShowView.</summary>
    internal sealed class FakeCamera : ICameraDirector
    {
        private int _pollsLeft;
        public int BlendPolls = 3;
        public bool IsBlending => _pollsLeft-- > 0;
        public void ShowView(CameraView view) => _pollsLeft = BlendPolls;
    }

    /// <summary>Records every simulation/flow event as a string so two runs can be compared line by line.</summary>
    internal sealed class EventLog
    {
        public readonly List<string> Lines = new();

        public EventLog(IEventBus bus)
        {
            Sub<AbilityUsedEvent>(bus); Sub<AbilityFailedEvent>(bus); Sub<AbilityCooldownStartedEvent>(bus); Sub<AbilityReadyEvent>(bus);
            Sub<ProjectileSpawnedEvent>(bus); Sub<ProjectileDeflectedEvent>(bus); Sub<ProjectileDespawnedEvent>(bus);
            Sub<HealthChangedEvent>(bus); Sub<FighterDamagedEvent>(bus); Sub<PassivityDetectedEvent>(bus); Sub<FighterDiedEvent>(bus);
            Sub<RevealWindowStartedEvent>(bus); Sub<RevealWindowEndedEvent>(bus); Sub<RevealTrailPointEvent>(bus);
            Sub<RoundStartedEvent>(bus); Sub<RoundEndedEvent>(bus); Sub<MatchStartedEvent>(bus); Sub<MatchEndedEvent>(bus);
            Sub<RecordingStartedEvent>(bus); Sub<RecordingCompletedEvent>(bus); Sub<BattleStartedEvent>(bus);
        }

        public int Count(string eventName) { int n = 0; foreach (string l in Lines) if (l.StartsWith(eventName + "(")) n++; return n; }

        private void Sub<T>(IEventBus bus) where T : IGameEvent => bus.Subscribe<T>(e => Lines.Add(Format(e)));

        private static string Format<T>(T e)
        {
            var sb = new StringBuilder(typeof(T).Name).Append('(');
            foreach (FieldInfo f in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object v = f.GetValue(e);
                sb.Append(f.Name).Append('=').Append(v is float fl ? fl.ToString("F4") : v?.ToString()).Append(';');
            }
            return sb.Append(')').ToString();
        }
    }

    internal static class Rig
    {
        public static SimMatchConfig Config(Action<SimMatchConfig> tweak = null)
        {
            var c = new SimMatchConfig();
            tweak?.Invoke(c);
            return c;
        }

        public static IEventBus Bus() => new Drewio.UnityToolkit.EventBus.EventBus();

        public static SimSpawn Spawn(float x, float z, float aimX, float aimZ) => new SimSpawn(new Vector3(x, 0f, z), new Vector3(aimX, 0f, aimZ));

        /// <summary>Player at z=-5 facing +z, opponent at z=+5 facing -z, arena centred on the origin.</summary>
        public static SimWorld FacingWorld(SimMatchConfig cfg, IEventBus bus, SimWorldMode mode, int round = 1)
        {
            return new SimWorld(cfg, bus, mode, round, Vector3.zero, Spawn(0, -5, 0, 1), Spawn(0, 5, 0, -1));
        }

        public static SimInputFrame Move(float x, float z) => new SimInputFrame(new Vector3(x, 0, z), Vector3.zero, false);
        public static SimInputFrame Fire(float aimX, float aimZ) => new SimInputFrame(Vector3.zero, new Vector3(aimX, 0, aimZ), true);

        public static void Run(SimWorld world, int ticks, Func<int, SimInputFrame> player, Func<int, SimInputFrame> opponent)
        {
            for (int i = 0; i < ticks && !world.IsEnded; i++)
            {
                world.Tick(player(world.TickCount), opponent(world.TickCount));
            }
        }

        public static Func<int, SimInputFrame> Idle => _ => SimInputFrame.None;
    }
}
