using UnityEngine;
using UnityEngine.EventSystems;

public class SteeringWheelUI : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rectTransform;
    private float currentAngle = 0f;
    private Vector2 lastDir;
    private bool isDragging = false;

    [Header("Settings")]
    public float maxSteeringAngle = 450f;
    public float releaseSpeed = 300f;

    public float InputValue { get; private set; }

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (!isDragging && Mathf.Abs(currentAngle) > 0.1f)
        {
            float step = releaseSpeed * Time.deltaTime;
            currentAngle = Mathf.MoveTowards(currentAngle, 0f, step);
            ApplyRotation();
        }

        InputValue = Mathf.Clamp(currentAngle / maxSteeringAngle, -1f, 1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;

        Vector2 centerPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        lastDir = eventData.position - centerPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 centerPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
        Vector2 currentDir = eventData.position - centerPos;

        float deltaAngle = Vector2.SignedAngle(lastDir, currentDir);

        currentAngle -= deltaAngle;

        currentAngle = Mathf.Clamp(currentAngle, -maxSteeringAngle, maxSteeringAngle);

        lastDir = currentDir;

        ApplyRotation();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
    }

    private void ApplyRotation()
    {
        rectTransform.localEulerAngles = new Vector3(0, 0, -currentAngle);
    }
}