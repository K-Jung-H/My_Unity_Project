using UnityEngine;

public static class GameData
{
    public static string selectedMapName = "Map_01";
    public static int difficultyLevel = 1;
    public static int CarId = 0;

    public static int totalScore = 0;

    public static void Reset()
    {
        selectedMapName = "None";
        difficultyLevel = 1;
        CarId = 0;
        totalScore = 0;
    }
}