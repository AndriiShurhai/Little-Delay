using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;


public class FighterSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _ghostPrefab;

    public event Action<Fighter> OnGhostCreated;

    public void SpawnGhost(Fighter fighter)
    {
        GameObject ghost = Instantiate(_ghostPrefab, fighter.Recording.StartPosition, fighter.Recording.StartBodyRotation);
        fighter.BindGhost(ghost);
        ConfigurePlayback(fighter);

        OnGhostCreated?.Invoke(fighter);
    }

    public void RespawnGhost(Fighter fighter)
    {
        fighter.GhostObject.SetActive(true);
        fighter.Controller.Teleport(fighter.Recording.StartPosition, fighter.Recording.StartBodyRotation);

        ConfigurePlayback(fighter);
    }

    private void ConfigurePlayback(Fighter fighter)
    {
        fighter.Controller.SetInputSource(fighter.Playback);
        fighter.Playback.StartPlayback(fighter.Recording);
        fighter.Playback.PausePlayback();
    }

    public void ResumeALL(IEnumerable<Fighter> fighters)
    {
        foreach (Fighter fighter in fighters)
        {
            if (fighter.IsAlive)
            {
                fighter.Playback.ResumePlayback();
            }
        }
    }
}