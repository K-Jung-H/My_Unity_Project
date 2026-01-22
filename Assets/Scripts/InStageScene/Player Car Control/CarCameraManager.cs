using UnityEngine;

public class CarCameraManager : MonoBehaviour
{
    [Header("Camera References")]
    public Camera sideCameraL;
    public Camera sideCameraR;
    public Camera backMirrorCamera;

    public bool IsLeftCameraOn => sideCameraL != null && sideCameraL.gameObject.activeSelf;
    public bool IsRightCameraOn => sideCameraR != null && sideCameraR.gameObject.activeSelf;

    public void SetLeftCamera(bool isOn)
    {
        if (sideCameraL != null) sideCameraL.gameObject.SetActive(isOn);
    }

    public void SetRightCamera(bool isOn)
    {
        if (sideCameraR != null) sideCameraR.gameObject.SetActive(isOn);
    }
    
    public void ToggleSideCameraL()
    {
        SetLeftCamera(!IsLeftCameraOn);
    }

    public void ToggleSideCameraR()
    {
        SetRightCamera(!IsRightCameraOn);
    }

    public void SetBackCameraState(bool isOn)
    {
        if (backMirrorCamera != null) backMirrorCamera.gameObject.SetActive(isOn);
    }
}