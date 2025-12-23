using UnityEngine;
using UnityEngine.EventSystems;

public class Reset_UI : MonoBehaviour, IPointerDownHandler
{
    [Header("Settings")]
    public Transform targetObject;
    public Vector3 targetPosition;
    public bool resetRotation = true;

    public void OnPointerDown(PointerEventData eventData)
    {
        TeleportObject();
    }

    void TeleportObject()
    {
        if (targetObject == null) return;

        targetObject.position = targetPosition;

        if (resetRotation)
        {
            targetObject.rotation = Quaternion.identity;
        }

        Rigidbody rb = targetObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log("Object Reset Complete");
    }
}