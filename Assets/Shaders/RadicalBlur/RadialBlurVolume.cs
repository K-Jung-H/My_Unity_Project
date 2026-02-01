using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/Radial Blur")]
public class RadialBlurVolume : VolumeComponent, IPostProcessComponent
{
    public FloatParameter intensity = new FloatParameter(0f);
    public IntParameter sampleCount = new IntParameter(10);
    public ClampedIntParameter downsample = new ClampedIntParameter(1, 0, 4); 

    public bool IsActive() => intensity.value > 0f && active;
    public bool IsTileCompatible() => false;
}