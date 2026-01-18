using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic; 

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Settings")]
    public float scoreUpdateTime = 3f;
    public float scorePerUpdate = 10f;

    public int Score => GameData.TotalScore;

    public event Action<int> OnScoreChanged;

    private float timer;
    private bool isScoringActive = false;

    private List<HealthSystem> connectedHealthSystems = new List<HealthSystem>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        GameData.TotalScore = 0;
        timer = 0f;
        isScoringActive = true;
        
        connectedHealthSystems.Clear();

        HealthSystem[] allHealthSystems = FindObjectsByType<HealthSystem>(FindObjectsSortMode.None);
            
        int playerLayerIndex = LayerMask.NameToLayer("Player");

        foreach (var health in allHealthSystems)
        {
           
            if (health.gameObject.layer == playerLayerIndex)
            {
                health.OnDeath += SaveGameResult;
                connectedHealthSystems.Add(health); 
            }
        }
        
        Debug.Log($"ScoreManager Initialized. Connected to {connectedHealthSystems.Count} Player object(s).");
    }

    private void OnDestroy()
    {
        foreach (var health in connectedHealthSystems)
        {
            if (health != null)
            {
                health.OnDeath -= SaveGameResult;
            }
        }
        connectedHealthSystems.Clear();
    }

    private void Update()
    {
        if (!isScoringActive) return;

        timer += Time.deltaTime;
        if (timer >= scoreUpdateTime)
        {
            AddScore(Mathf.FloorToInt(scorePerUpdate));
            timer = 0f;
        }
    }

    public void AddScore(int amount)
    {
        if (!isScoringActive) return;

        GameData.TotalScore += amount;
        OnScoreChanged?.Invoke(GameData.TotalScore);
    }

    private void SaveGameResult()
    {
        if (!isScoringActive) return;
        
        isScoringActive = false;

        GameResult result = new GameResult();

        string json = JsonUtility.ToJson(result, true);

        string fileName = $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(Application.persistentDataPath, "GameLogs");

        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string fullPath = Path.Combine(path, fileName);
        File.WriteAllText(fullPath, json);

        Debug.Log($"[ScoreManager] Game Saved: {fullPath}");
        Debug.Log($"[ScoreManager] Final Score: {result.finalScore}");
    }
}