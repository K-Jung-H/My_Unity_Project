using UnityEngine;

public class SkyBox_DayNightCycle : MonoBehaviour
{
    [Header("References")]
    public Light sunLight;

    [Header("Sky Blend Settings")]
    public Gradient sunColor;
    public Gradient skyTintColor; 
    public float skyRotationSpeed = 1f;

    [Header("Ambient Settings")]
    [Range(0f, 2f)] public float dayAmbientIntensity = 1f;
    [Range(0f, 2f)] public float nightAmbientIntensity = 0.2f;

    private void Start()
    {
        Material currentSky = RenderSettings.skybox;
        if (currentSky != null)
        {
            currentSky.SetFloat("_EnableRotation", 1f);
            currentSky.EnableKeyword("_ENABLEROTATION_ON");
        }
    }

    private void Update()
    {
        if (GameTimeManager.Instance == null) return;

        float currentTime = GameTimeManager.Instance.timeOfDay;

        UpdateSun(currentTime);
        UpdateSkybox(currentTime);
        UpdateAmbientLight(currentTime);
    }

    private void UpdateSun(float time)
    {
        if (sunLight == null) return;

        float rotX = (time / 24f) * 360f - 90f;
        sunLight.transform.localRotation = Quaternion.Euler(rotX, 170f, 0f);

        float time01 = time / 24f;
        sunLight.color = sunColor.Evaluate(time01);

        if (time < 6f || time > 18f)
            sunLight.intensity = Mathf.MoveTowards(sunLight.intensity, 0f, Time.deltaTime);
        else
            sunLight.intensity = Mathf.MoveTowards(sunLight.intensity, 1f, Time.deltaTime);
    }

    private void UpdateSkybox(float time)
    {
        Material activeSkybox = RenderSettings.skybox;
        if (activeSkybox == null) return;

        float radian = (time / 24f) * Mathf.PI * 2f;
        float blendFactor = (Mathf.Cos(radian) + 1f) * 0.5f;

        activeSkybox.SetFloat("_CubemapTransition", blendFactor);
        
        float time01 = time / 24f;
        activeSkybox.SetColor("_TintColor", skyTintColor.Evaluate(time01));
        activeSkybox.SetFloat("_Exposure", 1f);

        float currentRot = activeSkybox.GetFloat("_Rotation");
        float nextRot = currentRot + (Time.deltaTime * skyRotationSpeed);
        if (nextRot >= 360f) nextRot -= 360f;
        
        activeSkybox.SetFloat("_Rotation", nextRot);
    }

    private void UpdateAmbientLight(float time)
    {
        float radian = (time / 24f) * Mathf.PI * 2f;
        
        float dayFactor = (-Mathf.Cos(radian) + 1f) * 0.5f;

        RenderSettings.ambientIntensity = Mathf.Lerp(nightAmbientIntensity, dayAmbientIntensity, dayFactor);
    }
}