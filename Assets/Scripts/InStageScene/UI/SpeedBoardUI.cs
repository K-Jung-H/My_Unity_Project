using UnityEngine;
using TMPro;

public class SpeedBoardUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Settings")]
    [SerializeField] private float speedMultiplier = 3.6f; 
    [SerializeField] private float displayScale = 1.0f; 

    private Rigidbody targetRigidbody;

    public void Initialize()
    {
        if (speedText != null)
        {
            speedText.text = "0 <size=70%>km/h</size>";
        }
    }

    public void SetTarget(CarController carController)
    {
        if (carController == null) return;
        targetRigidbody = carController.GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (targetRigidbody == null || speedText == null) return;

        Vector3 velocity = targetRigidbody.linearVelocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
        
        float rawSpeed = horizontalVelocity.magnitude;
        float calculatedSpeed = rawSpeed * speedMultiplier * displayScale;
        int finalSpeed = Mathf.RoundToInt(calculatedSpeed);

        speedText.text = $"{finalSpeed} <size=70%>km/h</size>";
    }
}