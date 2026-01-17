using UnityEngine;
using UnityEngine.InputSystem;

public class CarInputManager : MonoBehaviour
{
    public CarController targetCar;

    [Header("UI Input Sources")]
    public SteeringWheelUI steeringWheelUI;
    public GearBoxUI gearBoxUI;
    public HoldPressInput accelPedalUI;
    public HoldPressInput brakePedalUI;

    private void Update()
    {
        if (targetCar == null) return;

        float steer = 0f;
        float accel = 0f;
        float brake = 0f;

        GearState gear = GearState.D;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steer = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steer = 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) accel = 1f;
            if (keyboard.spaceKey.isPressed) brake = 1f;

            if (gearBoxUI != null && gearBoxUI.gearSlider != null)
            {
                if (keyboard.digit0Key.wasPressedThisFrame) gearBoxUI.gearSlider.value = 0;
                else if (keyboard.digit1Key.wasPressedThisFrame) gearBoxUI.gearSlider.value = 1;
                else if (keyboard.digit2Key.wasPressedThisFrame) gearBoxUI.gearSlider.value = 2;
                else if (keyboard.digit3Key.wasPressedThisFrame) gearBoxUI.gearSlider.value = 3;
            }
        }
        

        if (steeringWheelUI != null && Mathf.Abs(steeringWheelUI.InputValue) > 0.01f)
        {
            steer = steeringWheelUI.InputValue;
        }

        if (accelPedalUI != null && accelPedalUI.CurrentValue > 0.01f) accel = accelPedalUI.CurrentValue;
        if (brakePedalUI != null && brakePedalUI.CurrentValue > 0.01f) brake = brakePedalUI.CurrentValue;

        if (gearBoxUI != null)
        {
            gear = gearBoxUI.CurrentGear;
        }

        targetCar.SetInput(steer, accel, brake, gear);
    }
}