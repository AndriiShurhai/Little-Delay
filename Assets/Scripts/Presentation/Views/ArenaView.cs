using Drewio.UnityToolkit.Core;
using LittleDelay.Simulation.Core;
using LittleDelay.Simulation.Events;
using LittleDelay.Simulation.Fighters;
using System;
using System.Collections.Generic;
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
        [Header("Optional 3D Boundary Mesh (Floor Disc or Forcefield Cylinder)")]
        [SerializeField] private Transform _boundary;
        [Tooltip("World diameter the boundary object has at scale 1 (a default Unity cylinder is 1).")]
        [SerializeField] private float _baseDiameter = 1f;
        [SerializeField] private float _catchUpSpeed = 8f;

        [Header("Procedural Ground Rings")]
        [SerializeField] private int _ringSegments = 96;
        [SerializeField] private float _groundHeightOffset = 0.05f;
        [SerializeField] private float _mainRingWidth = 0.18f;
        [SerializeField] private float _subRingWidth = 0.07f;
        [Tooltip("Assign an Unlit/Sprite material so the shader is never stripped in builds.")]
        [SerializeField] private Material _ringMaterialAsset;

        [Header("Ring Colors")]
        [SerializeField] private Color _normalBorderColor = new Color(0f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color _wallProximityColor = new Color(1f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color _shrinkPreviewColor = new Color(1f, 0.65f, 0f, 0.45f);
        [SerializeField] private Color _minRadiusRingColor = new Color(1f, 1f, 1f, 0.15f);

        private SimWorld _world;
        private SimMatchConfig _config;
        private float _shownRadius = -1f;
        private LineRenderer _activeBorderRing;
        private LineRenderer _shrinkPreviewRing;
        private LineRenderer _minZoneRing;
        private Material _runtimeMaterial;
        private readonly List<IDisposable> _subs = new();

        public void Initialize(IEventBus eventBus, SimMatchConfig config = null)
        {
            _config = config ?? new SimMatchConfig();

            EnsureRingsCreated();

            _subs.Add(eventBus.Subscribe<SimWorldCreatedEvent>(e =>
            {
                _world = e.World;
                if (_shownRadius < 0f) _shownRadius = _world.ArenaRadius;

                if (_boundary != null)
                {
                    _boundary.position = new Vector3(_world.ArenaCenter.x, _boundary.position.y, _world.ArenaCenter.z);
                }

                DrawCircle(_minZoneRing, _world.ArenaCenter, _config.ArenaMinRadius);
            }));
        }

        private void OnDestroy()
        {
            foreach (IDisposable sub in _subs) sub.Dispose();
            _subs.Clear();
        }

        private void Update()
        {
            if (_world == null) return;

            _shownRadius = Mathf.MoveTowards(_shownRadius, _world.ArenaRadius, _catchUpSpeed * Time.deltaTime);

            // 1. Scale optional 3D boundary object (floor cylinder or forcefield mesh)
            if (_boundary != null)
            {
                float scale = _shownRadius * 2f / Mathf.Max(0.0001f, _baseDiameter);
                Vector3 current = _boundary.localScale;
                _boundary.localScale = new Vector3(scale, current.y, scale);
            }

            // 2. Draw primary active border ring
            DrawCircle(_activeBorderRing, _world.ArenaCenter, _shownRadius);
            UpdateActiveBorderColor();

            // 3. During Recording, show the inner ring where the Battle border will shrink to at this exact elapsed time
            if (_config.ArenaShrinkRatePerSecond > 0f)
            {
                float battleRadiusAtThisTick = SimMovement.ComputeShrunkRadius(
                    _config, _world.ArenaStartRadius, _world.ElapsedSeconds);

                if (battleRadiusAtThisTick < _shownRadius - 0.05f)
                {
                    _shrinkPreviewRing.enabled = true;
                    DrawCircle(_shrinkPreviewRing, _world.ArenaCenter, battleRadiusAtThisTick);
                }
                else
                {
                    _shrinkPreviewRing.enabled = false;
                }
            }
            else
            {
                _shrinkPreviewRing.enabled = false;
            }
        }

        private void UpdateActiveBorderColor()
        {
            if (_activeBorderRing == null || _world == null) return;

            // Warn the player when they approach the clamp wall during Recording, or pulse during Battle shrink
            Vector3 playerOffset = _world.Player.Position - _world.ArenaCenter;
            playerOffset.y = 0f;
            float distFromWall = Mathf.Max(0f, _shownRadius - playerOffset.magnitude);

            float warningT = _world.Mode == SimWorldMode.Recording
                ? 1f - Mathf.Clamp01(distFromWall / 1.25f)
                : 0.35f + 0.25f * Mathf.Sin(Time.time * 6f);

            Color c = Color.Lerp(_normalBorderColor, _wallProximityColor, warningT);
            _activeBorderRing.startColor = c;
            _activeBorderRing.endColor = c;
        }

        private void EnsureRingsCreated()
        {
            if (_activeBorderRing != null) return;

            if (_runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
                _runtimeMaterial = _ringMaterialAsset != null ? _ringMaterialAsset : new Material(shader);
            }

            _activeBorderRing = CreateRing("ActiveBorderRing", _mainRingWidth, _normalBorderColor);
            _shrinkPreviewRing = CreateRing("BattleShrinkPreviewRing", _subRingWidth, _shrinkPreviewColor);
            _minZoneRing = CreateRing("MinArenaRadiusRing", _subRingWidth, _minRadiusRingColor);
        }

        private LineRenderer CreateRing(string ringName, float width, Color color)
        {
            var go = new GameObject(ringName);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = Mathf.Max(16, _ringSegments);
            lr.startWidth = width;
            lr.endWidth = width;
            lr.material = _runtimeMaterial;
            lr.startColor = color;
            lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            return lr;
        }

        private void DrawCircle(LineRenderer lr, Vector3 center, float radius)
        {
            if (lr == null) return;

            int count = lr.positionCount;
            float y = center.y + _groundHeightOffset;
            float step = Mathf.PI * 2f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * step;
                lr.SetPosition(i, new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    y,
                    center.z + Mathf.Sin(angle) * radius));
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Vector3 center = _world != null
                ? _world.ArenaCenter
                : (_boundary != null ? _boundary.position : transform.position);

            float initialRadius = _config != null ? _config.ArenaInitialRadius : 15f;
            float minRadius = _config != null ? _config.ArenaMinRadius : 4f;
            float projectileMargin = _config != null ? _config.ProjectileArenaExitMargin : 2f;

            // Outer Initial Arena Radius (Cyan)
            Gizmos.color = new Color(0f, 0.85f, 1f, 0.8f);
            DrawGizmoCircle(center, _world != null ? _world.ArenaRadius : initialRadius);

            // Minimum Arena Radius (Yellow)
            Gizmos.color = new Color(1f, 0.85f, 0f, 0.6f);
            DrawGizmoCircle(center, minRadius);

            // Projectile Despawn Margin (Red)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            DrawGizmoCircle(center, (_world != null ? _world.ArenaRadius : initialRadius) + projectileMargin);
        }

        private static void DrawGizmoCircle(Vector3 center, float radius, int segments = 64)
        {
            float step = Mathf.PI * 2f / segments;
            Vector3 prev = center + new Vector3(radius, 0.05f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0.05f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
#endif
    }
}
