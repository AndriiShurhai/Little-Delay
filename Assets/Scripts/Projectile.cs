using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 5f;
    [SerializeField] private float _knockbackForce = 10f;

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

        gameObject.layer = ownerRoot.gameObject.layer;

        Destroy(gameObject, _lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_ownerRoot != null && other.transform.IsChildOf(_ownerRoot))
            return;

        if (other.TryGetComponent<PlayerController>(out var player) && player.IsInvincible)
        {
            return;
        }

        if (other.TryGetComponent<HealthComponent>(out var health))
        {
            health.TakeDamage(_damage);

            Vector3 randomForce = new Vector3(
                Random.Range(-_knockbackForce, _knockbackForce),
                Random.Range(-_knockbackForce, _knockbackForce),
                Random.Range(-_knockbackForce, _knockbackForce));

            other.GetComponent<PlayerController>()?.ApplyDeflectionForce(randomForce);
        }

        Explode();
    }

    private void Explode()
    {
        Destroy(gameObject);
    }
}