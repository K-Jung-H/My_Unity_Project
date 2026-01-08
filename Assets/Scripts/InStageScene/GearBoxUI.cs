using UnityEngine;
using UnityEngine.UI;

public enum GearState
{
    P = 0,
    R = 1,
    N = 2,
    D = 3
}

public class GearBoxUI : MonoBehaviour
{
    [Header("UI Components")]
    public Slider gearSlider;
    public Text currentGearText;

    [Header("Settings")]
    private GearState currentGear;

    public GearState CurrentGear => currentGear;

    private void Start()
    {
        if (gearSlider != null)
        {
            gearSlider.minValue = 0;
            gearSlider.maxValue = 3; 
            gearSlider.wholeNumbers = true;
            
            gearSlider.onValueChanged.AddListener(OnSliderValueChanged);
            
            OnSliderValueChanged(gearSlider.value);
        }
    }

    private void OnSliderValueChanged(float value)
    {
        int intValue = Mathf.RoundToInt(value);
        currentGear = (GearState)intValue;

        if (currentGearText != null)
        {
            currentGearText.text = currentGear.ToString();
        }
    }
}