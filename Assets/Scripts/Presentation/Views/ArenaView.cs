using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Shows the arena boundary at the simulation's current radius. The old ArenaController moved physical wall
    /// rigidbodies; now the arena is a circle the simulation clamps against, and this only draws it. The shown
    /// radius eases toward the real one so the round-to-round shrink animates instead of snapping.
    /// Boundary visual: a ring/cylinder whose XZ scale of (baseDiameter) corresponds to a diameter of baseDiameter world units.
    /// </summary>
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] private Transform _boundary;
        [Tooltip("World diameter the boundary object has at scale 1 (a default Unity cylinder is 1).")]
        [SerializeField] private float _baseDiameter = 1f;
        [SerializeField] private float _catchUpSpeed = 1f;

        private SimWorld _world;
        private float _shownRadius = -1f;
        private System.IDisposable _createdSub;

        public void Initialize(IEventBus eventBus)
        {
            _createdSub = eventBus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                _world = e.World;
                if (_shownRadius < 0f) _shownRadius = _world.ArenaRadius;
                if (_boundary != null) _boundary.position = new Vector3(_world.ArenaCenter.x, _boundary.position.y, _world.ArenaCenter.z);
            });
        }

        private void OnDestroy() => _createdSub?.Dispose();

        private void Update()
        {
            if (_world == null || _boundary == null) return;

            _shownRadius = Mathf.MoveTowards(_shownRadius, _world.ArenaRadius, _catchUpSpeed * Time.deltaTime);

            float scale = _shownRadius * 2f / Mathf.Max(0.0001f, _baseDiameter);
            Vector3 current = _boundary.localScale;
            _boundary.localScale = new Vector3(scale, current.y, scale);
        }
    }
}
