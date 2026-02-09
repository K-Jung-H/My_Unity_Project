using UnityEngine;

[RequireComponent(typeof(CarController))]
[RequireComponent(typeof(HealthSystem))]
public class CarEffectController : MonoBehaviour
{
    [Header("References")]
    private CarController carController;
    private HealthSystem healthSystem;

    [Header("Exhaust Effects (배기구)")]
    public ParticleSystem[] exhaustParticles;

    [Header("Damage Smoke (화재 연기)")]
    public ParticleSystem damageSmokeParticle;
    [Range(0f, 1f)] public float smokeStartHealthRatio = 0.5f;
    public float maxSmokeEmission = 20f;

    [Header("Skid Marks (스키드마크)")]
    public WheelSkid[] wheelSkids;

    [Header("Fuel Effect (주유)")]
    public ParticleSystem fuelChargingParticle;
    
    private ParticleSystem.EmissionModule smokeEmissionModule;

    private void Awake()
    {
        carController = GetComponent<CarController>();
        healthSystem = GetComponent<HealthSystem>();
        
        if (exhaustParticles == null || exhaustParticles.Length == 0)
        {
        }
        
        if (damageSmokeParticle != null)
        {
            smokeEmissionModule = damageSmokeParticle.emission;
            damageSmokeParticle.Stop();
        }
        
        if (wheelSkids == null || wheelSkids.Length == 0)
        {
            wheelSkids = GetComponentsInChildren<WheelSkid>();
        }
        
        if (fuelChargingParticle != null)
        {
            fuelChargingParticle.Stop();
        }
    }

    private void Update()
    {
        if (carController == null || healthSystem == null) return;

        HandleExhaust();
        HandleDamageSmoke();
        HandleSkidMarks();
    }
    
    private void HandleExhaust()
    {
        if (exhaustParticles == null) return;
        
        bool isEngineActive = (carController.currentGear != GearState.P)
                           && (carController.currentFuel > 0)
                           && (!healthSystem.IsDead);

        foreach (var ps in exhaustParticles)
        {
            if (ps == null) continue;
            var emission = ps.emission;
            emission.enabled = isEngineActive;
        }
    }
    
    private void HandleDamageSmoke()
    {
        if (damageSmokeParticle == null) return;
        
        if (healthSystem.IsDead)
        {
            if (!damageSmokeParticle.isPlaying) damageSmokeParticle.Play();
            
            var emission = damageSmokeParticle.emission;
            emission.rateOverTime = maxSmokeEmission;
            return;
        }
        
        float currentRatio = healthSystem.CurrentHealth / healthSystem.MaxHealth;

        if (currentRatio <= smokeStartHealthRatio)
        {
            if (!damageSmokeParticle.isPlaying) damageSmokeParticle.Play();
            
            float severity = 1f - (currentRatio / smokeStartHealthRatio);
            
            var emission = damageSmokeParticle.emission;
            emission.rateOverTime = severity * maxSmokeEmission;
        }
        else
        {
            if (damageSmokeParticle.isPlaying) damageSmokeParticle.Stop();
        }
    }
    
    private void HandleSkidMarks()
    {
        if (wheelSkids == null) return;

        bool isSkidding = carController.IsSkidding;

        foreach (var skid in wheelSkids)
        {
            if (skid != null)
            {
                skid.SetSkidActive(isSkidding);
            }
        }
    }
    
    public void SetFuelCharging(bool isCharging)
    {
        if (fuelChargingParticle == null) return;

        if (isCharging)
        {
            if (!fuelChargingParticle.isPlaying) fuelChargingParticle.Play();
        }
        else
        {
            if (fuelChargingParticle.isPlaying) fuelChargingParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}