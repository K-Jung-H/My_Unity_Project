using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

public enum GearState
{
    P = 0,
    R = 1,
    N = 2,
    D = 3
}

[RequireComponent(typeof(CarInputManager))]
[RequireComponent(typeof(CarCameraManager))]
[RequireComponent(typeof(HealthSystem))]
[RequireComponent(typeof(CarCollisionHandler))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CarEffectController))]
public class CarController : MonoBehaviour
{
    public event Action OnDeath;

    [Header("Dependencies")]
    [SerializeField] private CarInputManager inputManager;
    [SerializeField] private CarCameraManager cameraManager;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private CarCollisionHandler collisionHandler;
    
    private CarEffectController effectController;

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

    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;
    public float idleConsumptionRate = 1.0f;
    public float driveConsumptionBase = 2.0f;
    public float speedConsumptionFactor = 0.05f;

    [Header("Car Specs")]
    public float motorForce = 8000f;
    public float brakeForce = 3000f;
    public float decelerationForce = 1000f;
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

    [Header("Speedometer Settings")]
    [SerializeField] private float speedMultiplier = 1.2f;

    [Header("Slope Settings")]
    public float slopeForce = 5000f;
    public float slopeThreshold = 0.1f;

    [Header("Drift Configuration")]
    public float minDriftSpeed = 10f;
    public float minDriftExitSpeed = 5f;
    public float driftRearStiffness = 0.1f;
    public float driftSmoothFactor = 5f;
    public float driftRotationalBoost = 3.0f;
    [Range(0f, 1f)] public float driftPathControl = 0.8f;
    [Range(0f, 5f)] public float driftDragFactor = 0.5f;
    [Range(0.1f, 0.8f)] public float driftSteerThreshold = 0.3f;
    [Range(0.5f, 1f)] public float autoDriftThreshold = 0.9f;

    [Header("Interaction Settings")]
    public LayerMask structureNatureLayer;
    [Range(0f, 1f)] public float slowSpeedFactor = 0.5f;
    public float slowDragAmount = 1.5f;

    public float CurrentSpeed { get; private set; }
    public GearState currentGear { get; private set; } = GearState.P;
    public bool IsDrifting { get; private set; } = false;
    public bool IsSkidding { get; private set; } = false;

    private Rigidbody carRigidbody;
    private float currentSteerInput = 0f;
    private float targetSteerInput = 0f;
    private float accelInput;
    private float brakeInput;
    private float defaultAngularDamping;
    private float driveDirection = 1f;
    private bool isReverseBraking = false;
    private bool isDead = false;
    private bool isSlowed = false;

    private Vector3 _cachedVelocity;
    private float _cachedSpeed;
    private float cachedEffectiveAccel;

    public bool IsGrounded
    {
        get
        {
            return frontLeftCollider.isGrounded || frontRightCollider.isGrounded || 
                   rearLeftCollider.isGrounded || rearRightCollider.isGrounded;
        }
    }

    private void Awake()
    {
        carRigidbody = GetComponent<Rigidbody>();
        effectController = GetComponent<CarEffectController>();
        
        if (inputManager == null) inputManager = GetComponent<CarInputManager>();
        if (cameraManager == null) cameraManager = GetComponent<CarCameraManager>();
        if (healthSystem == null) healthSystem = GetComponent<HealthSystem>();
        if (collisionHandler == null) collisionHandler = GetComponent<CarCollisionHandler>();
    }

    private void Start()
    {
        carRigidbody.centerOfMass = centerOfMassOffset;
        defaultAngularDamping = carRigidbody.angularDamping;
        carRigidbody.maxAngularVelocity = 20f;

        if (healthSystem != null)
        {
            healthSystem.OnDeath += HandlePlayerDeath;
        }
    }

    private void Update()
    {
        if (isDead) return;

        ProcessSteeringSmoothing();
        CalculateDisplaySpeed();
        CalculateFuelConsumption();
    }

    private void FixedUpdate()
    {
        _cachedVelocity = carRigidbody.linearVelocity;
        _cachedSpeed = _cachedVelocity.magnitude;

        cachedEffectiveAccel = ValidateAccelInput(accelInput);

        UpdateDriveDirection();
        CheckDriftState();

        ApplyMotorForce();
        ApplySteering();
        ApplySlopeAssist();

        IsSkidding = IsDrifting || isReverseBraking;

        if (IsGrounded)
        {
            ApplyArcadePhysics();
            
            if (isSlowed)
            {
                carRigidbody.linearDamping = slowDragAmount;
            }
            else if (!IsDrifting) 
            {
                carRigidbody.linearDamping = 0.05f;
            }
        }
        else
        {
            ApplyAirPhysics();
        }

        UpdateWheelVisuals();
        ApplyWheelFriction();
    }

    private void OnDestroy()
    {
        if (healthSystem != null)
        {
            healthSystem.OnDeath -= HandlePlayerDeath;
        }
    }

    public void SetInputs(float steer, float accel, float brake)
    {
        if (isDead)
        {
            targetSteerInput = 0f;
            accelInput = 0f;
            brakeInput = 1f; 
            return; 
        }

        targetSteerInput = steer;
        accelInput = accel;
        brakeInput = brake;
    }

    public void ChangeGear(GearState newGear)
    {
        if (currentGear == newGear) return;
        currentGear = newGear;
    }

    public void AddFuel(float amount)
    {
        currentFuel += amount;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
    }

    public void SetFuelCharging(bool isCharging)
    {
        if (effectController != null)
        {
            effectController.SetFuelCharging(isCharging);
        }
    }

    public void Revive(float fuelRatio, Transform respawnPoint)    
    {
        isDead = false;
        this.enabled = true;
        
        if (fuelRatio >= 0f)
        {
            currentFuel = maxFuel * fuelRatio;
        }
        
        if (respawnPoint != null)
        {
            transform.position = respawnPoint.position;
            transform.rotation = respawnPoint.rotation;
        }

        if (carRigidbody != null)
        {
            carRigidbody.linearVelocity = Vector3.zero; 
            carRigidbody.angularVelocity = Vector3.zero;
        }

        if (effectController != null)
        {
            effectController.SetFuelCharging(false); 
        }
        
        accelInput = 0f;
        brakeInput = 0f;
        targetSteerInput = 0f;
        currentSteerInput = 0f;
    }

    public float ValidateAccelInput(float input)
    {
        if (currentFuel <= 0)
        {
            if (currentGear == GearState.D || currentGear == GearState.R)
            {
                return 0f;
            }
        }
        return input;
    }

    private void HandlePlayerDeath()
    {
        if (isDead) return;
        
        isDead = true;
        targetSteerInput = 0f;
        accelInput = 0f;
        brakeInput = 1f;
        currentGear = GearState.N;

        OnDeath?.Invoke();
    }

    private void ProcessSteeringSmoothing()
    {
        if (Mathf.Abs(targetSteerInput) > 0.01f)
        {
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, targetSteerInput, steerSensitivity * Time.deltaTime);
        }
        else
        {
            currentSteerInput = Mathf.MoveTowards(currentSteerInput, 0f, steerGravity * Time.deltaTime);
        }
    }

    private void CalculateDisplaySpeed()
    {
        if (carRigidbody != null)
        {
            Vector3 horizontalVelocity = new Vector3(carRigidbody.linearVelocity.x, 0, carRigidbody.linearVelocity.z);
            CurrentSpeed = horizontalVelocity.magnitude * speedMultiplier;
        }
    }

    private void CalculateFuelConsumption()
    {
        if (currentFuel <= 0) return;

        float currentSpeed = carRigidbody.linearVelocity.magnitude;

        float consumption = 0f;
        switch (currentGear)
        {
            case GearState.P:
            case GearState.N:
                consumption = idleConsumptionRate;
                break;
            case GearState.D:
            case GearState.R:
                consumption = driveConsumptionBase + (currentSpeed * speedConsumptionFactor);
                break;
        }

        currentFuel -= consumption * Time.deltaTime;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
    }

    private void UpdateDriveDirection()
    {
        float velocityDot = Vector3.Dot(_cachedVelocity, transform.forward);
        float velocityMag = _cachedSpeed;
        
        if (velocityDot > 1.0f) driveDirection = 1f;
        else if (velocityDot < -1.0f) driveDirection = -1f;
        else
        {
            if (cachedEffectiveAccel > 0.1f)
            {
                if (currentGear == GearState.R) driveDirection = -1f;
                else driveDirection = 1f;
            }
            else
            {
                if (velocityMag < 0.1f) driveDirection = 1f;
                else driveDirection = velocityDot >= 0 ? 1f : -1f;
            }
        }
    }

    private void CheckDriftState()
    {
        if (!IsGrounded)
        {
            IsDrifting = false;
            carRigidbody.angularDamping = defaultAngularDamping;
            return;
        }

        float speed = _cachedSpeed;
        
        float currentSpeedThreshold = IsDrifting ? minDriftExitSpeed : minDriftSpeed;
        bool speedCondition = speed > currentSpeedThreshold;
        bool turnCondition = Mathf.Abs(currentSteerInput) > driftSteerThreshold;
        
        bool isSmartBraking = (Vector3.Dot(_cachedVelocity, transform.forward) > 5.0f && currentGear == GearState.R && cachedEffectiveAccel > 0.1f);
        bool brakeCondition = (brakeInput > 0.1f) || isSmartBraking;
        bool autoDriftCondition = Mathf.Abs(currentSteerInput) > autoDriftThreshold;

        IsDrifting = speedCondition && turnCondition && (brakeCondition || autoDriftCondition);

        if (IsDrifting) carRigidbody.angularDamping = 0.05f;
        else carRigidbody.angularDamping = defaultAngularDamping;
    }

    private void ApplyMotorForce()
    {
        float currentMotorForce = 0f;
        float currentBrakeForce = 0f;
        
        float forwardSpeed = Vector3.Dot(transform.forward, _cachedVelocity);

        float currentMaxMotorForce = isSlowed ? motorForce * slowSpeedFactor : motorForce;

        isReverseBraking = false;

        if (currentGear == GearState.P)
        {
            currentMotorForce = 0f;
            currentBrakeForce = brakeForce * 100f;
        }
        else if (currentGear == GearState.N)
        {
            currentMotorForce = 0f;
            currentBrakeForce = brakeInput * brakeForce;
        }
        else
        {
            currentBrakeForce = brakeInput * brakeForce;

            float gearDirection = 0f;
            if (currentGear == GearState.D) gearDirection = 1f;
            else if (currentGear == GearState.R) gearDirection = -1f;

            if (cachedEffectiveAccel > 0.1f)
            {
                if (forwardSpeed * gearDirection < -1.0f)
                {
                    isReverseBraking = true; 
                    currentMotorForce = 0f;
                    currentBrakeForce = brakeForce * 5.0f; 
                }
                else
                {
                    currentMotorForce = cachedEffectiveAccel * currentMaxMotorForce * gearDirection;
                    if (brakeInput < 0.1f) currentBrakeForce = 0f;
                }
            }

            if (cachedEffectiveAccel < 0.1f && brakeInput < 0.1f)
            {
                currentBrakeForce = decelerationForce;
            }
        }

        if (IsDrifting && !isReverseBraking)
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

    private void ApplySlopeAssist()
    {
        if (!IsGrounded) return;

        float slopeDot = Vector3.Dot(transform.forward, Vector3.up);
        
        if (slopeDot > slopeThreshold && cachedEffectiveAccel > 0.1f && currentGear != GearState.R)
        {
            Vector3 assistForce = transform.forward * slopeForce * slopeDot * cachedEffectiveAccel;
            carRigidbody.AddForce(assistForce, ForceMode.Acceleration);
        }
    }

    private void ApplyArcadePhysics()
    {
        float speed = _cachedSpeed;

        ApplyLowSpeedStop(speed, cachedEffectiveAccel);
        ApplyTurnPhysics(speed, cachedEffectiveAccel);
        ApplyDriftPhysics(speed);
        ApplyDownForce(speed);
    }

    private void ApplyLowSpeedStop(float speed, float effectiveAccel)
    {
        if (speed < 0.5f && (brakeInput > 0.1f || effectiveAccel < 0.1f))
        {
            carRigidbody.linearVelocity = Vector3.Lerp(carRigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
            carRigidbody.angularVelocity = Vector3.Lerp(carRigidbody.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }
    }

    private void ApplyTurnPhysics(float speed, float effectiveAccel)
    {
        bool hasThrottleInput = effectiveAccel > 0.1f;
        bool isMoving = speed > 1.0f;
        bool canTurn = hasThrottleInput || isMoving;

        if (canTurn && Mathf.Abs(currentSteerInput) > 0.05f)
        {
            float direction = driveDirection;
            float driftMult = IsDrifting ? driftRotationalBoost : 1.0f;
            float targetTurnSpeed = currentSteerInput * driftMult * steerHelper * 3.0f * direction;

            float lowSpeedDamping = Mathf.InverseLerp(0.5f, 5.0f, speed);
            targetTurnSpeed *= lowSpeedDamping;

            if (IsDrifting)
            {
                float lateralSpeed = Vector3.Dot(_cachedVelocity, transform.right);
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
    }

    private void ApplyDriftPhysics(float speed)
    {
        if (IsDrifting && speed > 2.0f)
        {
            Vector3 steerDirection = Quaternion.Euler(0, currentSteerInput * maxSteerAngle, 0) * transform.forward;
            Vector3 targetVelocityDir = steerDirection.normalized;

            Vector3 currentDir = carRigidbody.linearVelocity.normalized;
            Vector3 newDir = Vector3.Lerp(currentDir, targetVelocityDir, Time.fixedDeltaTime * driftPathControl);

            float adjustedSpeed = speed;
            if (brakeInput > 0.1f)
            {
                float brakeDecel = 1f - (brakeInput * 1.5f * Time.fixedDeltaTime);
                adjustedSpeed *= brakeDecel;
            }

            carRigidbody.linearVelocity = newDir * adjustedSpeed;

            float slipAngle = Vector3.Angle(transform.forward, carRigidbody.linearVelocity);
            float slipFactor = Mathf.Clamp01(slipAngle / 90f);

            float dragMultiplier = 1f - (slipFactor * driftDragFactor * Time.fixedDeltaTime);
            dragMultiplier = Mathf.Clamp(dragMultiplier, 0.5f, 1f);

            carRigidbody.linearVelocity *= dragMultiplier;
        }
    }

    private void ApplyDownForce(float speed)
    {
        float uprightDot = Vector3.Dot(transform.up, Vector3.up);
        Vector3 downForceDir;

        if (uprightDot > 0.0f) downForceDir = -transform.up; 
        else downForceDir = Vector3.down; 

        float currentDownForce = IsDrifting ? downForce * 0.2f : downForce;
        carRigidbody.AddForce(downForceDir * currentDownForce * speed);
    }

    private void ApplyAirPhysics()
    {
        carRigidbody.linearDamping = 0.05f; 
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;
        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;
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

        if (isReverseBraking)
        {
            forwardFriction.stiffness = wheelStiffness * 2.0f; 
        }

        frontLeftCollider.forwardFriction = forwardFriction;
        frontLeftCollider.sidewaysFriction = sidewaysFriction;
        frontRightCollider.forwardFriction = forwardFriction;
        frontRightCollider.sidewaysFriction = sidewaysFriction;

        float speed = _cachedSpeed;
        bool isBrakingWhileMoving = (brakeInput > 0.1f && speed > 0.5f);
        float targetRearStiffness = (IsDrifting || isBrakingWhileMoving) ? driftRearStiffness : wheelStiffness;

        float currentRearStiffness = rearLeftCollider.sidewaysFriction.stiffness;
        float newRearStiffness = Mathf.Lerp(currentRearStiffness, targetRearStiffness, Time.fixedDeltaTime * driftSmoothFactor);
        sidewaysFriction.stiffness = newRearStiffness;

        rearLeftCollider.forwardFriction = forwardFriction;
        rearLeftCollider.sidewaysFriction = sidewaysFriction;
        rearRightCollider.forwardFriction = forwardFriction;
        rearRightCollider.sidewaysFriction = sidewaysFriction;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & structureNatureLayer) != 0)
        {
            isSlowed = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & structureNatureLayer) != 0)
        {
            isSlowed = false;
        }
    }
}