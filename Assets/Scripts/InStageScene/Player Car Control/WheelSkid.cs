using UnityEngine;

[RequireComponent(typeof(WheelCollider))]
public class WheelSkid : MonoBehaviour
{
    [Header("Settings")]
    public TrailRenderer skidTrailPrefab;
    
    [Header("Values")]
    public float groundOffset = 0.02f;

    private WheelCollider targetWheel;
    private TrailRenderer currentTrail;
    
    private bool isSkiddingSignal = false; 

    private void Awake()
    {
        targetWheel = GetComponent<WheelCollider>();
    }
    
    public void SetSkidActive(bool isActive)
    {
        isSkiddingSignal = isActive;
    }

    private void LateUpdate()
    {
        if (targetWheel == null || skidTrailPrefab == null) return;

        WheelHit hit;
        bool isGrounded = targetWheel.GetGroundHit(out hit);
        
        if (isGrounded && isSkiddingSignal)
        {
            if (currentTrail == null)
            {
                CreateNewTrail();
            }

            if (currentTrail != null)
            {
                currentTrail.transform.position = hit.point + (hit.normal * groundOffset);
                
                if (hit.normal.sqrMagnitude > 0.001f)
                {
                    currentTrail.transform.rotation = Quaternion.LookRotation(-hit.normal, targetWheel.transform.forward);
                }
            }
        }
        else
        {
            currentTrail = null;
        }
    }

    private void CreateNewTrail()
    {
        currentTrail = Instantiate(skidTrailPrefab, transform.position, Quaternion.identity);
        currentTrail.name = $"{targetWheel.name}_Skid";
        currentTrail.transform.parent = null;
        currentTrail.emitting = true;
        Destroy(currentTrail.gameObject, skidTrailPrefab.time + 1.0f);
    }
}