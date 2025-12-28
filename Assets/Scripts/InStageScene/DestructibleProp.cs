using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DestructibleProp : MonoBehaviour
{
    private Rigidbody rb;
    private Collider col;

    private Vector2Int myChunkCoord;
    private int myIndex;
    private bool isInitialized = false;
    private bool isDestroyed = false; 

    [Header("Physics Settings")]
    public float pushPower = 2.0f;
    public float hitThreshold = 1.0f;

    [Header("Destroy Settings")]
    public float lifeTime = 2.0f;


    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    public void InitProp(Vector2Int chunkCoord, int index)
    {
        myChunkCoord = chunkCoord;
        myIndex = index;
        isInitialized = true;
    }

    public void SetDestroyedState()
    {
        isDestroyed = true;
        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        isDestroyed = false;
        gameObject.SetActive(true);

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized || isDestroyed || !rb.isKinematic)
        {
            Debug.Log("Collision ignored: " + (isInitialized ? "" : "Not initialized; ") + (isDestroyed ? "Already destroyed; " : "") + (!rb.isKinematic ? "Not kinematic; " : ""));
            return;
        }
        if (collision.rigidbody == null)
        {
            Debug.Log("Collision ignored: No rigidbody on colliding object.");
            return;
        }

        if (collision.relativeVelocity.magnitude > hitThreshold)
        {
            Debug.Log("DestructibleProp hit with velocity: " + collision.relativeVelocity.magnitude);
            BreakAndPush(collision);
        }
    }

    void BreakAndPush(Collision collision)
    {
        isDestroyed = true;
        rb.isKinematic = false; 

        Vector3 dir = -collision.contacts[0].normal + Vector3.up * 0.5f;
        dir.Normalize();
        float impactSpeed = Mathf.Max(collision.relativeVelocity.magnitude, 5.0f);

        rb.AddForce(dir * impactSpeed * pushPower, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * impactSpeed * pushPower * 2f, ForceMode.Impulse);

        WorldObjectDataManager.Instance.RegisterDestruction(myChunkCoord, myIndex);


        Invoke(nameof(DisableSelf), lifeTime);
    }

    void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}