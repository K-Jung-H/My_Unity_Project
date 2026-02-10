using UnityEngine;

public class CarLightsController : MonoBehaviour
{
    [Header("Light Objects")]
    public GameObject[] headLights; 
    
    private void Start()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayNightChanged += HandleDayNightChange;
            
            HandleDayNightChange(GameTimeManager.Instance.IsNight);
        }
    }

    private void OnDestroy()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnDayNightChanged -= HandleDayNightChange;
        }
    }

    private void HandleDayNightChange(bool isNight)
    {
        ToggleLights(headLights, isNight);
    }

    private void ToggleLights(GameObject[] lights, bool isActive)
    {
        if (lights == null) return;
        foreach (var lightObj in lights)
        {
            if (lightObj != null) lightObj.SetActive(isActive);
        }
    }
}