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

    private int playerLayer;

    [Header("Physics Settings")]
    public float pushPower = 2.0f;
    public float hitThreshold = 1.0f;

    [Header("Destroy Settings")]
    public float lifeTime = 2.0f;

    [Header("Score Settings")]
    public int scoreValue = 50;


    private Vector3 initialLocalPos;
    private Quaternion initialLocalRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        playerLayer = LayerMask.NameToLayer("Player");

        initialLocalPos = transform.localPosition;
        initialLocalRot = transform.localRotation;
    }

    public void InitProp(Vector2Int chunkCoord, int index)
    {
        myChunkCoord = chunkCoord;
        myIndex = index;
        isInitialized = true;
        
        rb.isKinematic = true;
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

        rb.isKinematic = false; 
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true; 

        transform.localPosition = initialLocalPos;
        transform.localRotation = initialLocalRot;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isDestroyed) return;

        if (other.gameObject.layer == playerLayer)
        {
            rb.isKinematic = false;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (isDestroyed) return;

        if (other.gameObject.layer == playerLayer)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isInitialized || isDestroyed)
        {
            return;
        }
        
        if (rb.isKinematic)
        {
            rb.isKinematic = false;
        }

        if (collision.rigidbody == null)
        {
            return;
        }

        if (collision.relativeVelocity.magnitude > hitThreshold)
        {
            bool isPlayerAction = collision.gameObject.layer == playerLayer;
            BreakAndPush(collision, isPlayerAction);
        }
    }

    void BreakAndPush(Collision collision, bool causedByPlayer)    
    {
        isDestroyed = true;
        rb.isKinematic = false;

        Vector3 dir = -collision.contacts[0].normal + Vector3.up * 0.5f;
        dir.Normalize();
        float impactSpeed = Mathf.Max(collision.relativeVelocity.magnitude, 5.0f);

        rb.AddForce(dir * impactSpeed * pushPower, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * impactSpeed * pushPower * 2f, ForceMode.Impulse);

        if (WorldObjectDataManager.Instance != null)
        {
            WorldObjectDataManager.Instance.RegisterDestruction(myChunkCoord, myIndex);
        }

        if (causedByPlayer && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(scoreValue);
        }

        Invoke(nameof(DisableSelf), lifeTime);
    }

    void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}