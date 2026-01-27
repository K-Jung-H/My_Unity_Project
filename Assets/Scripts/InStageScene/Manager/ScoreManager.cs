using UnityEngine;
using System;
using System.IO;

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
    private CarController targetCar;

    public void Initialize()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GameData.TotalScore = Mathf.Max(0, GameData.TotalScore);
        timer = 0f;
    }

    public void SetTarget(CarController car)
    {
        targetCar = car;
        isScoringActive = (targetCar != null);
        Debug.Log("ScoreManager Target Set and Active");
    }

    private void Update()
    {
        if (!isScoringActive || targetCar == null) return;

        timer += Time.deltaTime;
        if (timer >= scoreUpdateTime)
        {
            AddScore(Mathf.FloorToInt(scorePerUpdate));
            timer = 0f;
        }
    }

    public void AddScore(int amount)
    {
        GameData.TotalScore += amount;
        if (GameData.TotalScore < 0) GameData.TotalScore = 0;
        OnScoreChanged?.Invoke(GameData.TotalScore);
    }

    public void ApplyScorePenalty(float keepRatio)
    {
        if (keepRatio < 0f) return;
        int newScore = Mathf.FloorToInt(GameData.TotalScore * keepRatio);
        GameData.TotalScore = Mathf.Max(0, newScore);
        OnScoreChanged?.Invoke(GameData.TotalScore);
    }

    public void SaveGameResult()
    {
        if (!isScoringActive) return;
        isScoringActive = false;
        
        GameResult result = new GameResult();
        string json = JsonUtility.ToJson(result, true);
        string fileName = $"GameLog_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string path = Path.Combine(Application.persistentDataPath, "GameLogs");

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        File.WriteAllText(Path.Combine(path, fileName), json);
    }
}