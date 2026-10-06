using System;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using LittleDelay.Simulation.Recording;
using LittleDelay.Simulation.Reveal;
using UnityEngine;

namespace LittleDelay.Simulation.Flow
{
    /// <summary>
    /// Pure match model: round counter, the recordings captured this round, the spawn/health carried between
    /// rounds, and the win rule. It owns no timers and no Unity objects; MatchFlow's states decide WHEN each
    /// step happens, SimMatch decides WHAT it does. Rule (from the old RoundManager): a kill wins the match
    /// outright, simultaneous death is a Draw, and a round that ends with nobody dead rolls into the next one
    /// (from where the fighters ended up, with the health they had) until MaxRounds, then the match is a Draw.
    /// </summary>
    public sealed class SimMatch
    {
        private readonly SimMatchConfig _config;
        private readonly IEventBus _eventBus;
        private readonly ISimRecordingInputProvider _inputProvider;
        private readonly Vector3 _arenaCenter;
        private readonly SimSpawn _initialPlayerSpawn;
        private readonly SimSpawn _initialOpponentSpawn;
        private readonly SimInputRecorder _playerRecorder = new();
        private readonly SimInputRecorder _opponentRecorder = new();

        private SimSpawn _playerSpawn;
        private SimSpawn _opponentSpawn;
        private float _playerHealth = -1f;
        private float _opponentHealth = -1f;
        private SimInputRecording _playerRecording = SimInputRecording.Empty;
        private SimInputRecording _opponentRecording = SimInputRecording.Empty;

        public int RoundNumber { get; private set; }
        public bool IsOver { get; private set; }
        public RoundOutcome? MatchOutcome { get; private set; }

        /// <summary>The runner for the phase in progress (recording or battle). Stays set after a phase finishes so views can show its final state.</summary>
        public SimRoundRunner CurrentRunner { get; private set; }
        public SimWorld CurrentWorld => CurrentRunner?.World;

        public SimMatch(
            SimMatchConfig config, IEventBus eventBus, ISimRecordingInputProvider inputProvider,
            Vector3 arenaCenter, SimSpawn playerSpawn, SimSpawn opponentSpawn)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _inputProvider = inputProvider ?? throw new ArgumentNullException(nameof(inputProvider));
            _arenaCenter = arenaCenter;
            _initialPlayerSpawn = playerSpawn;
            _initialOpponentSpawn = opponentSpawn;
        }

        /// <summary>Resets everything for a fresh match (also used for rematches) and publishes MatchStartedEvent.</summary>
        public void StartMatch()
        {
            RetireCurrentWorld();
            CurrentRunner = null;
            RoundNumber = 0;
            IsOver = false;
            MatchOutcome = null;
            _playerSpawn = _initialPlayerSpawn;
            _opponentSpawn = _initialOpponentSpawn;
            _playerHealth = -1f;
            _opponentHealth = -1f;
            _playerRecording = SimInputRecording.Empty;
            _opponentRecording = SimInputRecording.Empty;

            _eventBus.Publish(new MatchStartedEvent(_config.MaxRounds));
        }

        /// <summary>Creates the next round's recording world (idle until its runner is stepped) and publishes it to the views.</summary>
        public SimRoundRunner BeginRound()
        {
            if (IsOver) throw new InvalidOperationException("The match is over; call StartMatch() first.");

            RetireCurrentWorld();
            RoundNumber++;

            var world = new SimWorld(_config, _eventBus, SimWorldMode.Recording, RoundNumber, _arenaCenter,
                _playerSpawn, _opponentSpawn, _playerHealth, _opponentHealth);

            var reveal = new SimRevealSystem(_config, _eventBus, world.Player, world.Opponent);

            CurrentRunner = SimRoundRunner.ForRecording(
                world, _config,
                _inputProvider.CreateSource(FighterSlot.Player, world, reveal),
                _inputProvider.CreateSource(FighterSlot.Opponent, world, reveal),
                _playerRecorder, _opponentRecorder, reveal);

            _eventBus.Publish(new SimWorldCreatedEvent(world, SimWorldMode.Recording));
            _eventBus.Publish(new RoundStartedEvent(RoundNumber, _config.MaxRounds));
            return CurrentRunner;
        }

        public void CompleteRecording()
        {
            if (CurrentRunner == null || CurrentRunner.Mode != SimWorldMode.Recording || !CurrentRunner.IsFinished)
            {
                throw new InvalidOperationException("The recording phase has not finished.");
            }

            _playerRecording = _playerRecorder.ToRecording();
            _opponentRecording = _opponentRecorder.ToRecording();
            _eventBus.Publish(new RecordingCompletedEvent());
        }

        /// <summary>Replaces the rehearsal world with a fresh battle world from the same spawns and health, ready to replay both recordings.</summary>
        public SimRoundRunner BeginBattle()
        {
            RetireCurrentWorld();

            var world = new SimWorld(_config, _eventBus, SimWorldMode.Battle, RoundNumber, _arenaCenter,
                _playerSpawn, _opponentSpawn, _playerHealth, _opponentHealth);

            CurrentRunner = SimRoundRunner.ForBattle(world, _config, _playerRecording, _opponentRecording);
            _eventBus.Publish(new SimWorldCreatedEvent(world, SimWorldMode.Battle));
            return CurrentRunner;
        }

        /// <summary>Resolves the finished battle: publishes RoundEndedEvent, decides whether the match is over, and carries state into the next round.</summary>
        public RoundOutcome CompleteBattle()
        {
            if (CurrentRunner == null || CurrentRunner.Mode != SimWorldMode.Battle || !CurrentRunner.IsFinished)
            {
                throw new InvalidOperationException("The battle has not finished.");
            }

            RoundOutcome outcome = CurrentRunner.Outcome;
            SimWorld world = CurrentRunner.World;

            _eventBus.Publish(new RoundEndedEvent(RoundNumber, outcome));

            if (outcome != RoundOutcome.NoKill)
            {
                EndMatch(outcome);
            }
            else if (RoundNumber >= _config.MaxRounds)
            {
                EndMatch(RoundOutcome.Draw);
            }
            else
            {
                CarryIntoNextRound(world);
            }

            return outcome;
        }

        private void EndMatch(RoundOutcome outcome)
        {
            // MatchEndedEvent is published by MatchOverState so the result screen waits out the round-resolve pause.
            IsOver = true;
            MatchOutcome = outcome;
        }

        private void CarryIntoNextRound(SimWorld finished)
        {
            float nextRadius = SimMovement.ComputeRoundStartRadius(_config, RoundNumber + 1);

            _playerSpawn = NextSpawn(finished.Player, nextRadius);
            _opponentSpawn = NextSpawn(finished.Opponent, nextRadius);

            if (_config.CarryHealthBetweenRounds)
            {
                _playerHealth = finished.Player.CurrentHealth;
                _opponentHealth = finished.Opponent.CurrentHealth;
            }
            else
            {
                _playerHealth = -1f;
                _opponentHealth = -1f;
            }
        }

        private SimSpawn NextSpawn(SimFighter fighter, float nextRadius)
        {
            Vector3 position = SimMovement.ClampToArena(fighter.Position, _arenaCenter, nextRadius);
            return new SimSpawn(position, fighter.AimDirection);
        }

        private void RetireCurrentWorld()
        {
            if (CurrentRunner == null) return;
            _eventBus.Publish(new SimWorldRetiredEvent(CurrentRunner.World));
        }
    }
}
