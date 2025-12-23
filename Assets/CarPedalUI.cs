using UnityEngine;
using UnityEngine.EventSystems;

public class CarPedalUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public float pressSpeed = 5f;
    public float releaseSpeed = 5f;
    public float InputValue { get; private set; }

    private bool isPressed;

    void Update()
    {
        float target = isPressed ? 1f : 0f;
        float speed = isPressed ? pressSpeed : releaseSpeed;

        InputValue = Mathf.MoveTowards(InputValue, target, speed * Time.deltaTime);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }
}