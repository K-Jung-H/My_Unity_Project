using UnityEngine;
using System.Collections.Generic;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Settings")]
    public DifficultyProfile profile;
    

    [SerializeField] public float currentDifficultyValue = 0f;

    public float CurrentDifficulty => currentDifficultyValue;


    public void Initialize(ScoreManager targetScoreManager)
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (targetScoreManager != null)
        {
            targetScoreManager.OnScoreChanged -= HandleScoreChange;         
            targetScoreManager.OnScoreChanged += HandleScoreChange;
            
            HandleScoreChange(targetScoreManager.Score);

            Debug.Log("DifficultyManager Initialized (ScoreManager Connected)");
        }
        else
        {
            Debug.LogError("[DifficultyManager] 초기화 실패: 주입된 ScoreManager가 Null입니다.");
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
        if (profile == null) return; 

        currentDifficultyValue = newScore * profile.difficultyScaler;
    }

    public int GetCurrentMaxEnemies()
    {
        if (profile == null) return 10;
        return Mathf.RoundToInt(profile.globalMaxEnemyCurve.Evaluate(currentDifficultyValue));
    }

    public EnemySpawnConfig PickEnemyToSpawn(Dictionary<string, int> currentEnemyCounts)
    {
        if (profile == null || profile.enemyConfigs.Count == 0) return null;

        float totalWeight = 0f;
        List<EnemySpawnConfig> candidates = new List<EnemySpawnConfig>();

        foreach (var config in profile.enemyConfigs)
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