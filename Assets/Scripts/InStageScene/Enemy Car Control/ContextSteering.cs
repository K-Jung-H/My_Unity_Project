using UnityEngine;

public class ContextSteering : MonoBehaviour
{
    [Header("Sensor Settings")]
    [SerializeField] private int rayCount = 8;
    [SerializeField] private float rayRange = 10f;
    [SerializeField] private LayerMask avoidanceMask;
    [SerializeField] private bool showGizmos = true;

    private float[] interestMap;
    private float[] dangerMap;
    private Vector3[] directions; 

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
    }

    public Vector3 GetDirectionToMove(Vector3 targetPosition, Transform targetToIgnore = null)
    {
        Vector3 targetDirWorld = (targetPosition - transform.position).normalized;
        Vector3 targetDirLocal = transform.InverseTransformDirection(targetDirWorld);

        SetInterest(targetDirLocal);
        
        SetDanger(targetToIgnore); 

        Vector3 chosenDirLocal = Vector3.zero;
        for (int i = 0; i < rayCount; i++)
        {
            float value = Mathf.Clamp01(interestMap[i] - dangerMap[i]);
            chosenDirLocal += directions[i] * value;
        }

        return chosenDirLocal.normalized;
    }

    private void SetInterest(Vector3 targetDirLocal)
    {
        for (int i = 0; i < rayCount; i++)
        {
            float dot = Vector3.Dot(directions[i], targetDirLocal);
            interestMap[i] = Mathf.Max(0, dot);
        }
    }

    private void SetDanger(Transform targetToIgnore)
    {
        for (int i = 0; i < rayCount; i++)
        {
            dangerMap[i] = 0f;
            
            Vector3 rayDirWorld = transform.TransformDirection(directions[i]);
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, rayDirWorld);

            if (Physics.Raycast(ray, out RaycastHit hit, rayRange, avoidanceMask))
            {
                if (targetToIgnore != null && (hit.transform == targetToIgnore || hit.transform.root == targetToIgnore.root))
                {
                    continue; 
                }

                float weight = 1 - (hit.distance / rayRange);
                dangerMap[i] = weight;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || directions == null || interestMap == null || dangerMap == null) return;

        for (int i = 0; i < rayCount; i++)
        {
            Vector3 rayDirWorld = transform.TransformDirection(directions[i]);
            
            if (dangerMap[i] > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rayDirWorld * rayRange * dangerMap[i]);
            }
            
            if (interestMap[i] > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, rayDirWorld * 3f * interestMap[i]);
            }
        }
    }
}