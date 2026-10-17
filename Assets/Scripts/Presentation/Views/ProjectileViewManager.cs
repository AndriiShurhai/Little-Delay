using System;
using System.Collections.Generic;
using Drewio.UnityToolkit.Core;
using Drewio.UnityToolkit.Pooling;
using LittleDelay.Simulation.Combat;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Spawns and releases pooled ProjectileViews in response to simulation events and positions them every
    /// frame from SimProjectile state. Replaces the old Projectile MonoBehaviour (Instantiate/Destroy, Rigidbody,
    /// OnTriggerEnter, Random knockback): all of that is now SimCombat's job.
    /// While recording "blind", only the player's own projectiles get a visual; the opponent's stay invisible.
    /// </summary>
    public sealed class ProjectileViewManager : MonoBehaviour
    {
        [SerializeField] private ProjectileView _prefab;
        [SerializeField] private Transform _poolRoot;
        [SerializeField] private int _prewarmCount = 16;
        [Tooltip("Lifts the visual (e.g. to chest height) without touching simulation positions.")]
        [SerializeField] private float _heightOffset = 0f;

        private readonly Dictionary<int, Entry> _active = new();
        private readonly List<int> _scratch = new();
        private GameObjectPool<ProjectileView> _pool;
        private SimTickClock _clock;
        private SimWorld _world;
        private readonly List<IDisposable> _subs = new();

        private struct Entry
        {
            public ProjectileView View;
            public SimProjectile State;
        }

        public void Initialize(IEventBus eventBus, SimTickClock clock)
        {
            _clock = clock;
            _pool = new GameObjectPool<ProjectileView>(_prefab, _poolRoot != null ? _poolRoot : transform, Mathf.Max(1, _prewarmCount));

            _subs.Add(eventBus.Subscribe<SimWorldCreatedEvent>(e => { ReleaseAll(); _world = e.World; }));
            _subs.Add(eventBus.Subscribe<SimWorldRetiredEvent>(e => { if (e.World == _world) { ReleaseAll(); _world = null; } }));
            _subs.Add(eventBus.Subscribe<ProjectileSpawnedEvent>(HandleSpawned));
            _subs.Add(eventBus.Subscribe<ProjectileDespawnedEvent>(e => Release(e.ProjectileId)));
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void HandleSpawned(ProjectileSpawnedEvent e)
        {
            if (_world == null) return;

            // Blind recording: the opponent's shots exist in the simulation but must not be seen.
            // Release() ignores ids it never tracked, so the matching despawn event is harmless.
            if (_world.Mode == SimWorldMode.Recording && e.Owner != FighterSlot.Player) return;

            SimProjectile state = null;
            for (int i = 0; i < _world.Projectiles.Count; i++)
            {
                if (_world.Projectiles[i].Id == e.ProjectileId) { state = _world.Projectiles[i]; break; }
            }
            if (state == null) return;

            ProjectileView view = _pool.Get();
            view.PlaceAt(Visual(e.Position), Facing(e.Velocity));
            _active[e.ProjectileId] = new Entry { View = view, State = state };
        }

        private void Release(int projectileId)
        {
            if (!_active.TryGetValue(projectileId, out Entry entry)) return;

            _pool.Release(entry.View);
            _active.Remove(projectileId);
        }

        private void ReleaseAll()
        {
            _scratch.Clear();
            _scratch.AddRange(_active.Keys);
            foreach (int id in _scratch) Release(id);
        }

        private void LateUpdate()
        {
            if (_active.Count == 0) return;

            float alpha = _clock.Alpha;
            foreach (Entry entry in _active.Values)
            {
                SimProjectile p = entry.State;
                entry.View.transform.SetPositionAndRotation(
                    Visual(Vector3.Lerp(p.PreviousPosition, p.Position, alpha)),
                    Facing(p.Velocity));
            }
        }

        private Vector3 Visual(Vector3 simPosition) => simPosition + Vector3.up * _heightOffset;

        private static Quaternion Facing(Vector3 velocity) =>
            velocity.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(velocity.normalized, Vector3.up) : Quaternion.identity;
    }
}