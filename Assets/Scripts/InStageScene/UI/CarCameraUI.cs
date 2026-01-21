using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CarCameraUI : MonoBehaviour
{
    [Header("UI References")]
    public Button leftMirrorBtn;
    public Button rightMirrorBtn;
    public GameObject backMirrorBtnObj;

    private CarCameraManager targetCameraManager;

    public void Initialize(CarCameraManager cameraManager)
    {
        targetCameraManager = cameraManager;

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (leftMirrorBtn != null)
        {
            leftMirrorBtn.onClick.RemoveAllListeners();
            leftMirrorBtn.onClick.AddListener(() => 
            {
                if (targetCameraManager != null) targetCameraManager.ToggleSideCameraL();
            });
        }

        if (rightMirrorBtn != null)
        {
            rightMirrorBtn.onClick.RemoveAllListeners();
            rightMirrorBtn.onClick.AddListener(() => 
            {
                if (targetCameraManager != null) targetCameraManager.ToggleSideCameraR();
            });
        }

        if (backMirrorBtnObj != null)
        {
            UIHoldHandler holdHandler = backMirrorBtnObj.GetComponent<UIHoldHandler>();
            if (holdHandler == null) holdHandler = backMirrorBtnObj.AddComponent<UIHoldHandler>();

            holdHandler.OnDownAction = () => 
            {
                if (targetCameraManager != null) targetCameraManager.SetBackCameraState(true);
            };
            
            holdHandler.OnUpAction = () => 
            {
                if (targetCameraManager != null) targetCameraManager.SetBackCameraState(false);
            };
        }
    }
}

public class UIHoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public System.Action OnDownAction;
    public System.Action OnUpAction;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDownAction?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnUpAction?.Invoke();
    }
}