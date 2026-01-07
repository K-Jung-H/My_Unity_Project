using UnityEngine;
using System.Collections;

public class EffectController : MonoBehaviour
{
    [Header("Main Settings")]
    public float totalDuration = 2.0f;

    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem[] particles;

    [Header("Light Settings")]
    public bool useLightEffect = false;
    [SerializeField] private Light effectLight;
    public AnimationCurve lightIntensityCurve;
    public float lightDuration = 0.5f;
    public float lightMultiplier = 1.0f;

    private void Awake()
    {
        if (particles == null || particles.Length == 0)
            particles = GetComponentsInChildren<ParticleSystem>();

        if (effectLight == null)
            effectLight = GetComponentInChildren<Light>();
    }

    public void PlayEffect()
    {
        foreach (var ps in particles)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        if (useLightEffect && effectLight != null)
        {
            StartCoroutine(AnimateLightRoutine());
        }
    }

    private IEnumerator AnimateLightRoutine()
    {
        effectLight.enabled = true;
        float timer = 0f;

        while (timer < lightDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / lightDuration;

            float curveValue = lightIntensityCurve.Evaluate(progress);
            effectLight.intensity = curveValue * lightMultiplier;

            yield return null;
        }

        effectLight.enabled = false;
    }
}