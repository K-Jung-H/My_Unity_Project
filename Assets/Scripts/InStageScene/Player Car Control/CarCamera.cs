using UnityEngine;

public class CarCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    
    [Header("View Settings")]
    public Vector3 defaultOffset = new Vector3(0, 2.5f, -5f);
    public Vector3 topDownOffset = new Vector3(0, 6f, -1f); 
    public Vector3 targetLookAtOffset = new Vector3(0, 0.5f, 0);

    [Header("Motion Settings")]
    public float smoothSpeed = 10f;
    public float rotationSpeed = 5f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        HandleCameraBehavior();
    }

    private void HandleCameraBehavior()
    {
        Quaternion targetRotation = Quaternion.Euler(0, target.eulerAngles.y, 0);

        Vector3 standardPos = target.position + targetRotation * defaultOffset;
        
        Vector3 direction = standardPos - target.position;
        float maxDistance = direction.magnitude;

        Vector3 finalTargetPosition = standardPos;

        if (Physics.SphereCast(target.position, collisionRadius, direction.normalized, out RaycastHit hit, maxDistance, collisionLayers))
        {
            float hitDistance = hit.distance;
            
            float ratio = Mathf.Clamp01(hitDistance / maxDistance);

            Vector3 blendedOffset = Vector3.Lerp(topDownOffset, defaultOffset, ratio);

            finalTargetPosition = target.position + targetRotation * blendedOffset;
        }

        transform.position = Vector3.SmoothDamp(transform.position, finalTargetPosition, ref currentVelocity, 1.0f / smoothSpeed);

        HandleRotation();
    }

    private void HandleRotation()
    {
        Vector3 lookTarget = target.position + targetLookAtOffset;
        Vector3 direction = lookTarget - transform.position;

        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Vector3 defaultPos = target.position + Quaternion.Euler(0, target.eulerAngles.y, 0) * defaultOffset;
            Gizmos.DrawWireSphere(defaultPos, collisionRadius);

            Gizmos.color = Color.yellow;
            Vector3 topPos = target.position + Quaternion.Euler(0, target.eulerAngles.y, 0) * topDownOffset;
            Gizmos.DrawWireSphere(topPos, collisionRadius);
            Gizmos.DrawLine(defaultPos, topPos);
        }
    }
}