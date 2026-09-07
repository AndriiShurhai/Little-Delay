using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Fighters;
using LittleDelay.Simulation.Combat;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Abilities;
using LittleDelay.SImulation.Abilities;

namespace LittleDelay.Simulation
{
    ///<summary>
    /// The engine-agnostic core of one orund: owns both SimFighters, every
    /// active SimProjectile, and the arena's current radius, and advances all
    /// of it by exactly one fixed step per Tick() call. No MonoBehaviour, no
    /// Unity Update loop reference anywhere in this class - whatever drives it
    /// ( a MonoBehaviour's FixedUpdate in Presentation, or a headless test) just
    /// has to call Tick() at a constant cadence matching the fixedDeltaTime
    /// passed to the constructor, since determinism depends on that cadence
    /// never varying.
    /// 
    /// Round outcome (win/loss/draw) is decided here rather than in the later
    /// match-flow state machine, because only SimWorld can see both
    /// fighters' health changes withing the same tick and correctly resolve a
    /// simultaneous death as a Draw - the state machine only ever finds out
    /// after the fact, via RoundEndedEvent.
    ///</summary>

    public sealed class SimWorld
    {
        private readonly SimMatchConfig _config;
        private readonly IEventBus _eventBus;
        private readonly SimCombat _combat;
        private readonly float _fixedDeltaTime;
        private readonly List<SimProjectile> _projectiles = new();

        private int _nextProjectileId;
        private int _tickIndex;
        private bool _roundEnded;

        public SimFighter Player { get; }
        public SimFighter Opponent { get; }
        public int RoundNumber { get; }
        public Vector3 ArenaCenter { get; }
        public float ArenaRadius { get; private set; }
        public IReadOnlyList<SimProjectile> Projectiles => _projectiles;

        public SimWorld(
            SimMatchConfig config,
            IEventBus eventBus,
            float fixedDeltaTime,
            int roundNumber,
            Vector3 arenaCenter,
            Vector3 playerSpawnPosition,
            Vector3 opponentSpawnPosition)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            if (fixedDeltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedDeltaTime), "fixedDeltaTime must be positive");
            }

            _fixedDeltaTime = fixedDeltaTime;
            RoundNumber = roundNumber;
            ArenaCenter = arenaCenter;
            ArenaRadius = config.ArenaInitialRadius;

            _combat = new SimCombat(config, eventBus);

            Player = new SimFighter(FighterSlot.Player, playerSpawnPosition, config.MaxHealth);
            Opponent = new SimFighter(FighterSlot.Opponent, opponentSpawnPosition, config.MaxHealth);

            Player.AddAbility(new SimBlastAbility(FighterSlot.Player, eventBus, config));
            Opponent.AddAbility(new SimBlastAbility(FighterSlot.Opponent, eventBus, config));
        }

        /// <summary>Simulation-side spawning - the only way a projectile ever enters the world. Abilities call this rather than constructing SimProjectile directly, so every spawn path publishes ProjectileSpawnedEvent consistently</summary>
        public int SpawnProjectile(FighterSlot owner, Vector3 position, Vector3 velocity)
        {
            int id = _nextProjectileId++;
            _projectiles.Add(new SimProjectile(id, owner, position, velocity));
            _eventBus.Publish(new ProjectileSpawnedEvent(id, owner, position, velocity));
            return id;
        }

        ///<summary>
        /// Advances the round by exactly one fixed step: moves both fighters
        /// resolves all projectile combat, check passivity, then evaluates
        /// whether the round just ended. A no-op once the round has already
        /// ended - safe to keep calling from a driver that hasn't noticed yet.
        ///</summary>
        public void Tick(SimInputFrame playerInput, SimInputFrame opponentInput)
        {
            if (_roundEnded) return;

            _tickIndex++;
            float roundElapsedSeconds = _tickIndex * _fixedDeltaTime;
            ArenaRadius = SimMovement.ComputeArenaRadius(_config, roundElapsedSeconds);

            
        }

        private void TickFighter(SimFighter fighter, SimInputFrame input)
        {
            foreach (SimAbility ability in fighter.Abilities)
            {
                ability.Tick(_fixedDeltaTime);
            }

            fighter.TickDeflection(_fixedDeltaTime);

            Vector3 inputVelocity = SimMovement.ComputeVelocity(input, _config);
            Vector3 totalVelocity = inputVelocity + fighter.DeflectionVelocity;

            Vector3 nextPosition = SimMovement.Integrate(fighter.Position, totalVelocity, _fixedDeltaTime);
            nextPosition = SimMovement.ClampToArena(nextPosition, ArenaCenter, ArenaRadius);

            fighter.SetKinematics(nextPosition, totalVelocity, input.AimDirection);

            if (input.FirePrimary && fighter.TryGetAbility(SimBlastAbility.Id, out SimAbility blastAbility))
            {
                blastAbility.TryUse(fighter, this);
            }
        }

        private void TickPassivity(SimFighter fighter, SimInputFrame input)
        {
            if (!fighter.IsAlive) return;

            float distanceThisTick = fighter.Velocity.magnitude * _fixedDeltaTime;
            bool checkCompleted = fighter.PassivityTracker.Update(
                distanceThisTick, input.FirePrimary, _fixedDeltaTime, _config, out bool isPassive);
            
            if (!checkCompleted || !isPassive) return;

            _eventBus.Publish(new PassivityDetectedEvent(fighter.Slot));
            _combat.ApplyDamage(fighter, _config.PassivityDamage, DamageSource.Passivity);
        }

        private void EvaluateRoundOutcome()
        {
            bool playerDead = !Player.IsAlive;
            bool opponentDead = !Opponent.IsAlive;
            if (!playerDead && !opponentDead) return;

            _roundEnded = true;

            RoundOutcome outcome = playerDead && opponentDead
                ? RoundOutcome.Draw
                : playerDead ? RoundOutcome.OpponentWin : RoundOutcome.PlayerWin;

            _eventBus.Publish(new RoundEndedEvent(RoundNumber, outcome));
        }
    }
}