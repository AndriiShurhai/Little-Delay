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
    /// no Update(), no Time.*, no UnityEngine.Random, no scene objects. Given the same config, spawns,
    /// start health and the same sequence of input frames, it produces the same result every time -
    /// that is what makes recordings replayable and what the determinism tests assert.
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

        public SimFighter Player { get; }
        public SimFighter Opponent { get; }

        /// <summary>Live projectiles only - expired ones are removed at the end of the tick that expired them.</summary>
        public IReadOnlyList<SimProjectile> Projectiles => _projectiles;

        /// <summary>Ticks completed. During a Tick call this is the index of the tick being run, until it finishes.</summary>
        public int TickCount { get; private set; }

        public float ElapsedSeconds => TickCount * _config.FixedDeltaTime;

        /// <summary>Battle only: set the tick a fighter dies. Null while the round is still running.</summary>
        public RoundOutcome? Outcome { get; private set; }
        public bool IsEnded => Outcome.HasValue;

        /// <summary>Lets the runner stop punishing inactivity once both recordings have run out and only projectiles are left flying.</summary>
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
            float opponentStartHealth = -1f)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _combat = new SimCombat(config, eventBus);

            Mode = mode;
            RoundNumber = roundNumber;
            ArenaCenter = arenaCenter;
            ArenaRadius = SimMovement.ComputeRoundStartRadius(config, roundNumber);

            Player = CreateFighter(FighterSlot.Player, playerSpawn, playerStartHealth);
            Opponent = CreateFighter(FighterSlot.Opponent, opponentSpawn, opponentStartHealth);
        }

        private SimFighter CreateFighter(FighterSlot slot, SimSpawn spawn, float startHealth)
        {
            Vector3 position = SimMovement.ClampToArena(spawn.Position, ArenaCenter, ArenaRadius);
            var fighter = new SimFighter(slot, position, spawn.AimDirection, _config.MaxHealth, startHealth);
            fighter.AddAbility(new SimBlastAbility(slot, _eventBus, _config));
            fighter.AddAbility(new SimDashAbility(slot, _eventBus, _config));
            return fighter;
        }

        public SimFighter GetFighter(FighterSlot slot) => slot == FighterSlot.Player ? Player : Opponent;

        /// <summary>Called by SimBlastAbility. Publishes ProjectileSpawnedEvent so the view layer can pull a pooled visual.</summary>
        internal void SpawnProjectile(FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            var projectile = new SimProjectile(_nextProjectileId++, owner, position, velocity);
            _projectiles.Add(projectile);
            _eventBus.Publish(new ProjectileSpawnedEvent(projectile.Id, owner, position, velocity));
        }

        /// <summary>
        /// Advances one fixed step. Order is part of the contract and never varies:
        /// fighters (Player, then Opponent; each: abilities -> jump takeoff -> horizontal move -> vertical move) -> projectile movement -> projectile/projectile bounce ->
        /// projectile/fighter hits (Battle only) -> arena bounds -> passivity (Battle only) -> cleanup -> outcome.
        /// </summary>
        public void Tick(SimInputFrame playerInput, SimInputFrame opponentInput)
        {
            if (IsEnded) return;

            float dt = _config.FixedDeltaTime;

            ArenaRadius = Mode == SimWorldMode.Battle
                ? SimMovement.ComputeBattleRadius(_config, RoundNumber, ElapsedSeconds)
                : SimMovement.ComputeRoundStartRadius(_config, RoundNumber);

            Player.BeginTick();
            Opponent.BeginTick();

            TickFighter(Player, playerInput, dt, out float playerDistance, out bool playerActed);
            TickFighter(Opponent, opponentInput, dt, out float opponentDistance, out bool opponentActed);

            _combat.AdvanceProjectiles(_projectiles, dt);
            _combat.ResolveProjectileProjectileCollisions(_projectiles, dt);

            if (Mode == SimWorldMode.Battle)
            {
                _combat.ResolveProjectileFighterCollisions(_projectiles, Player, Opponent);
            }

            _combat.ResolveArenaBoundsForProjectiles(_projectiles, ArenaCenter, ArenaRadius);

            if (Mode == SimWorldMode.Battle && PassivityEnabled)
            {
                TickPassivity(Player, playerDistance, playerActed, dt);
                TickPassivity(Opponent, opponentDistance, opponentActed, dt);
            }

            _projectiles.RemoveAll(p => p.IsExpired);
            TickCount++;

            if (Mode == SimWorldMode.Battle)
            {
                EvaluateOutcome();
            }
        }

        private void TickFighter(SimFighter fighter, SimInputFrame input, float dt, out float chosenDistance, out bool actionTaken)
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

            if (fighter.TryGetAbility(SimDashAbility.Id, out SimAbility dash))
            {
                if (input.UseMobility)
                {
                    actionTaken |= dash.TryUse(fighter, this, reportFailure: !fighter.MobilityWasRequested);
                }
                fighter.MobilityWasRequested = input.UseMobility;
            }

            if (fighter.TryGetAbility(SimBlastAbility.Id, out SimAbility blast))
            {
                if (input.FirePrimary)
                {
                    actionTaken |= blast.TryUse(fighter, this, reportFailure: !fighter.FireWasRequested);
                }
                fighter.FireWasRequested = input.FirePrimary;
            }

            // Jump: takes off from the ground only (no double jump, no buffering). It deliberately does NOT set
            // actionTaken - otherwise hopping in place would satisfy the passivity check that abilities are meant to satisfy.
            // A non-positive JumpSpeed or JumpGravity disables jumping (the latter so a bad config can't leave anyone floating forever).
            if (input.Jump && _config.JumpSpeed > 0f && _config.JumpGravity > 0f && fighter.IsGrounded)
            {
                fighter.StartJump(_config.JumpSpeed);
            }

            // Dash is a chosen movement; knockback is not. Passivity only counts the former.
            Vector3 chosenVelocity = SimMovement.ComputeVelocity(move, _config) + fighter.DashVelocity;
            Vector3 totalVelocity = chosenVelocity + fighter.DeflectionVelocity;

            Vector3 next = SimMovement.Integrate(fighter.Position, totalVelocity, dt);
            next = SimMovement.ClampToArena(next, ArenaCenter, ArenaRadius);

            // Vertical motion is independent of the horizontal math above, which never touches y.
            SimMovement.IntegrateVertical(
                next.y, fighter.VerticalVelocity, fighter.GroundHeight, _config.JumpGravity, dt,
                out float height, out float verticalVelocity);
            next.y = height;

            fighter.SetMotion(next, totalVelocity);
            fighter.SetVerticalVelocity(verticalVelocity);
            chosenDistance = chosenVelocity.magnitude * dt;
        }

        private void TickPassivity(SimFighter fighter, float distanceThisTick, bool actedThisTick, float dt)
        {
            if (!fighter.IsAlive) return;

            if (!fighter.PassivityTracker.Update(distanceThisTick, actedThisTick, dt, _config, out bool isPassive) || !isPassive)
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

            // Both dying on the same tick is a Draw, never an arbitrary winner.
            Outcome = playerDead && opponentDead ? RoundOutcome.Draw
                    : playerDead ? RoundOutcome.OpponentWin
                    : RoundOutcome.PlayerWin;
        }
    }
}