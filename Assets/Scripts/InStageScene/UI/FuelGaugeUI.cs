using UnityEngine;
using UnityEngine.UI;

public class FuelGaugeUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider fuelSlider;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private Gradient colorGradient;

    private CarController targetCar;

    public void Initialize()
    {
        if (fuelSlider != null)
        {
            fuelSlider.interactable = false;
            fuelSlider.minValue = 0f;
            fuelSlider.maxValue = 1f;
            fuelSlider.value = 0f;
        }
    }
    
    public void SetTarget(CarController car)
    {
        targetCar = car;
        if (fuelSlider != null)
        {
            fuelSlider.value = (targetCar != null && targetCar.maxFuel > 0) ? (targetCar.currentFuel / targetCar.maxFuel) : 0f;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (targetCar == null) return;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (targetCar == null) return;

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