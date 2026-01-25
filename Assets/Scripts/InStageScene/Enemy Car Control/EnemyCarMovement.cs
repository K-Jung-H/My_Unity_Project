using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyCarMovement : MonoBehaviour
{
    [Header("Drive Settings")]
    [SerializeField] private float accelerationForce = 50f;
    [SerializeField] private float maxSpeed = 30f;
    [SerializeField] private float turnSpeed = 200f; 
    [SerializeField] private float brakeForce = 10f;

    [Header("Handling")]
    [Tooltip("낮을수록 드리프트(미끄러짐), 높을수록 그립 주행")]
    [Range(0.1f, 5f)] [SerializeField] private float steeringGrip = 2.0f; 
    [SerializeField] private float stability = 5.0f;

    [Header("Physics Sensors")]
    [SerializeField] private float downForce = 100f;
    [SerializeField] private float groundCheckDist = 2.0f;
    [SerializeField] private float groundCheckOffset = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private float inputThrottle;
    private float inputSteer;
    private bool isGrounded;
    private Vector3 groundNormal = Vector3.up;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = 1500f;
        rb.linearDamping = 0.2f; 
        rb.angularDamping = 5f;
        rb.centerOfMass = new Vector3(0, -0.9f, 0); 
    }

    private void FixedUpdate()
    {
        CheckGround();
        
        ApplySteering();
        
        AlignVelocityToForward();
        
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
        if (Physics.Raycast(origin, -Vector3.up, out RaycastHit hit, groundCheckDist, groundLayer))
        {
            isGrounded = true;
            groundNormal = hit.normal;
        }
        else
        {
            isGrounded = false;
            groundNormal = Vector3.up;
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

    private void AlignVelocityToForward()
    {
        if (!isGrounded || rb.linearVelocity.magnitude < 0.1f) return;

        Vector3 newVelocity = Vector3.RotateTowards(
            rb.linearVelocity, 
            transform.forward, 
            steeringGrip * Time.fixedDeltaTime, 
            0f
        );

        rb.linearVelocity = newVelocity.normalized * rb.linearVelocity.magnitude;
    }

    private void ApplyEngineForce()
    {
        if (isGrounded)
        {
            Vector3 forwardForceDir = Vector3.ProjectOnPlane(transform.forward, groundNormal).normalized;
            
            if (inputThrottle != 0)
            {
                rb.AddForce(forwardForceDir * inputThrottle * accelerationForce, ForceMode.Acceleration);
            }
            else
            {
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * brakeForce);
            }

            rb.AddForce(-groundNormal * downForce, ForceMode.Acceleration);
        }
        else
        {
            rb.AddForce(Vector3.down * 20f, ForceMode.Acceleration);
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