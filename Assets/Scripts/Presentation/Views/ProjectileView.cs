using Drewio.UnityToolkit.Core;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>Pooled visual for one SimProjectile. Pure display: it owns no velocity, no collider and no lifetime.</summary>
    public sealed class ProjectileView : MonoBehaviour, IPoolable
    {
        [SerializeField] private TrailRenderer _trail;

        public void PlaceAt(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            if (_trail != null) _trail.Clear(); // otherwise the trail streaks from wherever the pooled object last was
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            if (_trail != null) _trail.Clear();
        }
    }
}
