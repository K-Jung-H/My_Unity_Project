using System;
using System.Collections.Generic;
using UnityEngine;

public enum GameMode
{
    Default,
    Custom
}

[CreateAssetMenu(fileName = "NewChunk", menuName = "Game/ChunkData")]
public class ChunkData : ScriptableObject
{
    public string chunkName;
    public GameObject chunkPrefab;
    public Sprite icon;
    public bool isMandatory;
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
        
        if (GameData.selectedChunks != null)
        {
            playedChunkCount = GameData.selectedChunks.Count;
        }
        else
        {
            playedChunkCount = 0;
        }
    }
}

public static class GameData
{
    public static int CarId = 0;
    public static GameMode gameMode = GameMode.Default;
    public static List<ChunkData> selectedChunks = new List<ChunkData>();
    
    public static int TotalScore = 0;

    public static void Reset()
    {
        TotalScore = 0;
    }
}