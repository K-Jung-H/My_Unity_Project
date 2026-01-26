using UnityEngine;
using System;

public class HealthSystem : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDeath;

    public bool IsDead => currentHealth <= 0;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void InitializeHealth(float healthValue)
    {
        maxHealth = healthValue;
        currentHealth = healthValue;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth -= amount;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} Destroyed!");
        OnDeath?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        currentHealth += amount;
        if (currentHealth > maxHealth) currentHealth = maxHealth;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void MultiplyCurrentHealth(float ratio)
    {
        currentHealth = Mathf.FloorToInt(currentHealth * ratio);
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0 && !IsDead) 
        {
            Die();
        }
    }

    public void SetHealthByMaxRatio(float ratio)
    {
        if (ratio > 0f)
        {
            currentHealth = Mathf.FloorToInt(maxHealth * ratio);
        }
        else
        {
            currentHealth = maxHealth;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}