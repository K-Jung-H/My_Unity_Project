using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DifficultyDataTable", menuName = "Game/Difficulty Data Table")]
public class DifficultyDataTable : ScriptableObject
{
    [Header("Profiles List")]
    public List<DifficultyProfile> profiles;

    public DifficultyProfile GetProfile(int index)
    {
        if (index >= 0 && index < profiles.Count)
        {
            return profiles[index];
        }
        
        Debug.LogWarning($"Difficulty Index {index} is out of range. Returning default (0).");
        return profiles.Count > 0 ? profiles[0] : null;
    }

    public int Count => profiles.Count;
}