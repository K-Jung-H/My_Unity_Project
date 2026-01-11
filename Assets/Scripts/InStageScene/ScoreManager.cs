using UnityEngine;
using System;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Settings")]
    public float scoreUpdateTime = 3f;
    public float scorePerUpdate = 10f;

    public event Action<int> OnScoreChanged;
    private float timer;
    private bool isScoringActive = true;

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
        GameData.totalScore += amount;
        OnScoreChanged?.Invoke(GameData.totalScore);
    }
}