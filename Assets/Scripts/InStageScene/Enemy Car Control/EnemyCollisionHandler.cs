using UnityEngine;

[RequireComponent(typeof(EnemyCarController))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyCollisionHandler : MonoBehaviour
{
    private EnemyCarController enemyController;
    private HealthSystem myHealth;

    [Header("Collision Settings")]
    [SerializeField] private float heavyImpactThreshold = 4000f;
    [SerializeField] private float environmentDamageFactor = 0.1f;
    [SerializeField] private string explosionKey = "Explosion";

    private int structureLayer;
    private int playerLayer;
    private int enemyLayer;

    private void Awake()
    {
        enemyController = GetComponent<EnemyCarController>();
        myHealth = GetComponent<HealthSystem>();

        structureLayer = LayerMask.NameToLayer("Structure_Static");
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (myHealth.IsDead) return;

        float impulse = collision.impulse.magnitude;

        if (impulse < heavyImpactThreshold) return;

        float damage = 0f;
        int layer = collision.gameObject.layer;

        if (layer == structureLayer)
        {
            damage = impulse * environmentDamageFactor;
            Debug.Log($"Enemy hit Wall! [Impulse: {impulse:F1} -> Damage: {damage:F1}]");
        }
        else if (layer == playerLayer || layer == enemyLayer)
        {
            if (enemyController.EnemyProfile != null)
            {
                damage = impulse * enemyController.EnemyProfile.ImpactDamageFactor;
            }
            else
            {
                damage = impulse * 0.05f;
            }
            Debug.Log($"Enemy hit Vehicle! [Impulse: {impulse:F1} -> Damage: {damage:F1}]");
        }

        if (damage > 0)
        {
            myHealth.TakeDamage(damage);
            PlayCrashEffect(collision);
        }
    }

    private void PlayCrashEffect(Collision collision)
    {
        if (collision.contactCount > 0 && EffectManager.Instance != null)
        {
            ContactPoint contact = collision.contacts[0];
            EffectManager.Instance.PlayEffect(explosionKey, contact.point, Quaternion.LookRotation(contact.normal));
        }
    }
}