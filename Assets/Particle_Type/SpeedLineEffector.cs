using UnityEngine;
using UnityEngine.Rendering.Universal;

[ExecuteAlways]
public class SpeedLineEffector : MonoBehaviour
{
    private const string LAYER_NAME = "SpeedLine";

    [Header("References")]
    [SerializeField] private Camera _overlayCamera;
    [SerializeField] private ParticleSystem _ps;
    [SerializeField] private Transform _targetCameraTransform;

    [Header("Positioning")]
    [SerializeField] private Vector3 _particleOffset = new Vector3(0, 0, 10f);
    [SerializeField] private bool _syncRotation = true;

    [Header("Control")]
    [SerializeField] private bool _isActive = true;

    [Header("Particle Settings")]
    [SerializeField] private float _startSpeed = 40f;
    [SerializeField] private float _radius = 15f;
    [SerializeField] private float _emissionRate = 30f;
    [SerializeField] private Vector3 _startSize3D = new Vector3(0.5f, 10f, 1f);
    [SerializeField] private Color _colorMin = new Color(1, 1, 1, 0.5f);
    [SerializeField] private Color _colorMax = new Color(0, 1, 1, 1f);

    private bool _needsUpdate = true;
    private bool _wasActive = true;

    private void OnEnable()
    {
        if (_targetCameraTransform == null && Camera.main != null)
        {
            _targetCameraTransform = Camera.main.transform;
        }

        SetupLayerSettings();
        SetupCameraStack();
        _needsUpdate = true;
    }

    private void OnDisable()
    {
        CleanupCameraStack();
    }

    private void SetupLayerSettings()
    {
        int layerIndex = LayerMask.NameToLayer(LAYER_NAME);

        if (layerIndex == -1)
        {
            Debug.LogError($"[SpeedLineEffector] '{LAYER_NAME}' Layer not found.");
            return;
        }

        if (_ps != null)
        {
            _ps.gameObject.layer = layerIndex;
        }

        if (_overlayCamera != null)
        {
            _overlayCamera.cullingMask = (1 << layerIndex);
        }

        if (_targetCameraTransform != null)
        {
            Camera mainCam = _targetCameraTransform.GetComponent<Camera>();
            if (mainCam != null)
            {
                mainCam.cullingMask &= ~(1 << layerIndex);
            }
        }
    }

    private void SetupCameraStack()
    {
        if (_targetCameraTransform == null || _overlayCamera == null) return;

        var cameraData = _targetCameraTransform.GetComponent<UniversalAdditionalCameraData>();
        
        if (cameraData != null)
        {
            bool alreadyInStack = false;
            foreach (var cam in cameraData.cameraStack)
            {
                if (cam == _overlayCamera)
                {
                    alreadyInStack = true;
                    break;
                }
            }

            if (!alreadyInStack)
            {
                cameraData.cameraStack.Add(_overlayCamera);
            }
        }
    }

    private void CleanupCameraStack()
    {
        if (_targetCameraTransform == null || _overlayCamera == null) return;

        var cameraData = _targetCameraTransform.GetComponent<UniversalAdditionalCameraData>();

        if (cameraData != null)
        {
            if (cameraData.cameraStack.Contains(_overlayCamera))
            {
                cameraData.cameraStack.Remove(_overlayCamera);
            }
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set { _isActive = value; }
    }

    public Transform TargetCamera
    {
        get => _targetCameraTransform;
        set 
        { 
                CleanupCameraStack();
                _targetCameraTransform = value; 
                SetupLayerSettings(); 
                SetupCameraStack();
        }
    }

    public float StartSpeed { get => _startSpeed; set { if (_startSpeed != value) { _startSpeed = value; _needsUpdate = true; } } }
    public float Radius { get => _radius; set { if (_radius != value) { _radius = value; _needsUpdate = true; } } }
    public float EmissionRate { get => _emissionRate; set { if (_emissionRate != value) { _emissionRate = value; _needsUpdate = true; } } }
    public Vector3 StartSize3D { get => _startSize3D; set { if (_startSize3D != value) { _startSize3D = value; _needsUpdate = true; } } }

    public void SetColors(Color min, Color max)
    {
        _colorMin = min;
        _colorMax = max;
        _needsUpdate = true;
    }

    private void OnValidate()
    {
        _needsUpdate = true;
    }

    private void LateUpdate()
    {
        SyncTransform();
        HandleActiveState();

        if (_needsUpdate && _ps != null)
        {
            ApplyParticleSettings();
            _needsUpdate = false;
        }
    }

    private void SyncTransform()
    {
        if (_overlayCamera == null || _targetCameraTransform == null) return;

        _overlayCamera.transform.position = _targetCameraTransform.position;
        if (_syncRotation)
        {
            _overlayCamera.transform.rotation = _targetCameraTransform.rotation;
        }

        if (_ps != null)
        {
            _ps.transform.position = _overlayCamera.transform.TransformPoint(_particleOffset);
            
            if (_syncRotation)
            {
                _ps.transform.rotation = _targetCameraTransform.rotation;
            }
        }
    }

    private void HandleActiveState()
    {
        if (_ps == null) return;

        if (_isActive != _wasActive)
        {
            if (_isActive)
            {
                _ps.gameObject.SetActive(true);
                if (!_ps.isPlaying) _ps.Play();
            }
            else
            {
                _ps.Stop();
                _ps.gameObject.SetActive(false);
            }
            _wasActive = _isActive;
        }
        
        if (_isActive && !_ps.isPlaying)
        {
            _ps.Play();
        }
    }

    private void ApplyParticleSettings()
    {
        var main = _ps.main;
        var shape = _ps.shape;
        var emission = _ps.emission;

        main.startSize3D = true;
        main.startSizeX = _startSize3D.x;
        main.startSizeY = _startSize3D.y;
        main.startSizeZ = _startSize3D.z;

        main.startSpeed = _startSpeed;
        main.startColor = new ParticleSystem.MinMaxGradient(_colorMin, _colorMax);

        if (shape.shapeType != ParticleSystemShapeType.Circle)
            shape.shapeType = ParticleSystemShapeType.Circle;
        
        shape.radius = _radius;
        emission.rateOverTime = _emissionRate;
    }
}