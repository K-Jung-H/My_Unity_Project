using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "CarDataTable", menuName = "Game/Car Data Table")]
public class CarDataTable : ScriptableObject
{
    [Header("Prefabs List")]
    public List<GameObject> carPrefabs;

    public GameObject GetCarPrefab(int index)
    {
        if (index >= 0 && index < carPrefabs.Count)
        {
            return carPrefabs[index];
        }
        
        Debug.LogWarning($"Car Index {index} is out of range. Returning default (0).");
        return carPrefabs.Count > 0 ? carPrefabs[0] : null;
    }
    
    public int Count => carPrefabs.Count;
}