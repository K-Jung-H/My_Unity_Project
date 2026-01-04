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

    [Header("UI Input Sources")]
    public SteeringWheelUI steeringWheelUI;
    public CarPedalUI accelPedalUI;
    public CarPedalUI brakePedalUI;
    public CarPedalUI reversePedalUI;

    [Header("Car Specs")]
    public float motorForce = 5000f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 40f;
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Input Sensitivity")]
    public float steerSensitivity = 3.0f;
    public float steerGravity = 5.0f;
    public float turnResponsiveness = 15.0f;

    [Header("Arcade Physics")]
    [Range(0f, 1f)] public float steerHelper = 0.5f;
    public float downForce = 1000f;
    [Range(0.1f, 3f)] public float wheelStiffness = 2.0f;
    public float maxAngularVelocity = 8.0f;

    [Header("Drift Configuration")]
    public float minDriftSpeed = 10f;
    public float driftRearStiffness = 0.1f;
    public float driftSmoothFactor = 5f;
    public float driftRotationalBoost = 3.0f;
    [Range(0f, 1f)] public float driftPathControl = 0.8f;
    [Range(0f, 5f)] public float driftDragFactor = 0.5f;

    private float currentSteerInput = 0f;
    private float accelInput;
    private float brakeInput;
    private float reverseInput;

    private Rigidbody carRigidbody;
    private bool isDrifting = false;
    private float defaultAngularDamping;
    private float driveDirection = 1f;

    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = centerOfMassOffset;
        defaultAngularDamping = carRigidbody.angularDamping;
        carRigidbody.maxAngularVelocity = 20f;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        UpdateDriveDirection();
        CheckDriftState();
        ApplyMotorForce();
        ApplySteering();
        ApplyArcadePhysics();
        UpdateWheelVisuals();
        ApplyWheelFriction();
    }

    private void HandleInput()
    {
        float targetKeyboardSteer = 0f;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) targetKeyboardSteer = -1f;
            else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) targetKeyboardSteer = 1f;
        }

        if (targetKeyboardSteer != 0)
        {
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, targetKeyboardSteer, steerSensitivity * Time.deltaTime);
        }
        else
        {
            if (steeringWheelUI == null || Mathf.Abs(steeringWheelUI.InputValue) < 0.01f)
            {
                currentSteerInput = Mathf.MoveTowards(currentSteerInput, 0f, steerGravity * Time.deltaTime);
            }
        }

        if (steeringWheelUI != null && Mathf.Abs(steeringWheelUI.InputValue) > 0.01f)
        {
            currentSteerInput = steeringWheelUI.InputValue;
        }

        accelInput = 0f;
        brakeInput = 0f;
        reverseInput = 0f;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) accelInput = 1f;
            if (keyboard.spaceKey.isPressed) brakeInput = 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) reverseInput = 1f;
        }

        if (accelPedalUI != null && accelPedalUI.InputValue > 0.01f) accelInput = accelPedalUI.InputValue;
        if (brakePedalUI != null && brakePedalUI.InputValue > 0.01f) brakeInput = brakePedalUI.InputValue;
        if (reversePedalUI != null && reversePedalUI.InputValue > 0.01f) reverseInput = reversePedalUI.InputValue;
    }

    private void UpdateDriveDirection()
    {
        float velocityDot = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
        float velocityMag = carRigidbody.linearVelocity.magnitude;

        if (velocityDot > 1.0f)
        {
            driveDirection = 1f;
        }
        else if (velocityDot < -1.0f)
        {
            driveDirection = -1f;
        }
        else
        {
            if (accelInput > 0.1f) driveDirection = 1f;
            else if (reverseInput > 0.1f) driveDirection = -1f;
            else
            {
                if (velocityMag < 0.1f) driveDirection = 1f;
                else driveDirection = velocityDot >= 0 ? 1f : -1f;
            }
        }
    }

    private void CheckDriftState()
    {
        float speed = carRigidbody.linearVelocity.magnitude;
        bool speedCondition = speed > minDriftSpeed;
        bool turnCondition = Mathf.Abs(currentSteerInput) > 0.1f;

        bool isSmartBraking = (Vector3.Dot(carRigidbody.linearVelocity, transform.forward) > 5.0f && reverseInput > 0.1f);
        bool brakeCondition = (brakeInput > 0.1f) || isSmartBraking;

        isDrifting = speedCondition && turnCondition && brakeCondition;

        if (isDrifting) carRigidbody.angularDamping = 0.05f;
        else carRigidbody.angularDamping = defaultAngularDamping;
    }

    private void ApplyMotorForce()
    {
        float velocityDot = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);

        bool isBrakingForward = (velocityDot > 1.0f && reverseInput > 0.1f);
        bool isBrakingReverse = (velocityDot < -1.0f && accelInput > 0.1f);

        float currentMotorForce = 0f;
        float currentBrakeForce = brakeInput * brakeForce;

        if (isBrakingForward || isBrakingReverse)
        {
            currentMotorForce = 0f;
            currentBrakeForce = brakeForce;
        }
        else
        {
            float moveInput = accelInput - reverseInput;
            currentMotorForce = moveInput * motorForce;
        }

        if (isDrifting)
        {
            frontLeftCollider.brakeTorque = 0f;
            frontRightCollider.brakeTorque = 0f;
            rearLeftCollider.brakeTorque = 0f;
            rearRightCollider.brakeTorque = 0f;
        }
        else
        {
            frontLeftCollider.brakeTorque = currentBrakeForce;
            frontRightCollider.brakeTorque = currentBrakeForce;
            rearLeftCollider.brakeTorque = currentBrakeForce;
            rearRightCollider.brakeTorque = currentBrakeForce;
        }

        rearLeftCollider.motorTorque = currentMotorForce;
        rearRightCollider.motorTorque = currentMotorForce;
    }

    private void ApplySteering()
    {
        float angle = currentSteerInput * maxSteerAngle;

        frontLeftCollider.steerAngle = angle;
        frontRightCollider.steerAngle = angle;
    }

    private void ApplyArcadePhysics()
    {
        float speed = carRigidbody.linearVelocity.magnitude;

        if (speed < 0.5f && (brakeInput > 0.1f || (accelInput < 0.1f && reverseInput < 0.1f)))
        {
            carRigidbody.linearVelocity = Vector3.Lerp(carRigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
            carRigidbody.angularVelocity = Vector3.Lerp(carRigidbody.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }

        bool hasThrottleInput = accelInput > 0.1f || reverseInput > 0.1f;
        bool isMoving = speed > 1.0f;
        bool canTurn = hasThrottleInput || isMoving;

        if (canTurn && Mathf.Abs(currentSteerInput) > 0.05f)
        {
            float direction = driveDirection;
            float driftMult = isDrifting ? driftRotationalBoost : 1.0f;
            float targetTurnSpeed = currentSteerInput * driftMult * steerHelper * 3.0f * direction;

            float lowSpeedDamping = Mathf.InverseLerp(0.5f, 5.0f, speed);
            targetTurnSpeed *= lowSpeedDamping;

            if (isDrifting)
            {
                float lateralSpeed = Vector3.Dot(carRigidbody.linearVelocity, transform.right);
                float driftControlFactor = Mathf.Clamp01(Mathf.Abs(lateralSpeed) / 2.0f);
                targetTurnSpeed *= driftControlFactor;
            }

            Vector3 currentAV = carRigidbody.angularVelocity;
            currentAV.y = Mathf.Lerp(currentAV.y, targetTurnSpeed, Time.fixedDeltaTime * turnResponsiveness);
            currentAV.y = Mathf.Clamp(currentAV.y, -maxAngularVelocity, maxAngularVelocity);

            carRigidbody.angularVelocity = currentAV;
        }
        else
        {
            Vector3 currentAV = carRigidbody.angularVelocity;
            currentAV.y = Mathf.Lerp(currentAV.y, 0f, Time.fixedDeltaTime * turnResponsiveness);
            carRigidbody.angularVelocity = currentAV;
        }

        if (isDrifting && speed > 2.0f)
        {
            float steerAngleRad = (currentSteerInput * maxSteerAngle) * Mathf.Deg2Rad;
            Vector3 steerDirection = Quaternion.Euler(0, currentSteerInput * maxSteerAngle, 0) * transform.forward;
            Vector3 targetVelocityDir = steerDirection.normalized;

            Vector3 currentDir = carRigidbody.linearVelocity.normalized;
            Vector3 newDir = Vector3.Lerp(currentDir, targetVelocityDir, Time.fixedDeltaTime * driftPathControl);
            carRigidbody.linearVelocity = newDir * speed;

            float slipAngle = Vector3.Angle(transform.forward, carRigidbody.linearVelocity);
            float slipFactor = Mathf.Clamp01(slipAngle / 90f);

            float dragMultiplier = 1f - (slipFactor * driftDragFactor * Time.fixedDeltaTime);
            dragMultiplier = Mathf.Clamp(dragMultiplier, 0.5f, 1f);

            carRigidbody.linearVelocity *= dragMultiplier;
        }

        float currentDownForce = isDrifting ? downForce * 0.2f : downForce;
        carRigidbody.AddForce(-transform.up * currentDownForce * speed);
    }

    private void UpdateWheelVisuals()
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

        frontLeftCollider.forwardFriction = forwardFriction;
        frontLeftCollider.sidewaysFriction = sidewaysFriction;
        frontRightCollider.forwardFriction = forwardFriction;
        frontRightCollider.sidewaysFriction = sidewaysFriction;

        float speed = carRigidbody.linearVelocity.magnitude;

        bool isSmartBraking = (Vector3.Dot(carRigidbody.linearVelocity, transform.forward) > 1.0f && reverseInput > 0.1f);
        bool isBrakingWhileMoving = (brakeInput > 0.1f && speed > 0.5f) || isSmartBraking;

        float targetRearStiffness;
        if (isDrifting || isBrakingWhileMoving)
        {
            targetRearStiffness = driftRearStiffness;
        }
        else
        {
            targetRearStiffness = wheelStiffness;
        }

        float currentRearStiffness = rearLeftCollider.sidewaysFriction.stiffness;
        float newRearStiffness = Mathf.Lerp(currentRearStiffness, targetRearStiffness, Time.fixedDeltaTime * driftSmoothFactor);

        sidewaysFriction.stiffness = newRearStiffness;

        rearLeftCollider.forwardFriction = forwardFriction;
        rearLeftCollider.sidewaysFriction = sidewaysFriction;
        rearRightCollider.forwardFriction = forwardFriction;
        rearRightCollider.sidewaysFriction = sidewaysFriction;
    }
}