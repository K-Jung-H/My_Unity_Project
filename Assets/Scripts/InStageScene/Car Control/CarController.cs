using UnityEngine;


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

    [Header("SmokeEffects")]
    public ParticleSystem[] SmokeParticles;

    [Header("FuelEffects")]
    public ParticleSystem FuelChargingParticle;


    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float currentFuel = 100f;

    public float idleConsumptionRate = 1.0f;
    public float driveConsumptionBase = 2.0f;
    public float speedConsumptionFactor = 0.05f;

    [Header("Car Specs")]
    public float motorForce = 5000f;
    public float brakeForce = 3000f;
    public float decelerationForce = 200f;
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
    public float minDriftExitSpeed = 5f;
    public float driftRearStiffness = 0.1f;
    public float driftSmoothFactor = 5f;
    public float driftRotationalBoost = 3.0f;
    [Range(0f, 1f)] public float driftPathControl = 0.8f;
    [Range(0f, 5f)] public float driftDragFactor = 0.5f;
    [Range(0.1f, 0.8f)] public float driftSteerThreshold = 0.3f;
    [Range(0.5f, 1f)] public float autoDriftThreshold = 0.9f;

    [Header("Gameplay Settings")]
    public LayerMask collisionLayerMask;
    public float collisionMinForce = 1000f;
    public float explosionEffectCoolTime = 0.5f;
    private float lastExplosionTime = -999f;

    public GearState currentGear { get; private set; } = GearState.P;
    public bool IsDrifting = false;
    
    private float currentSteerInput = 0f;
    private float targetSteerInput = 0f;
    private float accelInput;
    private float brakeInput;
    private Rigidbody carRigidbody;
    private float defaultAngularDamping;
    private float driveDirection = 1f;

    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = centerOfMassOffset;
        defaultAngularDamping = carRigidbody.angularDamping;
        carRigidbody.maxAngularVelocity = 20f;

        if(FuelChargingParticle != null)
            FuelChargingParticle.Stop();
    }

    private void Update()
    {
        ProcessSteeringSmoothing();
        CalculateFuelConsumption();
        UpdateExhaustParticles();
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

    public void SetInput(float steer, float accel, float brake, GearState gear)
    {
        targetSteerInput = steer;
        accelInput = accel;
        brakeInput = brake;
        currentGear = gear;
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

    private void CalculateFuelConsumption()
    {
        if (currentFuel <= 0) return;

        float consumption = 0f;

        float currentSpeed = carRigidbody.linearVelocity.magnitude;

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

    public void AddFuel(float amount)
    {
        currentFuel += amount;
        currentFuel = Mathf.Clamp(currentFuel, 0, maxFuel);
    }

    public void SetFuelCharging(bool isCharging)
    {
        if (FuelChargingParticle == null) return;

        if (isCharging)
        {
            if (!FuelChargingParticle.isPlaying)
                FuelChargingParticle.Play();
        }
        else
        {
            if (FuelChargingParticle.isPlaying)
                FuelChargingParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void UpdateExhaustParticles()
    {
        if (SmokeParticles == null || SmokeParticles.Length == 0) return;

        bool isEngineActive = (currentGear != GearState.P) && (currentFuel > 0);

        foreach (var ps in SmokeParticles)
        {
            var emission = ps.emission;
            emission.enabled = isEngineActive;
        }
    }

    private void UpdateDriveDirection()
    {
        float velocityDot = Vector3.Dot(carRigidbody.linearVelocity, transform.forward);
        float velocityMag = carRigidbody.linearVelocity.magnitude;

        float effectiveAccel = ValidateAccelInput(accelInput);

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
            if (effectiveAccel > 0.1f)
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
        float speed = carRigidbody.linearVelocity.magnitude;
        float effectiveAccel = ValidateAccelInput(accelInput);

        float currentSpeedThreshold = IsDrifting ? minDriftExitSpeed : minDriftSpeed;
        bool speedCondition = speed > currentSpeedThreshold;

        bool turnCondition = Mathf.Abs(currentSteerInput) > driftSteerThreshold;

        bool isSmartBraking = (Vector3.Dot(carRigidbody.linearVelocity, transform.forward) > 5.0f && currentGear == GearState.R && effectiveAccel > 0.1f);
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
        float effectiveAccel = ValidateAccelInput(accelInput);

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

            if (effectiveAccel > 0.1f)
            {
                if (currentGear == GearState.D)
                {
                    currentMotorForce = effectiveAccel * motorForce;
                }
                else if (currentGear == GearState.R)
                {
                    currentMotorForce = effectiveAccel * -motorForce;
                }
            }

            if (effectiveAccel < 0.1f && brakeInput < 0.1f)
            {
                currentBrakeForce = decelerationForce;
            }
        }

        if (IsDrifting)
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
        float effectiveAccel = ValidateAccelInput(accelInput);

        if (speed < 0.5f && (brakeInput > 0.1f || effectiveAccel < 0.1f))
        {
            carRigidbody.linearVelocity = Vector3.Lerp(carRigidbody.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
            carRigidbody.angularVelocity = Vector3.Lerp(carRigidbody.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 10f);
        }

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

        float currentDownForce = IsDrifting ? downForce * 0.2f : downForce;
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

        bool isBrakingWhileMoving = (brakeInput > 0.1f && speed > 0.5f);

        float targetRearStiffness;
        if (IsDrifting || isBrakingWhileMoving)
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

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastExplosionTime < explosionEffectCoolTime) return;

        if (collisionLayerMask == (collisionLayerMask | (1 << collision.gameObject.layer)))
        {
            if (collision.impulse.magnitude >= collisionMinForce)
            {
                lastExplosionTime = Time.time;
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.PlayEffect("Explosion", transform.position, transform.rotation);
                }
            }
        }
    }
}