using UnityEngine;
using UnityEngine.Rendering;

public class SpeedBlurManager : MonoBehaviour
{
    public static SpeedBlurManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private Volume globalVolume;
    
    private CarController targetCar;

    [Header("Blur Settings")]
    [SerializeField] private float minSpeed = 30f;
    [SerializeField] private float maxSpeed = 150f;
    [SerializeField, Range(0f, 1f)] private float maxIntensity = 0.05f;
    [SerializeField] private float smoothing = 5f;

    private RadialBlurVolume radialBlurVolume;
    private float currentIntensity;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Initialize()
    {
        if (globalVolume != null && globalVolume.profile.TryGet(out RadialBlurVolume vol))
        {
            radialBlurVolume = vol;
        }
    }

    public void SetTargetCar(CarController car)
    {
        targetCar = car;
        if (radialBlurVolume != null) radialBlurVolume.intensity.value = 0f; 
    }

    private void Update()
    {
        if (radialBlurVolume == null || targetCar == null) return;

        float speed = targetCar.CurrentSpeed;
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);
        float targetIntensity = Mathf.Lerp(0f, maxIntensity, t);
        
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothing);
        radialBlurVolume.intensity.value = currentIntensity;
    }
}