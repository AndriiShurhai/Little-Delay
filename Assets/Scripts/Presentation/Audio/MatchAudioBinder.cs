using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Audio;
using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Abilities;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Routes gameplay events to the toolkit AudioPlaybackService. Plain C# (no MonoBehaviour): the one place that
    /// knows which event means which sound, so neither the simulation nor the views ever touch audio.
    /// Created and disposed by MatchCompositionRoot.
    /// While recording "blind", sounds that would reveal the opponent (their shots, dashes, projectile bounces)
    /// are suppressed.
    /// </summary>
    public sealed class MatchAudioBinder : IDisposable
    {
        private readonly AudioPlaybackService _audio;
        private readonly MatchAudioCues _cues;
        private readonly List<IDisposable> _subs = new();
        private SimWorldMode _mode = SimWorldMode.Battle;

        public MatchAudioBinder(IEventBus bus, AudioPlaybackService audio, MatchAudioCues cues)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _cues = cues ?? throw new ArgumentNullException(nameof(cues));

            _subs.Add(bus.Subscribe<SimWorldCreatedEvent>(e => _mode = e.Mode));

            _subs.Add(bus.Subscribe<AbilityUsedEvent>(e =>
            {
                if (IsHiddenFromPlayer(e.Fighter)) return;
                Play(e.AbilityId == SimDashAbility.Id ? _cues.DashUsed : _cues.BlastFired);
            }));
            _subs.Add(bus.Subscribe<AbilityFailedEvent>(e => { if (e.Fighter == FighterSlot.Player) Play(_cues.AbilityOnCooldown); }));

            // A bounce involves an enemy projectile, so during a blind recording it would give its position away.
            _subs.Add(bus.Subscribe<ProjectileDeflectedEvent>(_ => { if (_mode != SimWorldMode.Recording) Play(_cues.ProjectileDeflected); }));

            _subs.Add(bus.Subscribe<FighterDamagedEvent>(e => { if (e.Source == DamageSource.Projectile) Play(_cues.FighterHit); }));
            _subs.Add(bus.Subscribe<PassivityDetectedEvent>(_ => Play(_cues.PassivityWarning)));
            _subs.Add(bus.Subscribe<FighterDiedEvent>(_ => Play(_cues.FighterDied)));
            _subs.Add(bus.Subscribe<CountdownTickEvent>(_ => Play(_cues.CountdownTick)));
            _subs.Add(bus.Subscribe<RecordingStartedEvent>(_ => Play(_cues.RecordingStarted)));
            _subs.Add(bus.Subscribe<RecordingCompletedEvent>(_ => Play(_cues.RecordingSaved)));
            _subs.Add(bus.Subscribe<BattleStartedEvent>(_ => Play(_cues.BattleStarted)));
            _subs.Add(bus.Subscribe<MatchEndedEvent>(e => Play(e.Outcome == RoundOutcome.Draw ? _cues.MatchDrawn : _cues.MatchWon)));

            _subs.Add(bus.Subscribe<MatchStartedEvent>(_ =>
            {
                if (!string.IsNullOrEmpty(_cues.MatchMusic) && _audio.CurrentMusicName != _cues.MatchMusic)
                {
                    _audio.PlayMusic(_cues.MatchMusic);
                }
            }));
        }

        private bool IsHiddenFromPlayer(FighterSlot fighter) =>
            _mode == SimWorldMode.Recording && fighter != FighterSlot.Player;

        private void Play(SoundGroup group)
        {
            if (group == null || group.IsEmpty) return;
            _audio.PlaySfx(group.PickRandom());
        }

        public void Dispose()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }
    }
}