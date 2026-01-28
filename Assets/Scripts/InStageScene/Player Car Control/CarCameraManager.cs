using UnityEngine;

public class CarCameraManager : MonoBehaviour
{
    private const string LAYER_NAME = "SpeedLine";

    [Header("Camera References")]
    public Camera sideCameraL;
    public Camera sideCameraR;
    public Camera backMirrorCamera;

    public bool IsLeftCameraOn => sideCameraL != null && sideCameraL.gameObject.activeSelf;
    public bool IsRightCameraOn => sideCameraR != null && sideCameraR.gameObject.activeSelf;

    private void Start()
    {
        FilterOutSpeedLineLayer();
    }

    private void FilterOutSpeedLineLayer()
    {
        int layerIndex = LayerMask.NameToLayer(LAYER_NAME);

        if (layerIndex == -1)
        {
            Debug.LogWarning($"[CarCameraManager] '{LAYER_NAME}' Layer not found in project settings.");
            return;
        }

        int maskToRemove = ~(1 << layerIndex);

        MaskOutLayer(sideCameraL, maskToRemove);
        MaskOutLayer(sideCameraR, maskToRemove);
        MaskOutLayer(backMirrorCamera, maskToRemove);
    }

    private void MaskOutLayer(Camera cam, int mask)
    {
        if (cam != null)
        {
            cam.cullingMask &= mask;
        }
    }

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