using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


public class BlastAbility : BaseAbility
{
    [SerializeField] private Projectile _projectilePrefab;

    [SerializeField] private Transform _spawnPoint;

    [SerializeField] private float _projectileSpeed = 20f;
    [SerializeField] private float _damage = 50f;


    public override void Execute(Vector3 targetPoint)
    {
        if (!IsReady || _projectilePrefab == null) return;

        Vector3 origin = _spawnPoint != null
            ? _spawnPoint.position
            : Owner.Transform.position + Owner.Transform.forward * 0.5f + Vector3.up * 0.5f;

        Vector3 direction = (targetPoint - origin).normalized;

        if (direction == Vector3.zero)
        {
            direction = Owner.Transform.forward;
        }

        if (Physics.Raycast(origin, direction, out RaycastHit hit, Vector3.Distance(origin, targetPoint)))
        {
            targetPoint = hit.point;
            direction = (targetPoint - origin).normalized;
        }

        Projectile projectile = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));

        projectile.Initialize(direction * _projectileSpeed, _damage, Owner.Transform);

        StartCooldown();
    }

    private void OnDrawGizmos()
    {
        if (_spawnPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_spawnPoint.position, 0.1f);
    }
}