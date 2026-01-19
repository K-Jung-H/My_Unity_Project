using UnityEngine;

public enum LightControlMode
{
    FollowCamera,    
    FixedLookAtRay   
}

[RequireComponent(typeof(Light))]
public class LobbyLightController : MonoBehaviour
{
    [Header("Mode Settings")]
    public LightControlMode currentMode = LightControlMode.FollowCamera;

    [Header("Target Settings")]
    public Camera targetCamera;

    [Header("Mode 1: Follow Settings")]
    public float positionLerpSpeed = 5f;
    public float rotationLerpSpeed = 5f;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    [Header("Mode 2: RayCast Settings")]
    public float rayDistance = 100f;
    public LayerMask hitLayerMask = ~0; 
    public float lookAtSpeed = 10f;
    public float defaultFocusDistance = 20f; 
    private Light targetLight;
    private bool isInitialized = false;
    private Vector3 initialPosition; 

    public void Initialize()
    {
        targetLight = GetComponent<Light>();

        if (targetLight.type != LightType.Spot)
        {
            Debug.LogWarning("[LobbyLightController] Spot Light 권장.");
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                Debug.LogError("[LobbyLightController] Target Camera Missing.");
                return;
            }
        }

        initialPosition = transform.position;

        if (currentMode == LightControlMode.FollowCamera)
        {
            transform.position = GetFollowPosition();
            transform.rotation = GetFollowRotation();
        }

        isInitialized = true;
        Debug.Log($"LobbyLightController Initialized. Mode: {currentMode}");
    }

    void LateUpdate()
    {
        if (!isInitialized || targetCamera == null) return;

        switch (currentMode)
        {
            case LightControlMode.FollowCamera:
                HandleFollowMode();
                break;
            case LightControlMode.FixedLookAtRay:
                HandleLookAtRayMode();
                break;
        }
    }

    private void HandleFollowMode()
    {
        Vector3 targetPos = GetFollowPosition();
        Quaternion targetRot = GetFollowRotation();

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * positionLerpSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
    }

    private void HandleLookAtRayMode()
    {
        
        Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        Vector3 lookTargetPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, hitLayerMask))
        {
            lookTargetPoint = hit.point;
        }
        else
        {
            lookTargetPoint = ray.GetPoint(defaultFocusDistance);
        }

        Vector3 directionToTarget = lookTargetPoint - transform.position;
        
        if (directionToTarget != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookAtSpeed);
        }
    }

    private Vector3 GetFollowPosition()
    {
        return targetCamera.transform.TransformPoint(positionOffset);
    }

    private Quaternion GetFollowRotation()
    {
        return targetCamera.transform.rotation * Quaternion.Euler(rotationOffset);
    }

    private void OnDrawGizmos()
    {
        if (targetCamera != null && currentMode == LightControlMode.FixedLookAtRay)
        {
            Gizmos.color = Color.yellow;
            Ray ray = targetCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.DrawRay(ray.origin, ray.direction * rayDistance);
        }
    }
}