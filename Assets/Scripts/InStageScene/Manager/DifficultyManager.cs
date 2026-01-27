using UnityEngine;
using System.Collections.Generic;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Data Source")]
    public DifficultyDataTable difficultyDataTable;

    [Header("Fallback Settings")]
    public int defaultMaxEnemies = 10;

    [Header("Runtime State")]
    [SerializeField] private DifficultyProfile currentProfile;
    [SerializeField] public float currentDifficultyValue = 0f;

    public float CurrentDifficulty => currentDifficultyValue;

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
        LoadDifficultyProfile();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChange;
            ScoreManager.Instance.OnScoreChanged += HandleScoreChange;

            HandleScoreChange(ScoreManager.Instance.Score);
        }
        else
        {
            Debug.LogError("[DifficultyManager] ScoreManager is null.");
        }
    }



    private void LoadDifficultyProfile()
    {
        if (difficultyDataTable == null)
        {
            Debug.LogError("[DifficultyManager] DifficultyDataTable is missing.");
            return;
        }

        int index = GameData.DifficultyIndex;
        currentProfile = difficultyDataTable.GetProfile(index);

        if (currentProfile == null)
        {
            Debug.LogError($"[DifficultyManager] Failed to load profile for Index {index}.");
        }
        else
        {
            Debug.Log($"[DifficultyManager] Difficulty Profile Loaded: {currentProfile.name}");
        }
    }

    void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChange;
        }
    }

    private void HandleScoreChange(int newScore)
    {
        if (currentProfile == null) return;

        currentDifficultyValue = newScore * currentProfile.difficultyScaler;
    }

    public int GetCurrentMaxEnemies()
    {
        if (currentProfile == null) return defaultMaxEnemies;
        return Mathf.RoundToInt(currentProfile.globalMaxEnemyCurve.Evaluate(currentDifficultyValue));
    }

    public EnemySpawnConfig PickEnemyToSpawn(Dictionary<string, int> currentEnemyCounts)
    {
        if (currentProfile == null || currentProfile.enemyConfigs.Count == 0) 
        {
            Debug.LogWarning("No Profile");
            return null;
        }

        float totalWeight = 0f;
        List<EnemySpawnConfig> candidates = new List<EnemySpawnConfig>();

        foreach (var config in currentProfile.enemyConfigs)
        {
            if (currentDifficultyValue < config.unlockThreshold) continue;

            if (config.maxInstanceCount != -1)
            {
                int currentCount = currentEnemyCounts.ContainsKey(config.enemyName) ? currentEnemyCounts[config.enemyName] : 0;
                if (currentCount >= config.maxInstanceCount) continue;
            }

            float weight = config.spawnWeightCurve.Evaluate(currentDifficultyValue);
            if (weight > 0)
            {
                candidates.Add(config);
                totalWeight += weight;
            }
        }

        if (candidates.Count == 0) return null;

        float randomPoint = Random.Range(0, totalWeight);
        float currentSum = 0f;

        foreach (var config in candidates)
        {
            currentSum += config.spawnWeightCurve.Evaluate(currentDifficultyValue);
            if (randomPoint <= currentSum)
            {
                return config;
            }
        }


        return candidates[0];
    }
}