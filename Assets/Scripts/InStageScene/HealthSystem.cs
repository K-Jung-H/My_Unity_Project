using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;


    [SerializeField]
    private int currentHealth;
    public int CurrentHealth => currentHealth;
    
    public bool IsDead => currentHealth <= 0;

    [Header("Damage Settings")]
    public float enemyContactDamagePerSec = 10f;
    public int collisionImpactDamage = 20;

    [Header("Collision Configuration")]
    public LayerMask enemyLayerMask;
    public LayerMask collisionLayerMask;
    public float collisionMinForce = 8000f;
    public float explosionEffectCoolTime = 0.5f;

    [Header("Visual Effects")]
    public string impactEffectKey = "Explosion";
    public string deathEffectKey = "";

    public event Action OnDeath;
    public event Action<int, int> OnHealthChanged;
    public event Action<int> OnDamageTaken;

    private float damageAccumulator = 0f;
    private float lastExplosionTime = -999f;

    private void OnValidate()
    {
        if (Application.isEditor && !Application.isPlaying)
        {
            currentHealth = maxHealth;
        }
    }

    private void OnEnable()
    {
        Initialize();
    }

    public void Initialize()
    {
        currentHealth = maxHealth;
        damageAccumulator = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth -= amount;
        


        OnDamageTaken?.Invoke(amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Revive(float healthRatio)
    {
        if (healthRatio > 0f)
            currentHealth = Mathf.FloorToInt(currentHealth * healthRatio);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (!string.IsNullOrEmpty(deathEffectKey) && EffectManager.Instance != null)
        {
            EffectManager.Instance.PlayEffect(deathEffectKey, transform.position, transform.rotation);
        }

        Debug.Log($"[HealthSystem] {gameObject.name} Died.");
        OnDeath?.Invoke();
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsDead) return;

        if (((1 << collision.gameObject.layer) & enemyLayerMask) != 0)
        {
            damageAccumulator += enemyContactDamagePerSec * Time.deltaTime;
            if (damageAccumulator >= 1f)
            {
                int damageToApply = Mathf.FloorToInt(damageAccumulator);
                TakeDamage(damageToApply);
                damageAccumulator -= damageToApply; 
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastExplosionTime < explosionEffectCoolTime) return;

        if (collisionLayerMask == (collisionLayerMask | (1 << collision.gameObject.layer)))
        {
            if (collision.impulse.magnitude >= collisionMinForce)
            {
                lastExplosionTime = Time.time;
                
                if (!string.IsNullOrEmpty(impactEffectKey) && EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect(impactEffectKey, transform.position, transform.rotation);
                }

                TakeDamage(collisionImpactDamage);
            }
        }
    }
}