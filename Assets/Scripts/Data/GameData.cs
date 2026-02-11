using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public static class GameData
{
    public static GameMode gameMode = GameMode.Default;
    public static int CarId = 0;
    public static int DifficultyIndex = 0;
    public static List<BiomeType> activeBiomes = new List<BiomeType>();
    public static int TotalScore = 0;

    public static void Reset()
    {
        gameMode = GameMode.Default;
        CarId = 0;
        DifficultyIndex = 0;
        activeBiomes.Clear();
        TotalScore = 0;
    }
}

[System.Serializable]
public class GameResult
{
    public string playDate;
    public int finalScore;
    public int carId;
    public GameMode gameMode;
    public int playedChunkCount;

    public GameResult()
    {
        playDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        finalScore = GameData.TotalScore;
        carId = GameData.CarId;
        gameMode = GameData.gameMode;
        
        if (GameData.activeBiomes != null)
        {
            playedChunkCount = GameData.activeBiomes.Count;
        }
        else
        {
            playedChunkCount = 0;
        }
    }
}

[System.Serializable]
public class GameHistory
{
    public List<GameResult> results = new List<GameResult>();
    public static string FilePath => Path.Combine(Application.persistentDataPath, "GameHistory.json");
    
    public int GetHighScore()
    {
        if (results == null || results.Count == 0) return 0;

        int maxScore = 0;
        foreach (var result in results)
        {
            if (result.finalScore > maxScore)
            {
                maxScore = result.finalScore;
            }
        }
        return maxScore;
    }
}

