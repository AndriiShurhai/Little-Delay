using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Combat;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Simulation.Core
{
    /// <summary>
    /// The whole game state for one round phase (recording rehearsal OR battle). Advanced only by Tick():
    /// no Update(), no Time.*, no UnityEngine.Random, no scene objects.
    /// </summary>
    public sealed class SimWorld
    {
        private readonly SimMatchConfig _config;
        private readonly IEventBus _eventBus;
        private readonly SimCombat _combat;
        private readonly List<SimProjectile> _projectiles = new();
        private int _nextProjectileId = 1;

        public SimWorldMode Mode { get; }
        public int RoundNumber { get; }
        public Vector3 ArenaCenter { get; }
        public float ArenaRadius { get; private set; }
        public float ArenaStartRadius { get; }

        public SimFighter Player { get; }
        public SimFighter Opponent { get; }

        public IReadOnlyList<SimProjectile> Projectiles => _projectiles;
        public int TickCount { get; private set; }
        public float ElapsedSeconds => TickCount * _config.FixedDeltaTime;

        public RoundOutcome? Outcome { get; private set; }
        public bool IsEnded => Outcome.HasValue;
        public bool PassivityEnabled { get; set; } = true;

        public SimWorld(
            SimMatchConfig config,
            IEventBus eventBus,
            SimWorldMode mode,
            int roundNumber,
            Vector3 arenaCenter,
            SimSpawn playerSpawn,
            SimSpawn opponentSpawn,
            float playerStartHealth = -1f,
            float opponentStartHealth = -1f,
            float arenaStartRadius = -1f,
            SimLoadout? playerLoadout = null,
            SimLoadout? opponentLoadout = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _combat = new SimCombat(config, eventBus);

            Mode = mode;
            RoundNumber = roundNumber;
            ArenaCenter = arenaCenter;
            ArenaStartRadius = arenaStartRadius > 0f ? arenaStartRadius : SimMovement.ComputeRoundStartRadius(config, roundNumber);
            ArenaRadius = ArenaStartRadius;

            Player = CreateFighter(FighterSlot.Player, playerSpawn, playerStartHealth, playerLoadout ?? SimLoadout.Default);
            Opponent = CreateFighter(FighterSlot.Opponent, opponentSpawn, opponentStartHealth, opponentLoadout ?? SimLoadout.Default);
        }

        private SimFighter CreateFighter(FighterSlot slot, SimSpawn spawn, float startHealth, SimLoadout loadout)
        {
            Vector3 position = SimMovement.ClampToArena(spawn.Position, ArenaCenter, ArenaRadius);
            var fighter = new SimFighter(slot, position, spawn.AimDirection, _config.MaxHealth, startHealth, loadout);

            fighter.AddAbility(CreateAbility(loadout.WeaponId, slot));
            fighter.AddAbility(CreateAbility(loadout.Ability1Id, slot));
            fighter.AddAbility(CreateAbility(loadout.Ability2Id, slot));
            return fighter;
        }

        private SimAbility CreateAbility(string abilityId, FighterSlot slot)
        {
            if (string.IsNullOrEmpty(abilityId)) return null;

            return abilityId switch
            {
                SimBlastAbility.Id => new SimBlastAbility(slot, _eventBus, _config),
                SimDashAbility.Id => new SimDashAbility(slot, _eventBus, _config),
                _ => throw new ArgumentException($"Unknown ability id '{abilityId}'.", nameof(abilityId))
            };
        }

        public SimFighter GetFighter(FighterSlot slot) => slot == FighterSlot.Player ? Player : Opponent;

        internal void SpawnProjectile(FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            var projectile = new SimProjectile(_nextProjectileId++, owner, position, velocity);
            _projectiles.Add(projectile);
            _eventBus.Publish(new ProjectileSpawnedEvent(projectile.Id, owner, position, velocity));
        }

        public void Tick(SimInputFrame playerInput, SimInputFrame opponentInput)
        {
            if (IsEnded) return;

            float dt = _config.FixedDeltaTime;

            ArenaRadius = Mode == SimWorldMode.Battle
                ? SimMovement.ComputeShrunkRadius(_config, ArenaStartRadius, ElapsedSeconds)
                : ArenaStartRadius;

            Player.BeginTick();
            Opponent.BeginTick();

            TickFighter(Player, playerInput, dt, out bool playerActed);
            TickFighter(Opponent, opponentInput, dt, out bool opponentActed);

            _combat.AdvanceProjectiles(_projectiles, dt);

            if (Mode == SimWorldMode.Battle)
            {
                _combat.ResolveProjectileProjectileCollisions(_projectiles, dt);
                _combat.ResolveProjectileFighterCollisions(_projectiles, Player, Opponent);
            }

            _combat.ResolveArenaBoundsForProjectiles(_projectiles, ArenaCenter, ArenaRadius);

            if (Mode == SimWorldMode.Battle && PassivityEnabled)
            {
                TickPassivity(Player, playerActed, dt);
                TickPassivity(Opponent, opponentActed, dt);
            }

            _projectiles.RemoveAll(p => p.IsExpired);
            TickCount++;

            if (Mode == SimWorldMode.Battle)
            {
                EvaluateOutcome();
            }
        }

        private void TickFighter(SimFighter fighter, SimInputFrame input, float dt, out bool actionTaken)
        {
            Vector3 move = SimMovement.ClampMoveInput(input.MoveDirection);
            fighter.SetInput(move, input.AimDirection);

            for (int i = 0; i < fighter.Abilities.Count; i++)
            {
                fighter.Abilities[i].Tick(dt);
            }

            fighter.TickDeflection(dt);
            fighter.TickDash(dt, _config.DashFrictionPerSecond);

            actionTaken = false;

            if (fighter.TryGetAbility(fighter.Loadout.Ability1Id, out SimAbility ability1))
            {
                if (input.UseAbility1)
                {
                    actionTaken |= ability1.TryUse(fighter, this, reportFailure: !fighter.Ability1WasRequested);
                }
                fighter.Ability1WasRequested = input.UseAbility1;
            }

            if (fighter.TryGetAbility(fighter.Loadout.Ability2Id, out SimAbility ability2))
            {
                if (input.UseAbility2)
                {
                    actionTaken |= ability2.TryUse(fighter, this, reportFailure: !fighter.Ability2WasRequested);
                }
                fighter.Ability2WasRequested = input.UseAbility2;
            }

            if (fighter.TryGetAbility(fighter.Loadout.WeaponId, out SimAbility weapon))
            {
                if (input.FirePrimary)
                {
                    actionTaken |= weapon.TryUse(fighter, this, reportFailure: !fighter.FireWasRequested);
                }
                fighter.FireWasRequested = input.FirePrimary;
            }

            if (input.Jump && _config.JumpSpeed > 0f && _config.JumpGravity > 0f && fighter.IsGrounded)
            {
                fighter.StartJump(_config.JumpSpeed);
            }

            Vector3 chosenVelocity = SimMovement.ComputeVelocity(move, _config) + fighter.DashVelocity;
            Vector3 totalVelocity = chosenVelocity + fighter.DeflectionVelocity;

            Vector3 next = SimMovement.Integrate(fighter.Position, totalVelocity, dt);
            next = SimMovement.ClampToArena(next, ArenaCenter, ArenaRadius);

            SimMovement.IntegrateVertical(
                next.y, fighter.VerticalVelocity, fighter.GroundHeight, _config.JumpGravity, dt,
                out float height, out float verticalVelocity);
            next.y = height;

            fighter.SetMotion(next, totalVelocity);
            fighter.SetVerticalVelocity(verticalVelocity);
        }

        private void TickPassivity(SimFighter fighter, bool actedThisTick, float dt)
        {
            if (!fighter.IsAlive) return;

            if (!fighter.PassivityTracker.Update(fighter.Position, actedThisTick, dt, _config, out bool isPassive) || !isPassive)
            {
                return;
            }

            _eventBus.Publish(new PassivityDetectedEvent(fighter.Slot));
            _combat.ApplyDamage(fighter, _config.PassivityDamage, DamageSource.Passivity);
        }

        private void EvaluateOutcome()
        {
            bool playerDead = !Player.IsAlive;
            bool opponentDead = !Opponent.IsAlive;
            if (!playerDead && !opponentDead) return;

            Outcome = playerDead && opponentDead ? RoundOutcome.Draw
                    : playerDead ? RoundOutcome.OpponentWin
                    : RoundOutcome.PlayerWin;
        }

        /// <summary>
        /// Deterministic 32-bit FNV-1a state hash over tick count, arena radius, fighter states, and active projectiles.
        /// Used by golden-run tests and M3 lockstep verification.
        /// </summary>
        public uint ComputeStateHash()
        {
            uint hash = 2166136261u;
            HashInt(ref hash, TickCount);
            HashFloat(ref hash, ArenaRadius);
            HashFighter(ref hash, Player);
            HashFighter(ref hash, Opponent);
            HashInt(ref hash, _projectiles.Count);
            for (int i = 0; i < _projectiles.Count; i++)
            {
                SimProjectile p = _projectiles[i];
                HashInt(ref hash, (int)p.Owner);
                HashVector3(ref hash, p.Position);
                HashVector3(ref hash, p.Velocity);
                HashFloat(ref hash, p.ElapsedSeconds);
            }
            return hash;
        }

        private static void HashFighter(ref uint hash, SimFighter f)
        {
            HashVector3(ref hash, f.Position);
            HashVector3(ref hash, f.Velocity);
            HashVector3(ref hash, f.AimDirection);
            HashFloat(ref hash, f.VerticalVelocity);
            HashFloat(ref hash, f.CurrentHealth);
        }

        private static void HashVector3(ref uint hash, Vector3 v)
        {
            HashFloat(ref hash, v.x);
            HashFloat(ref hash, v.y);
            HashFloat(ref hash, v.z);
        }

        private static void HashFloat(ref uint hash, float value) =>
            HashInt(ref hash, BitConverter.SingleToInt32Bits(value));

        private static void HashInt(ref uint hash, int value)
        {
            unchecked
            {
                hash = (hash ^ (uint)(value & 0xFF)) * 16777619u;
                hash = (hash ^ (uint)((value >> 8) & 0xFF)) * 16777619u;
                hash = (hash ^ (uint)((value >> 16) & 0xFF)) * 16777619u;
                hash = (hash ^ (uint)((value >> 24) & 0xFF)) * 16777619u;
            }
        }
    }
}