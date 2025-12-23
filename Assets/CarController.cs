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
    public float motorForce = 1500f;
    public float brakeForce = 3000f;
    public float maxSteerAngle = 30f;
    public Vector3 centerOfMassOffset = new Vector3(0, -0.5f, 0);

    [Header("Arcade Settings (�ٽ�)")]
    [Range(0f, 1f)] public float steerHelper = 0.5f; // 0: ������, 1: ���� ������(��� ȸ��)
    public float downForce = 100f; // ���ӿ��� ���� �ٴڿ� ���̴� ��
    [Range(0.1f, 3f)] public float wheelStiffness = 2.0f; // Ÿ�̾� �׸���

    private float horizontalInput;
    private float verticalInput;
    private bool isBraking;
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
        ApplyArcadePhysics(); // �����̵� ���� �Լ�
        UpdateWheels();
        ApplyWheelFriction();
    }

    private void GetInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // �ε巯�� �Ƴ��α� �Է� ��� �ﰢ���� ������ ���ϸ� ReadValue ��� ����
        // ���⼭�� ���� ���� ����
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput = -1f;
        else if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput = 1f;
        else horizontalInput = 0f;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalInput = -1f;
        else if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalInput = 1f;
        else verticalInput = 0f;

        isBraking = keyboard.spaceKey.isPressed;
    }

    private void HandleMotor()
    {
        float currentMotorForce = verticalInput * motorForce;

        rearLeftCollider.motorTorque = currentMotorForce;
        rearRightCollider.motorTorque = currentMotorForce;

        float currentBrakeForce = isBraking ? brakeForce : 0f;

        frontLeftCollider.brakeTorque = currentBrakeForce;
        frontRightCollider.brakeTorque = currentBrakeForce;
        rearLeftCollider.brakeTorque = currentBrakeForce;
        rearRightCollider.brakeTorque = currentBrakeForce;
    }

    private void HandleSteering()
    {
        float currentSteerAngle = horizontalInput * maxSteerAngle;

        frontLeftCollider.steerAngle = currentSteerAngle;
        frontRightCollider.steerAngle = currentSteerAngle;
    }

    // �ڡڡ� �����̵� ���� ���� �Լ� �ڡڡ�
    private void ApplyArcadePhysics()
    {
        // 1. Steer Helper: ������ ���ϴ� �������� ������ ��ü�� ȸ����Ŵ
        // WheelCollider�� ���������� �̲���������, �� �ڵ�� ������ ���� ��������
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            float rotationSpeed = carRigidbody.linearVelocity.magnitude * steerHelper;
            // ���ڸ� ȸ�� ���� (�ӵ��� ���� ���� ȸ��)
            if (carRigidbody.linearVelocity.magnitude > 1f)
            {
                carRigidbody.AddTorque(transform.up * horizontalInput * rotationSpeed * 10f);
            }
        }

        // 2. Downforce: �ӵ��� �������� �ٴ����� �� ������ (������ ���)
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
        sidewaysFriction.stiffness = wheelStiffness; // ���� �׸� ��ȭ

        WheelCollider[] wheels = { frontLeftCollider, frontRightCollider, rearLeftCollider, rearRightCollider };

        foreach (var wheel in wheels)
        {
            wheel.forwardFriction = forwardFriction;
            wheel.sidewaysFriction = sidewaysFriction;
        }
    }
}