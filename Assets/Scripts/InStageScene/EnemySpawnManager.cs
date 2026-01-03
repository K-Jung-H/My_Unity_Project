using UnityEngine;
using System.Collections.Generic;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("References")]
    public DynamicChunkManager chunkManager;
    public Transform player;
    public Transform globalEnemyRoot;

    [Header("Settings")]
    public GameObject[] enemyPrefabs;
    public int enemiesPerChunk = 3;
    public float enemyCullDistance = 150f;

    private HashSet<Vector2Int> visitedChunks = new HashSet<Vector2Int>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    void Start()
    {
        if (chunkManager != null)
        {
            chunkManager.OnChunkLoaded += HandleChunkLoaded;
        }

        StartCoroutine(CullEnemiesRoutine());
    }

    void OnDestroy()
    {
        if (chunkManager != null)
        {
            chunkManager.OnChunkLoaded -= HandleChunkLoaded;
        }
    }

    private void HandleChunkLoaded(ChunkController chunk, Vector2Int coord)
    {
        if (visitedChunks.Contains(coord)) return;

        visitedChunks.Add(coord);
        SpawnEnemiesInChunk(chunk);
    }

    private void SpawnEnemiesInChunk(ChunkController chunk)
    {
        List<Transform> points = chunk.GetSpawnPoints();
        if (points == null || points.Count == 0) return;

        List<Transform> availablePoints = new List<Transform>(points);
        int spawnCount = Mathf.Min(enemiesPerChunk, availablePoints.Count);

        for (int i = 0; i < spawnCount; i++)
        {
            int rndIndex = Random.Range(0, availablePoints.Count);
            Transform targetPoint = availablePoints[rndIndex];
            availablePoints.RemoveAt(rndIndex);

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            GameObject enemy = Instantiate(prefab, targetPoint.position, targetPoint.rotation, globalEnemyRoot);

            activeEnemies.Add(enemy);
        }
    }

    System.Collections.IEnumerator CullEnemiesRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1.0f);

        while (true)
        {
            yield return wait;

            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                GameObject enemy = activeEnemies[i];

                if (enemy == null)
                {
                    activeEnemies.RemoveAt(i);
                    continue;
                }

                if (Vector3.Distance(player.position, enemy.transform.position) > enemyCullDistance)
                {
                    Destroy(enemy);
                    activeEnemies.RemoveAt(i);
                }
            }
        }
    }
}