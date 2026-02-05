using System;
using System.Collections.Generic;
using UnityEngine;


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