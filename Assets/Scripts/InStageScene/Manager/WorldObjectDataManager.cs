using UnityEngine;
using System.Collections.Generic;

public class WorldObjectDataManager : MonoBehaviour
{
    public static WorldObjectDataManager Instance;

    private Dictionary<Vector2Int, HashSet<int>> destructionData = new Dictionary<Vector2Int, HashSet<int>>();

    private Dictionary<Vector2Int, List<DeadEnemyData>> deadEnemyData = new Dictionary<Vector2Int, List<DeadEnemyData>>();

    [System.Serializable]
    public struct DeadEnemyData
    {
        public string enemyID;
        public Vector3 localPosition;
        public Quaternion localRotation;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize()
    {
        destructionData.Clear();
        deadEnemyData.Clear();
        Debug.Log("WorldObjectDataManager Initialized");
    }

    public void RegisterDestruction(Vector2Int chunkCoord, int propIndex)
    {
        if (!destructionData.ContainsKey(chunkCoord))
            destructionData[chunkCoord] = new HashSet<int>();

        destructionData[chunkCoord].Add(propIndex);
    }

    public bool IsPropDestroyed(Vector2Int chunkCoord, int propIndex)
    {
        if (destructionData.TryGetValue(chunkCoord, out HashSet<int> destroyedSet))
            return destroyedSet.Contains(propIndex);
        return false;
    }

    public void RegisterDeadEnemy(Vector2Int chunkCoord, string enemyName, Vector3 worldPos, Quaternion worldRot, Transform chunkTransform)
    {
        if (!deadEnemyData.ContainsKey(chunkCoord))
            deadEnemyData[chunkCoord] = new List<DeadEnemyData>();

        DeadEnemyData data = new DeadEnemyData
        {
            enemyID = enemyName,
            localPosition = chunkTransform.InverseTransformPoint(worldPos),
            localRotation = Quaternion.Inverse(chunkTransform.rotation) * worldRot
        };

        deadEnemyData[chunkCoord].Add(data);
    }

    public List<DeadEnemyData> GetDeadEnemies(Vector2Int chunkCoord)
    {
        if (deadEnemyData.TryGetValue(chunkCoord, out List<DeadEnemyData> data))
        {
            return data;
        }
        return null;
    }
}