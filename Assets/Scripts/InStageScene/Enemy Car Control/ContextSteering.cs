using UnityEngine;

public class ContextSteering : MonoBehaviour
{
    [Header("Sensor Settings")]
    [SerializeField] private int rayCount = 8;
    [SerializeField] private float rayRange = 10f;
    [SerializeField] private float brakeRayRangeMultiplier = 1.5f;
    [SerializeField] private LayerMask avoidanceMask; 
    [SerializeField] private bool showGizmos = true;
    
    [SerializeField] private int updateInterval = 3; 

    private float[] interestMap;
    private float[] dangerMap;
    private Vector3[] directions;
    
    private Vector3 cachedDirection;
    private float cachedSafetyFactor;
    private int frameOffset;

    private void Awake()
    {
        interestMap = new float[rayCount];
        dangerMap = new float[rayCount];
        directions = new Vector3[rayCount];

        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * 2 * Mathf.PI / rayCount;
            directions[i] = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)).normalized;
        }
        
        frameOffset = Random.Range(0, updateInterval);
    }

    public Vector3 GetDirectionToMove(Vector3 targetPosition, Transform carTransform)
    {
        if ((Time.frameCount + frameOffset) % updateInterval == 0)
        {
            PerformContextSteering(targetPosition, carTransform);
        }
        return cachedDirection;
    }

    public float GetSafetyFactor(Vector3 chosenDirLocal, Transform carTransform)
    {
        if ((Time.frameCount + frameOffset) % updateInterval == 0)
        {
            PerformSafetyCheck(chosenDirLocal, carTransform);
        }
        return cachedSafetyFactor;
    }

    private void PerformContextSteering(Vector3 targetPosition, Transform carTransform)
    {
        Vector3 targetDirWorld = (targetPosition - transform.position).normalized;
        Vector3 targetDirLocal = carTransform.InverseTransformDirection(targetDirWorld);

        SetInterest(targetDirLocal);
        SetDanger(carTransform);

        Vector3 chosenDirLocal = Vector3.zero;
        for (int i = 0; i < rayCount; i++)
        {
            float value = Mathf.Clamp01(interestMap[i] - dangerMap[i]);
            chosenDirLocal += directions[i] * value;
        }
        
        cachedDirection = chosenDirLocal.normalized;
    }

    private void PerformSafetyCheck(Vector3 chosenDirLocal, Transform carTransform)
    {
        float obstacleFactor = 1f;
        float checkDist = rayRange * brakeRayRangeMultiplier;
        
        if (Physics.Raycast(transform.position, carTransform.forward, out RaycastHit hit, checkDist, avoidanceMask))
        {
            obstacleFactor = Mathf.Clamp01(hit.distance / checkDist);
            obstacleFactor = Mathf.Pow(obstacleFactor, 2);
        }
        
        float turnFactor = Vector3.Dot(Vector3.forward, chosenDirLocal);
        turnFactor = Mathf.Clamp01((turnFactor + 1f) * 0.5f);
        
        cachedSafetyFactor = Mathf.Min(obstacleFactor, turnFactor);
    }

    private void SetInterest(Vector3 targetDirLocal)
    {
        for (int i = 0; i < rayCount; i++)
        {
            float dot = Vector3.Dot(directions[i], targetDirLocal);
            interestMap[i] = Mathf.Clamp01((dot + 1) * 0.5f);
        }
    }

    private void SetDanger(Transform carTransform)
    {
        Vector3 origin = transform.position;
        for (int i = 0; i < rayCount; i++)
        {
            dangerMap[i] = 0f;
            Vector3 worldDir = carTransform.TransformDirection(directions[i]);
            if (Physics.Raycast(origin, worldDir, out RaycastHit hit, rayRange, avoidanceMask))
            {
                float strength = 1f - (hit.distance / rayRange);
                dangerMap[i] = strength;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * 2 * Mathf.PI / rayCount;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            Gizmos.DrawRay(transform.position, transform.TransformDirection(dir) * rayRange);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.forward * (rayRange * brakeRayRangeMultiplier));
    }
}