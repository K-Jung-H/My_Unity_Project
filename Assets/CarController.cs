using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Meshes")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    public float motorForce = 5000f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Arcade Settings")]
    [Range(0f, 1f)] public float steerHelper = 0.5f;
    public float downForce = 100f;
    [Range(0.1f, 3f)] public float wheelStiffness = 2.0f;

    [Header("UI Input Sources")]
    public SteeringWheelUI steeringWheelUI;
    public CarPedalUI accelPedalUI;
    public CarPedalUI brakePedalUI;

    private float steerInput;
    private float accelInput;
    private float brakeInput;
    private Rigidbody carRigidbody;

    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = centerOfMassOffset;
    }

    private void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        HandleMotor();
        HandleSteering();
        ApplyArcadePhysics();
        UpdateWheels();
        ApplyWheelFriction();
    }

    private void GetInput()
    {
        steerInput = 0f;
        accelInput = 0f;
        brakeInput = 0f;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steerInput = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steerInput = 1f;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) accelInput = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed) brakeInput = 1f;
        }

        if (steeringWheelUI != null && Mathf.Abs(steeringWheelUI.InputValue) > 0.01f)
        {
            steerInput = steeringWheelUI.InputValue;
        }

        if (accelPedalUI != null && accelPedalUI.InputValue > 0.01f)
        {
            accelInput = accelPedalUI.InputValue;
        }

        if (brakePedalUI != null && brakePedalUI.InputValue > 0.01f)
        {
            brakeInput = brakePedalUI.InputValue;
        }
    }

    private void HandleMotor()
    {
        float currentMotorForce = accelInput * motorForce;

        rearLeftCollider.motorTorque = currentMotorForce;
        rearRightCollider.motorTorque = currentMotorForce;

        float currentBrakeForce = brakeInput * brakeForce;

        frontLeftCollider.brakeTorque = currentBrakeForce;
        frontRightCollider.brakeTorque = currentBrakeForce;
        rearLeftCollider.brakeTorque = currentBrakeForce;
        rearRightCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        float currentSteerAngle = steerInput * maxSteerAngle;

        frontLeftCollider.steerAngle = currentSteerAngle;
        frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyArcadePhysics()
    {
        if (Mathf.Abs(steerInput) > 0.1f)
        {
            float rotationSpeed = carRigidbody.linearVelocity.magnitude * steerHelper;
            if (carRigidbody.linearVelocity.magnitude > 1f)
            {
                carRigidbody.AddTorque(transform.up * steerInput * rotationSpeed * 10f);
            }
        }

        carRigidbody.AddForce(-transform.up * downForce * carRigidbody.linearVelocity.magnitude);
    }

    private void UpdateWheels()
    {
        UpdateSingleWheel(frontLeftCollider, frontLeftMesh);
        UpdateSingleWheel(frontRightCollider, frontRightMesh);
        UpdateSingleWheel(rearLeftCollider, rearLeftMesh);
        UpdateSingleWheel(rearRightCollider, rearRightMesh);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    private void ApplyWheelFriction()
    {
        WheelFrictionCurve forwardFriction = frontLeftCollider.forwardFriction;
        WheelFrictionCurve sidewaysFriction = frontLeftCollider.sidewaysFriction;

        forwardFriction.stiffness = wheelStiffness;
        sidewaysFriction.stiffness = wheelStiffness;

        WheelCollider[] wheels = { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };

        foreach (var wheel in wheels)
        {
            wheel.forwardFriction = forwardFriction;
            wheel.sidewaysFriction = sidewaysFriction;
        }
    }
}