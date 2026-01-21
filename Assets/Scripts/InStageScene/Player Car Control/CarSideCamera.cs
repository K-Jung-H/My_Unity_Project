using UnityEngine;

public class CarCameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public Camera sideCameraL;
    public Camera sideCameraR;
    public Camera backMirrorCamera;

    private void Start()
    {
        if(sideCameraL) sideCameraL.gameObject.SetActive(false);
        if(sideCameraR) sideCameraR.gameObject.SetActive(false);
        if(backMirrorCamera) backMirrorCamera.gameObject.SetActive(false);
    }

    public void ToggleSideCameraL()
    {
        if (sideCameraL != null)
        {
            bool isActive = sideCameraL.gameObject.activeSelf;
            sideCameraL.gameObject.SetActive(!isActive);
        }
    }

    public void ToggleSideCameraR()
    {
        if (sideCameraR != null)
        {
            bool isActive = sideCameraR.gameObject.activeSelf;
            sideCameraR.gameObject.SetActive(!isActive);
        }
    }


    public void SetBackCameraState(bool isOn)
    {
        if (backMirrorCamera != null)
        {
            backMirrorCamera.gameObject.SetActive(isOn);
        }
    }
}