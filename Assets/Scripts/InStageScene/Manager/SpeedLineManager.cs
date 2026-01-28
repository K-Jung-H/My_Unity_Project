using UnityEngine;
using System.Collections.Generic;

public class SpeedLineManager : MonoBehaviour
{
    public static SpeedLineManager Instance { get; private set; }

    [System.Serializable]
    public struct SpeedConfig
    {
        public float speedThreshold;
        public float emissionRate;
        public float radius;
        public Vector3 startSize;
    }

    [Header("Dependencies")]
    [SerializeField] private Transform _effectContainer;
    [SerializeField] private List<SpeedLineEffector> _effectorPrefabs;
    
    private CarController _targetCar;
    private SpeedLineEffector _currentEffectorInstance;

    [Header("Settings")]
    [SerializeField] private List<SpeedConfig> _speedTable = new List<SpeedConfig>();
    [SerializeField] private float _smoothing = 5f;

    private float _currentEmission;
    private float _currentRadius;
    private Vector3 _currentStartSize;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public void Initialize()
    {
        if (_effectorPrefabs == null || _effectorPrefabs.Count == 0)
        {
            Debug.LogError("[SpeedLineManager] Prefab list is empty.");
            return;
        }

        if (_effectContainer == null)
        {
            Debug.LogError("[SpeedLineManager] Effect Container is not assigned.");
            return;
        }

        if (_currentEffectorInstance != null)
        {
            Destroy(_currentEffectorInstance.gameObject);
        }

        var prefab = _effectorPrefabs[0];
        var obj = Instantiate(prefab, _effectContainer);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        
        _currentEffectorInstance = obj.GetComponent<SpeedLineEffector>();
        _currentEffectorInstance.IsActive = false;
        
        if (_speedTable.Count > 0)
        {
            _currentStartSize = _speedTable[0].startSize;
        }
    }

    public void SetTargetCar(CarController car)
    {
        _targetCar = car;
        
        if (_currentEffectorInstance != null)
        {
            _currentEffectorInstance.EmissionRate = 0f;
            _currentEffectorInstance.IsActive = false;
        }
    }

    private void Update()
    {
        if (_targetCar == null || _currentEffectorInstance == null) return;
        if (_speedTable.Count == 0) return;

        float currentSpeed = _targetCar.CurrentSpeed;

        CalculateTargetValues(currentSpeed, out float targetEmission, out float targetRadius, out Vector3 targetSize);

        float dt = Time.deltaTime * _smoothing;
        _currentEmission = Mathf.Lerp(_currentEmission, targetEmission, dt);
        _currentRadius = Mathf.Lerp(_currentRadius, targetRadius, dt);
        _currentStartSize = Vector3.Lerp(_currentStartSize, targetSize, dt);

        if (_currentEmission > 0.1f)
        {
            if (!_currentEffectorInstance.IsActive) _currentEffectorInstance.IsActive = true;
            
            _currentEffectorInstance.EmissionRate = _currentEmission;
            _currentEffectorInstance.Radius = _currentRadius;
            _currentEffectorInstance.StartSize3D = _currentStartSize;
        }
        else
        {
            if (_currentEffectorInstance.IsActive) _currentEffectorInstance.IsActive = false;
        }
    }

    private void CalculateTargetValues(float speed, out float emission, out float radius, out Vector3 size)
    {
        if (speed < _speedTable[0].speedThreshold)
        {
            emission = 0f;
            radius = _speedTable[0].radius;
            size = _speedTable[0].startSize;
            return;
        }

        if (speed >= _speedTable[_speedTable.Count - 1].speedThreshold)
        {
            emission = _speedTable[_speedTable.Count - 1].emissionRate;
            radius = _speedTable[_speedTable.Count - 1].radius;
            size = _speedTable[_speedTable.Count - 1].startSize;
            return;
        }

        for (int i = 0; i < _speedTable.Count - 1; i++)
        {
            if (speed >= _speedTable[i].speedThreshold && speed <= _speedTable[i + 1].speedThreshold)
            {
                float t = Mathf.InverseLerp(_speedTable[i].speedThreshold, _speedTable[i + 1].speedThreshold, speed);
                
                emission = Mathf.Lerp(_speedTable[i].emissionRate, _speedTable[i + 1].emissionRate, t);
                radius = Mathf.Lerp(_speedTable[i].radius, _speedTable[i + 1].radius, t);
                size = Vector3.Lerp(_speedTable[i].startSize, _speedTable[i + 1].startSize, t);
                return;
            }
        }

        emission = 0f;
        radius = 20f;
        size = Vector3.one;
    }
}