using UnityEngine;
using UnityEngine.EventSystems;

public class HoldPressInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Range Settings")]
    [Tooltip("버튼을 누르지 않았을 때의 기본값입니다.")]
    public float minValue = 0f;

    [Tooltip("버튼을 꾹 눌렀을 때 도달하는 최대값입니다.")]
    public float maxValue = 1f;

    [Header("Sensitivity Settings")]
    [Tooltip("눌렀을 때 값이 변하는 속도입니다. (초당 변화량)")]
    public float pressSpeed = 5f;

    [Tooltip("뗐을 때 값이 복귀하는 속도입니다. (초당 변화량)")]
    public float releaseSpeed = 5f;

    public float CurrentValue { get; private set; }

    private bool isPressed;

    private void OnEnable()
    {
        CurrentValue = minValue;
        isPressed = false;
    }

    void Update()
    {
        float target = isPressed ? maxValue : minValue;
        float speed = isPressed ? pressSpeed : releaseSpeed;

        CurrentValue = Mathf.MoveTowards(CurrentValue, target, speed * Time.deltaTime);
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