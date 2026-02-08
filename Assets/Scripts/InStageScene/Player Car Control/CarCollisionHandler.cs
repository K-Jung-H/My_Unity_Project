using UnityEngine;

[RequireComponent(typeof(HealthSystem))]
public class CarCollisionHandler : MonoBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private float heavyImpactThreshold = 5000f;
    [SerializeField] private string explosionKey = "Explosion";
    
    [Header("Environment Settings")]
    [SerializeField] private float environmentDamageFactor = 0.05f;

    private HealthSystem myHealthSystem;
    private int structureLayer;

    private void Awake()
    {
        myHealthSystem = GetComponent<HealthSystem>();
        structureLayer = LayerMask.NameToLayer("Structure_Static");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == structureLayer)
        {
            float impulse = collision.impulse.magnitude;

            if (impulse > heavyImpactThreshold)
            {
                float damage = impulse * environmentDamageFactor;
                myHealthSystem.TakeDamage(damage);

                Debug.Log($"Wall Crash: {collision.gameObject.name} | Damage: {damage:F1}");

                if (collision.contactCount > 0 && EffectManager.Instance != null)
                {
                    ContactPoint contact = collision.contacts[0];
                    EffectManager.Instance.PlayEffect(explosionKey, contact.point, Quaternion.LookRotation(contact.normal));
                }
            }
            return;
        }

        if (collision.gameObject.TryGetComponent(out EnemyCarController enemy))
        {
            if (enemy.EnemyProfile != null)
            {
                float impulse = collision.impulse.magnitude;

                if (impulse > heavyImpactThreshold)
                {
                    float damage = impulse * enemy.EnemyProfile.ImpactDamageFactor;
                    
                    myHealthSystem.TakeDamage(damage);

                    Debug.Log($"Vehicle Crash! Player Dmg: {damage:F1}");

                    if (collision.contactCount > 0 && EffectManager.Instance != null)
                    {
                        ContactPoint contact = collision.contacts[0];
                        EffectManager.Instance.PlayEffect(explosionKey, contact.point, Quaternion.LookRotation(contact.normal));
                    }
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out EnemyCarController enemy))
        {
            if (enemy.EnemyProfile != null)
            {
                float damage = enemy.EnemyProfile.ContinuousDamage * Time.fixedDeltaTime;
                
                myHealthSystem.TakeDamage(damage);

                if (enemy.Health != null)
                {
                    enemy.Health.TakeDamage(damage);
                }
            }
        }
    }
}