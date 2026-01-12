using UnityEngine;
using UnityEngine.UI;

public class FuelGauge : MonoBehaviour
{
    [Header("Target Reference")]
    [SerializeField] private CarController targetCar;

    [Header("UI References")]
    public Slider fuelSlider;
    public Image fillImage;

    [Header("Settings")]
    public Gradient colorGradient;

    void Awake()
    {
        PlayerManager.OnLocalPlayerCreated += HandlePlayerCreated;
    }

    void OnDestroy()
    {
        PlayerManager.OnLocalPlayerCreated -= HandlePlayerCreated;
    }

    void Start()
    {
        if (fuelSlider != null)
        {
            fuelSlider.interactable = false;
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 1f;
            fuelSlider.value = 0f;
        }

        if (targetCar == null)
        {
            CarController existingCar = FindObjectOfType<CarController>();
            if (existingCar != null && existingCar.CompareTag("Player"))
            {
                HandlePlayerCreated(existingCar);
            }
        }
    }

    private void HandlePlayerCreated(CarController createdCar)
    {
        targetCar = createdCar;
    }

    void Update()
    {
        if (targetCar == null) return;

        UpdateUI();
    }

    private void UpdateUI()
    {
        float current = targetCar.currentFuel;
        float max = targetCar.maxFuel;
        
        float ratio = (max > 0) ? (current / max) : 0f;

        if (fuelSlider != null)
        {
            fuelSlider.value = ratio;
        }

        if (fillImage != null)
        {
            fillImage.color = colorGradient.Evaluate(ratio);
        }
    }
}