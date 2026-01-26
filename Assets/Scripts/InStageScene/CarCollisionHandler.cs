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

    private void Awake()
    {
        myHealthSystem = GetComponent<HealthSystem>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        int layer = collision.gameObject.layer;
        
        if (LayerMask.LayerToName(layer) == "Structure_Static")
        {
            float impulse = collision.impulse.magnitude;

            if (impulse > heavyImpactThreshold)
            {
                float damage = impulse * environmentDamageFactor;
                myHealthSystem.TakeDamage(damage);

                Debug.Log($"Wall Crash: {collision.gameObject.name} | Damage: {damage:F1}");

                ContactPoint contact = collision.contacts[0];
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect(explosionKey, contact.point, Quaternion.LookRotation(contact.normal));
                }
            }
            return;
        }

        var enemy = collision.gameObject.GetComponent<EnemyCarController>();
        
        if (enemy != null && enemy.EnemyProfile != null)
        {
            float impulse = collision.impulse.magnitude;

            if (impulse > heavyImpactThreshold)
            {
                float damage = impulse * enemy.EnemyProfile.ImpactDamageFactor;
                
                myHealthSystem.TakeDamage(damage);

                if (enemy.Health != null)
                {
                    enemy.Health.TakeDamage(damage);
                }

                Debug.Log($"Vehicle Crash! Player Dmg: {damage:F1} | Enemy Dmg: {damage:F1}");

                ContactPoint contact = collision.contacts[0];
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect(explosionKey, contact.point, Quaternion.LookRotation(contact.normal));
                }
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        var enemy = collision.gameObject.GetComponent<EnemyCarController>();
        if (enemy != null && enemy.EnemyProfile != null)
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