using UnityEngine;
using System;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance;

    [Header("Time Settings")]
    [Range(0, 24)] public float timeOfDay = 12f;
    public float cycleDurationMinutes = 6f;

    public bool IsNight { get; private set; }

    public event Action<bool> OnDayNightChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize()
    {

    }

    private void Update()
    {
        if (cycleDurationMinutes > 0)
        {
            float timeMultiplier = 24f / (cycleDurationMinutes * 60f);
            timeOfDay += Time.deltaTime * timeMultiplier;
            
            if (timeOfDay >= 24f) timeOfDay %= 24f;
        }

        CheckDayNightState();
    }

    private void CheckDayNightState()
    {
        bool currentIsNight = (timeOfDay >= 18f || timeOfDay < 6f);

        if (IsNight != currentIsNight)
        {
            IsNight = currentIsNight;
            OnDayNightChanged?.Invoke(IsNight);
        }
    }
}