using UnityEngine;
using TMPro;

public class SpeedBoardUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI speedText;

    private CarController targetCar;

    public void Initialize()
    {
        if (speedText != null)
        {
            speedText.text = "0 <size=70%>km/h</size>";
        }
    }

    public void SetTarget(CarController carController)
    {
        targetCar = carController;
    }

    private void Update()
    {
        if (targetCar == null || speedText == null) return;

        int finalSpeed = Mathf.RoundToInt(targetCar.CurrentSpeed);

        speedText.text = $"{finalSpeed} <size=70%>km/h</size>";
    }
}