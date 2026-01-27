using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CarCameraUIManager : MonoBehaviour
{
    [Header("Option UI References (Side Mirrors)")]
    public Toggle leftMirrorToggle;
    public Toggle rightMirrorToggle;

    [Header("Game UI References (Back Mirror)")]
    public GameObject backMirrorInputObj;

    private CarCameraManager targetCameraManager;

    public void Initialize()
    {
        
    }

    public void SetTarget(CarController localPlayer)
    {
        CarCameraManager playerCameraManager = localPlayer.GetComponentInChildren<CarCameraManager>();
        targetCameraManager = playerCameraManager;
        targetCameraManager.SetBackCameraState(false);
        
        SyncTogglesWithPlayer();

        SetupListeners();
    }

    private void SyncTogglesWithPlayer()
    {
        if (targetCameraManager == null) return;
        
        if (leftMirrorToggle != null)
            leftMirrorToggle.SetIsOnWithoutNotify(targetCameraManager.IsLeftCameraOn);

        if (rightMirrorToggle != null)
            rightMirrorToggle.SetIsOnWithoutNotify(targetCameraManager.IsRightCameraOn);
    }

    private void SetupListeners()
    {
        if (leftMirrorToggle != null)
        {
            leftMirrorToggle.onValueChanged.RemoveAllListeners();
            leftMirrorToggle.onValueChanged.AddListener((isOn) => 
            {
                if (targetCameraManager != null) targetCameraManager.SetLeftCamera(isOn);
            });
        }

        if (rightMirrorToggle != null)
        {
            rightMirrorToggle.onValueChanged.RemoveAllListeners();
            rightMirrorToggle.onValueChanged.AddListener((isOn) => 
            {
                if (targetCameraManager != null) targetCameraManager.SetRightCamera(isOn);
            });
        }

        if (backMirrorInputObj != null)
        {
            UIHoldHandler holdHandler = backMirrorInputObj.GetComponent<UIHoldHandler>();
            if (holdHandler == null) holdHandler = backMirrorInputObj.AddComponent<UIHoldHandler>();

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

    public void OnPointerDown(PointerEventData eventData) => OnDownAction?.Invoke();
    public void OnPointerUp(PointerEventData eventData) => OnUpAction?.Invoke();
}