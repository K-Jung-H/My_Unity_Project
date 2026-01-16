using UnityEngine;

public class OutlineBlurManager : MonoBehaviour
{
    public static OutlineBlurManager Instance { get; private set; }

    [Header("Resources")]
    public Material edgeMaterial;
    public Material compositeMaterial;

    [Header("Settings")]
    public bool isOutlineActive = true;
    public Color outlineColor = Color.yellow;

    [Range(1, 10)]
    public int outlineThickness = 1;

    [Range(0.0f, 5.0f)]
    public float blurIntensity = 1.0f;

    public uint renderingLayerMask = 2; 

    private static readonly int ThicknessID = Shader.PropertyToID("_OutlineThickness");
    private static readonly int ColorID = Shader.PropertyToID("_OutlineColor");
    private static readonly int BlurID = Shader.PropertyToID("_BlurIntensity");

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start() => UpdateOutlineSettings();
    void Update() => UpdateOutlineSettings();
    void OnValidate() => UpdateOutlineSettings();

    public void UpdateOutlineSettings()
    {
        // Feature 전역 변수 설정
        OutlineBlurFeature.IsActive = isOutlineActive;
        OutlineBlurFeature.ExtensionLayerMask = renderingLayerMask;

        if (!isOutlineActive) return;

        if (edgeMaterial != null)
            edgeMaterial.SetFloat(ThicknessID, outlineThickness);

        if (compositeMaterial != null)
        {
            compositeMaterial.SetColor(ColorID, outlineColor);
            compositeMaterial.SetFloat(BlurID, blurIntensity);
        }
    }
}