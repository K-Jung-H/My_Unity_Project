using UnityEngine;
using System.Collections.Generic;

public class WorldObjectDataManager : MonoBehaviour
{
    public static WorldObjectDataManager Instance;

    private Dictionary<Vector2Int, HashSet<int>> destructionData = new Dictionary<Vector2Int, HashSet<int>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterDestruction(Vector2Int chunkCoord, int propIndex)
    {
        if (!destructionData.ContainsKey(chunkCoord))
        {
            destructionData[chunkCoord] = new HashSet<int>();
        }

        destructionData[chunkCoord].Add(propIndex);
    }

    public bool IsPropDestroyed(Vector2Int chunkCoord, int propIndex)
    {
        if (destructionData.TryGetValue(chunkCoord, out HashSet<int> destroyedSet))
        {
            return destroyedSet.Contains(propIndex);
        }
        return false;
    }
}