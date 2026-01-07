using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ClickAction : MonoBehaviour
{
    [System.Serializable]
    public class Vector3Event : UnityEvent<Vector3> { }

    [Header("Click Settings")]
    public LayerMask clickLayerMask = ~0;

    [Header("Events")]
    public UnityEvent onVoidClick;

    public Vector3Event onPositionClick;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found");
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = mouse.position.ReadValue();
            OnMouseClick(mousePos);
        }
    }

    private void OnMouseClick(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane, clickLayerMask))
        {
            onPositionClick?.Invoke(hit.point);
            onVoidClick?.Invoke();
        }
    }
}