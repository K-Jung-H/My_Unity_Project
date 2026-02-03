using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/Outline Blur")]
public class OutlineBlurVolume : VolumeComponent, IPostProcessComponent
{
    public BoolParameter isActive = new BoolParameter(false);
    public IntParameter optimizeLayer = new IntParameter(-1);
    public ClampedIntParameter downsample = new ClampedIntParameter(1, 0, 2);
    public ClampedIntParameter targetLightLayer = new ClampedIntParameter(1, 0, 32); 

    public ColorParameter outlineColor = new ColorParameter(Color.yellow);
    public ClampedFloatParameter outlineThickness = new ClampedFloatParameter(1f, 0f, 10f);
    public ClampedFloatParameter blurIntensity = new ClampedFloatParameter(1f, 0f, 5f);

    public bool IsActive() => active && isActive.value;
    public bool IsTileCompatible() => false;
}