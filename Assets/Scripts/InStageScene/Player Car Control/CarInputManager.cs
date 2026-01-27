using UnityEngine;
using UnityEngine.InputSystem;

public class CarInputManager : MonoBehaviour
{
    public CarController targetCar;

    [Header("UI Input Sources")]
    [SerializeField] public SteeringWheelUI steeringWheelUI;
    [SerializeField] public HoldPressInput accelPedalUI;
    [SerializeField] public HoldPressInput brakePedalUI;

    private void Update()
    {
        if (targetCar == null) return;

        float steer = 0f;
        float accel = 0f;
        float brake = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steer = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steer = 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) accel = 1f;
            if (keyboard.spaceKey.isPressed) brake = 1f;

            if (keyboard.digit0Key.wasPressedThisFrame) targetCar.ChangeGear(GearState.P);
            else if (keyboard.digit1Key.wasPressedThisFrame) targetCar.ChangeGear(GearState.R);
            else if (keyboard.digit2Key.wasPressedThisFrame) targetCar.ChangeGear(GearState.N);
            else if (keyboard.digit3Key.wasPressedThisFrame) targetCar.ChangeGear(GearState.D);
        }

        if (steeringWheelUI != null && Mathf.Abs(steeringWheelUI.InputValue) > 0.01f)
        {
            steer = steeringWheelUI.InputValue;
        }

        if (accelPedalUI != null && accelPedalUI.CurrentValue > 0.01f) accel = accelPedalUI.CurrentValue;
        if (brakePedalUI != null && brakePedalUI.CurrentValue > 0.01f) brake = brakePedalUI.CurrentValue;

        targetCar.SetInputs(steer, accel, brake);
    }
}