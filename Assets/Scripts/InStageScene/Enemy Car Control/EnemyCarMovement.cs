using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarMovement : MonoBehaviour
{
    [Header("Current Stats")] 
    [SerializeField] private float accelerationForce;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float brakeForce;
    [SerializeField] private float steeringGrip;
    [SerializeField] private float stability;

    [Header("Physics Sensors")]
    [SerializeField] private float downForce = 50f;
    [SerializeField] private float groundCheckDist = 2.0f;
    [SerializeField] private float groundCheckOffset = 1.0f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private float inputThrottle;
    private float inputSteer;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;
    
    private float currentGroundDist;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void InitializeFromProfile(EnemyStatProfile profile)
    {
        if (profile == null)
        {
            Debug.LogError($"{gameObject.name}: EnemyStatProfile is missing!");
            return;
        }

        if (rb == null) rb = GetComponent<Rigidbody>();

        rb.mass = profile.Mass;
        rb.linearDamping = profile.LinearDamping;
        rb.angularDamping = profile.AngularDamping;
        rb.centerOfMass = new Vector3(0, profile.CenterOfMassY, 0);

        this.accelerationForce = profile.AccelerationForce;
        this.maxSpeed = profile.MaxSpeed;
        this.turnSpeed = profile.TurnSpeed;
        this.brakeForce = profile.BrakeForce;
        this.steeringGrip = profile.SteeringGrip;
        this.stability = profile.Stability;
    }

    private void FixedUpdate()
    {
        CheckGround();
        ApplySteering();
        KillLateralVelocity();
        ApplyEngineForce();
        AlignToGround();
    }

    public void SetInputs(float throttle, float steer)
    {
        inputThrottle = Mathf.Clamp(throttle, -1f, 1f);
        inputSteer = Mathf.Clamp(steer, -1f, 1f);
    }

    private void CheckGround()
    {
        Vector3 origin = transform.position + (Vector3.up * groundCheckOffset);
        
        float checkDistance = groundCheckDist + groundCheckOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance, groundLayer))
        {
            isGrounded = true;
            groundNormal = hit.normal;
            currentGroundDist = hit.distance - groundCheckOffset;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
            currentGroundDist = float.MaxValue;
        }
    }

    private void ApplySteering()
    {
        if (Mathf.Abs(inputSteer) > 0.01f)
        {
            float turnAmount = inputSteer * turnSpeed * Time.fixedDeltaTime;
            Quaternion turnRotation = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRotation);
        }
    }

    private void KillLateralVelocity()
    {
        if (!isGrounded) return;
        Vector3 localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
        localVelocity.x = Mathf.Lerp(localVelocity.x, 0f, Time.fixedDeltaTime * steeringGrip);
        rb.linearVelocity = transform.TransformDirection(localVelocity);
    }

    private void ApplyEngineForce()
    {
        if (isGrounded)
        {
            Vector3 forwardForceDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            
            if (Mathf.Abs(inputThrottle) > 0.01f)
            {
                rb.AddForce(forwardForceDir * inputThrottle * accelerationForce, ForceMode.Force);
            }
            else
            {
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * brakeForce * 0.5f);
            }

            if (currentGroundDist > 0.1f)
            {
                rb.AddForce(-groundNormal * downForce * rb.mass, ForceMode.Force);
            }
        }
        else
        {
            rb.AddForce(Vector3.down * 15f * rb.mass, ForceMode.Force);
        }
        
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private void AlignToGround()
    {
        if (isGrounded)
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * stability));
        }
        else
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 2f));
        }
    }
}