using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ChunkEnvironmentController : MonoBehaviour
{
    [Header("Target Settings")]
    public ParticleSystem environmentParticle;
    public LayerMask playerLayer;

    private void Start()
    {
        if (environmentParticle != null)
        {
            if (environmentParticle.isPlaying)
            {
                environmentParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayerTrigger(other))
        {
            if (environmentParticle != null && !environmentParticle.isPlaying)
            {
                environmentParticle.Play(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayerTrigger(other))
        {
            if (environmentParticle != null && environmentParticle.isPlaying)
            {
                environmentParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private bool IsPlayerTrigger(Collider other)
    {
        bool isCorrectLayer = (playerLayer.value & (1 << other.gameObject.layer)) != 0;
        
        return isCorrectLayer && other.isTrigger;
    }
}