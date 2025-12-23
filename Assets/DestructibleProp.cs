using UnityEngine;

public class DestructibleProp : MonoBehaviour
{
    private Rigidbody rb;
    private bool isHit = false;

    public float pushPower = 2.0f;
    public float hitThreshold = 1.0f;
    public float lifeTime = 10.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (rb == null || !rb.isKinematic) return;

        if (other.attachedRigidbody != null)
        {
            rb.isKinematic = false;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isHit || rb == null) return;

        if (collision.rigidbody == null) return;

        if (collision.relativeVelocity.magnitude > hitThreshold)
        {
            WakeUpPhysics(collision);
        }
    }

    void WakeUpPhysics(Collision collision)
    {
        isHit = true;
        rb.isKinematic = false;

        Vector3 dir = -collision.contacts[0].normal + Vector3.up * 0.5f;
        dir.Normalize();

        float impactSpeed = Mathf.Max(collision.relativeVelocity.magnitude, 5.0f);

        rb.AddForce(dir * impactSpeed * pushPower, ForceMode.VelocityChange);
        rb.AddTorque(Random.insideUnitSphere * impactSpeed * pushPower * 2f, ForceMode.Impulse);

        Destroy(gameObject, lifeTime);
    }
}