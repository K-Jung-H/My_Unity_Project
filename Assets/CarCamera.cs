using UnityEngine;

public class CarCamera : MonoBehaviour
{
    [Header("Target & Offset")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 2.5f, -5f);

    [Header("Follow Settings")]
    public float smoothSpeed = 10f;
    public float rotationSpeed = 5f;

    [Header("Collision Settings")]
    public LayerMask collisionLayers;
    public float collisionBuffer = 0.5f;

    private Vector3 currentVelocity;

    private void LateUpdate()
    {
        if (target == null) return;

        HandleMovement();
        HandleRotation();
    }

    private void HandleMovement()
    {
        Vector3 desiredPosition = target.TransformPoint(offset);

        Vector3 correctedPosition = CheckCameraCollision(target.position, desiredPosition);

        transform.position = Vector3.SmoothDamp(transform.position, correctedPosition, ref currentVelocity, 1.0f / smoothSpeed);
    }

    private void HandleRotation()
    {
        Vector3 direction = target.position - transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction + Vector3.up * 0.5f);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private Vector3 CheckCameraCollision(Vector3 targetPos, Vector3 desiredPos)
    {
        RaycastHit hit;
        Vector3 dir = desiredPos - targetPos;
        float dist = dir.magnitude;

        if (Physics.SphereCast(targetPos, 0.2f, dir.normalized, out hit, dist, collisionLayers))
        {
            return hit.point + (hit.normal * collisionBuffer);
        }

        return desiredPos;
    }
}