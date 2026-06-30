using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 5f;

    private float _damage;
    private Transform _ownerRoot;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(Vector3 velocity, float damage, Transform ownerRoot)
    {
        _damage = damage;
        _ownerRoot = ownerRoot;

        _rigidbody.linearVelocity = velocity;

        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_ownerRoot != null && other.transform.IsChildOf(_ownerRoot))
            return;

        if (other.TryGetComponent<HealthComponent>(out var health))
        {
            health.TakeDamage(_damage);
        }

        Explode();
    }

    private void Explode()
    {
        Destroy(gameObject);
    }
}