using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class RoundManager : MonoBehaviour
{
    public event Action<Fighter> OnMatchWon;
    public event Action OnMatchDraw;
    public event Action<int> OnRoundContinues;

    private readonly List<Fighter> _fighters = new();

    private int _currentRound = 1;
    private int _pendingPlaybackCompletions;
    private bool _deathPending;
    private bool _roundResolved = true;

    public void RegisterFighter(Fighter fighter)
    {
        _fighters.Add(fighter);
        fighter.Lifecycle.OnGhostDied += HandleGhostDied;
        fighter.Playback.OnPlaybackCompleted += HandlePlaybackCompleted;
    }

    public void ClearForNewMatch()
    {
        foreach (Fighter fighter in _fighters)
        {
            if (fighter.Lifecycle != null) fighter.Lifecycle.OnGhostDied -= HandleGhostDied;
            if (fighter.Playback != null) fighter.Playback.OnPlaybackCompleted -= HandlePlaybackCompleted;
        }

        _fighters.Clear();
        _currentRound = 1;
        _roundResolved = false;
    }

    public void BeginRound()
    {
        _pendingPlaybackCompletions = 0;
        _deathPending = false;
        _roundResolved = false;
    }

    private void HandleGhostDied(GameObject ghost)
    {
        _deathPending = true;
    }

    private void HandlePlaybackCompleted()
    {
        _pendingPlaybackCompletions++;
    }

    private void LateUpdate()
    {
        if (_roundResolved) return;

        if (_fighters.Count == 0) return;

        if (_deathPending)
        {
            _roundResolved = true;

            List<Fighter> survivors = _fighters.FindAll(f => f.IsAlive);

            if (survivors.Count == 1)
            {
                OnMatchWon?.Invoke(survivors[0]);
            }
            else
            {
                OnMatchDraw?.Invoke();
            }

            return;
        }

        if (_pendingPlaybackCompletions >= _fighters.Count)
        {
            _roundResolved = true;
            _currentRound++;
            OnRoundContinues?.Invoke(_currentRound);
        }
    }
}