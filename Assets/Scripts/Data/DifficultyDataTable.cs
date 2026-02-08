using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DifficultyDataTable", menuName = "Game/Difficulty Data Table")]
public class DifficultyDataTable : ScriptableObject
{
    [Header("Profiles List")]
    public List<DifficultyProfile> profiles;
    public int Count => profiles.Count;

    public DifficultyProfile GetProfile(int index)
    {
        if (index >= 0 && index < profiles.Count)
        {
            return profiles[index];
        }
        
        Debug.LogWarning($"Difficulty Index {index} is out of range. Returning default (0).");
        return profiles.Count > 0 ? profiles[0] : null;
    }


    public GameObject FindEnemyPrefabByID(string enemyID)
    {
        if (profiles == null) return null;

        foreach (var profile in profiles)
        {
            if (profile == null || profile.enemyConfigs == null) continue;

            foreach (var config in profile.enemyConfigs)
            {
                if (config.enemyName == enemyID)
                {
                    return config.prefab;
                }
            }
        }
        
         Debug.LogWarning($"[DifficultyDataTable] Cannot find enemy prefab for ID: {enemyID}");
        return null;
    }
}