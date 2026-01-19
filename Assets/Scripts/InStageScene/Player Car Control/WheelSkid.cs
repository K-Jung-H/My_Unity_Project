using UnityEngine;

public class WheelSkid : MonoBehaviour
{
    [Header("Settings")]
    public TrailRenderer skidTrailPrefab;
    public WheelCollider targetWheel;
    public CarController carController;

    [Header("Values")]
    public float groundOffset = 0.02f;

    private TrailRenderer currentTrail;

    void LateUpdate()
    {
        if (targetWheel == null || skidTrailPrefab == null) return;

        WheelHit hit;
        bool isGrounded = targetWheel.GetGroundHit(out hit);

        bool shouldEmit = carController != null && carController.IsSkidding;

        if (isGrounded && shouldEmit)
        {
            if (currentTrail == null)
            {
                CreateNewTrail();
            }

            if (currentTrail != null)
            {
                currentTrail.transform.position = hit.point + (hit.normal * groundOffset);
                if (hit.normal.sqrMagnitude < 0.001f) hit.normal = Vector3.up;
                currentTrail.transform.rotation = Quaternion.LookRotation(-hit.normal, targetWheel.transform.forward);
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