using UnityEngine;
using UnityEngine.EventSystems;

public class Button_UI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private bool isPressed;

    void Update()
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
        Debug.Log("Button Pressed");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public bool GetButtonState()
    {
        return isPressed;
    }
}
