using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

[System.Serializable]
public struct OutlineFuelState
{
    [Range(0f, 1f)]
    public float fuelRatio;
    public Color color;
    [Range(0f, 5f)] public float blurIntensity;
    [Range(1, 10)] public int outlineThickness;
}

public class OutlineBlurManager : MonoBehaviour
{
    public static OutlineBlurManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Volume globalVolume; 
    private CarController targetCar;

    [Header("Configuration")]
    public List<OutlineFuelState> outlineStates = new List<OutlineFuelState>();

    [Header("Status (Read Only)")]
    public bool isOutlineActive = false;
    public OutlineFuelState currentOutlineState;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (outlineStates.Count > 0)
        {
            outlineStates.Sort((a, b) => b.fuelRatio.CompareTo(a.fuelRatio));
        }
    }

    public void SetTargetCar(CarController car)
    {
        targetCar = car;
    }

    void Update()
    {
        if (targetCar != null)
        {
            ProcessFuelState();
        }
        UpdateVolume();
    }

    void OnValidate()
    {
        if (outlineStates.Count > 0)
        {
            outlineStates.Sort((a, b) => b.fuelRatio.CompareTo(a.fuelRatio));
        }
    }

    private void ProcessFuelState()
    {
        if (outlineStates == null || outlineStates.Count == 0) return;

        float currentRatio = targetCar.currentFuel / targetCar.maxFuel;
        currentOutlineState.fuelRatio = currentRatio;

        if (currentRatio > outlineStates[0].fuelRatio)
        {
            isOutlineActive = false;
            return;
        }

        isOutlineActive = true;

        if (currentRatio <= outlineStates[outlineStates.Count - 1].fuelRatio)
        {
            SetCurrentState(outlineStates[outlineStates.Count - 1]);
            return;
        }

        for (int i = 0; i < outlineStates.Count - 1; i++)
        {
            OutlineFuelState start = outlineStates[i];
            OutlineFuelState end = outlineStates[i + 1];

            if (currentRatio <= start.fuelRatio && currentRatio > end.fuelRatio)
            {
                float range = start.fuelRatio - end.fuelRatio;
                float t = (start.fuelRatio - currentRatio) / range;

                InterpolateState(start, end, t);
                return;
            }
        }
    }

    private void SetCurrentState(OutlineFuelState state)
    {
        currentOutlineState.color = state.color;
        currentOutlineState.blurIntensity = state.blurIntensity;
        currentOutlineState.outlineThickness = state.outlineThickness;
    }

    private void InterpolateState(OutlineFuelState from, OutlineFuelState to, float t)
    {
        currentOutlineState.color = Color.Lerp(from.color, to.color, t);
        currentOutlineState.blurIntensity = Mathf.Lerp(from.blurIntensity, to.blurIntensity, t);
        currentOutlineState.outlineThickness = (int)Mathf.Lerp(from.outlineThickness, to.outlineThickness, t);
    }

    private void UpdateVolume()
    {
        if (globalVolume == null || globalVolume.profile == null) return;

        if (globalVolume.profile.TryGet(out OutlineBlurVolume volumeComponent))
        {
            volumeComponent.isActive.overrideState = true;
            volumeComponent.isActive.value = isOutlineActive;

            if (isOutlineActive)
            {
                volumeComponent.outlineColor.overrideState = true;
                volumeComponent.outlineColor.value = currentOutlineState.color;

                volumeComponent.outlineThickness.overrideState = true;
                volumeComponent.outlineThickness.value = currentOutlineState.outlineThickness;

                volumeComponent.blurIntensity.overrideState = true;
                volumeComponent.blurIntensity.value = currentOutlineState.blurIntensity;
            }
        }
    }
}