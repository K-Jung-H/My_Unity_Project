using UnityEngine;
using UnityEngine.InputSystem;

public class Debug_Command : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetObject;
    public Vector3 targetPosition;
    public bool resetRotation = true;

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.rKey.isPressed)
        {
            TeleportObject();
        }
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
    }
}