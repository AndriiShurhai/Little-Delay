using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Fighter
{
    public int ID { get; }
    public Transform SpawnPoint { get; }
    public Recording Recording { get; set; }

    public GameObject GhostObject { get; private set; }
    public PlayerController Controller { get; private set; }
    public InputPlayback Playback { get; private set; }
    public HealthComponent Health { get; private set; }
    public GhostLifecycle Lifecycle { get; private set; }

    public bool IsAlive => GhostObject != null && Health != null && !Health.IsDead;

    public Fighter (int id, Transform spawnPoint)
    {
        ID = id;
        SpawnPoint = spawnPoint;
    }

    public void BindGhost(GameObject ghost)
    {
        GhostObject = ghost;
        Controller = ghost.GetComponent<PlayerController>();
        Playback = ghost.GetComponent<InputPlayback>();
        Health = ghost.GetComponent<HealthComponent>();
        Lifecycle = ghost.GetComponent<GhostLifecycle>();
    }
}