using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GearBoxUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Slider gearSlider;
    [SerializeField] private TextMeshProUGUI currentGearText;

    private CarController targetCar;

    public void Initialize()
    {
        if (gearSlider != null)
        {
            gearSlider.minValue = 0;
            gearSlider.maxValue = 3;
            gearSlider.wholeNumbers = true;
            gearSlider.interactable = true;
            
            gearSlider.onValueChanged.RemoveAllListeners();
            gearSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        if (currentGearText != null)
        {
            currentGearText.text = "-";
        }
    }

    public void SetTarget(CarController car)
    {
        targetCar = car;
        UpdateUI();
    }

    private void OnSliderValueChanged(float value)
    {
        if (targetCar == null) return;

        GearState selectedGear = (GearState)Mathf.RoundToInt(value);
        if (targetCar.currentGear != selectedGear)
        {
            targetCar.ChangeGear(selectedGear);
        }
    }

    private void Update()
    {
        if (targetCar == null) return;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (targetCar == null) return;

        int gearValue = (int)targetCar.currentGear;

        if (gearSlider != null)
        {
            gearSlider.SetValueWithoutNotify(gearValue);
        }

        if (currentGearText != null)
        {
            currentGearText.text = targetCar.currentGear.ToString();
        }
    }
}