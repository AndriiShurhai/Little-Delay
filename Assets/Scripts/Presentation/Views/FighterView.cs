using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using UnityEngine;

namespace LittleDelay.Presentation
{
    /// <summary>
    /// Visual for one fighter. Reads SimFighter, never writes it: position and facing are interpolated between
    /// the last two simulation ticks with SimTickClock.Alpha, so motion is smooth at any frame rate even though
    /// the simulation only steps 50 times a second. No Rigidbody, no CharacterController, no physics.
    /// The same view is reused for the recording world and the battle world; it just rebinds on SimWorldCreatedEvent.
    /// While recording "blind", the opponent's visual stays hidden (the player only learns about it through
    /// reveal windows and the spawn indicator).
    /// </summary>
    public sealed class FighterView : MonoBehaviour
    {
        [SerializeField] private FighterSlot _slot;
        [Tooltip("Visual root toggled on death. Defaults to this GameObject.")]
        [SerializeField] private GameObject _visualRoot;
        [SerializeField] private float _hideDelayAfterDeath = 2f;

        private SimTickClock _clock;
        private SimWorld _world;
        private SimFighter _fighter;
        private float _hideAtTime = -1f;
        private bool _hiddenByIntelRule;
        private System.IDisposable _createdSub, _retiredSub, _diedSub;

        public FighterSlot Slot => _slot;

        public void Initialize(IEventBus eventBus, SimTickClock clock)
        {
            _clock = clock;
            if (_visualRoot == null) _visualRoot = gameObject;

            _createdSub = eventBus.Subscribe<SimWorldCreatedEvent>(e => Bind(e.World));
            _retiredSub = eventBus.Subscribe<SimWorldRetiredEvent>(e => { if (e.World == _world) Unbind(); });
            _diedSub = eventBus.Subscribe<FighterDiedEvent>(e => { if (e.Fighter == _slot) _hideAtTime = Time.time + _hideDelayAfterDeath; });

            SetVisible(false);
        }

        private void OnDestroy()
        {
            _createdSub?.Dispose();
            _retiredSub?.Dispose();
            _diedSub?.Dispose();
        }

        private void Bind(SimWorld world)
        {
            _world = world;
            _fighter = world.GetFighter(_slot);
            _hideAtTime = -1f;

            // Recording is "blind": the opponent exists in the simulation but must not be drawn.
            _hiddenByIntelRule = world.Mode == SimWorldMode.Recording && _slot == FighterSlot.Opponent;

            transform.SetPositionAndRotation(_fighter.Position, FacingOf(_fighter.AimDirection));
            SetVisible(!_hiddenByIntelRule);
        }

        private void Unbind()
        {
            _world = null;
            _fighter = null;
            _hiddenByIntelRule = false;
        }

        private void LateUpdate()
        {
            if (_fighter == null) return;

            if (_hideAtTime > 0f && Time.time >= _hideAtTime)
            {
                SetVisible(false);
                _hideAtTime = -1f;
            }

            // Nothing to interpolate for a view that must stay invisible.
            if (_hiddenByIntelRule) return;

            float alpha = _clock.Alpha;
            Vector3 position = Vector3.Lerp(_fighter.PreviousPosition, _fighter.Position, alpha);
            Vector3 aim = Vector3.Lerp(_fighter.PreviousAimDirection, _fighter.AimDirection, alpha);

            transform.SetPositionAndRotation(position, FacingOf(aim));
        }

        private static Quaternion FacingOf(Vector3 aim)
        {
            aim.y = 0f;
            return aim.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(aim.normalized, Vector3.up) : Quaternion.identity;
        }

        private void SetVisible(bool visible)
        {
            if (_visualRoot != null && _visualRoot.activeSelf != visible) _visualRoot.SetActive(visible);
        }
    }
}