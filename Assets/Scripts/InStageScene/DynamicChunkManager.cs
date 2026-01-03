using System;
using System.Collections.Generic;
using UnityEngine;

public class DynamicChunkManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;
    public Transform environmentRoot;
    public ChunkController[] chunkPrefabs;

    public event Action<ChunkController, Vector2Int> OnChunkLoaded;
    public event Action<Vector2Int> OnChunkUnloaded;

    [Header("Settings")]
    public float chunkSize = 64f;
    [Range(3, 16)]
    public int renderDistance = 3;
    [Range(1, 5)]
    public int physicsDistance = 1;

    private Vector2Int currentCenterChunk;
    private Dictionary<Vector2Int, ChunkController> activeChunks = new Dictionary<Vector2Int, ChunkController>();
    private Queue<ChunkController> chunkPool = new Queue<ChunkController>();


    void Start()
    {
        currentCenterChunk = GetChunkCoord(player.position);
        UpdateChunks();
    }

    void Update()
    {
        Vector2Int playerChunk = GetChunkCoord(player.position);

        if (playerChunk != currentCenterChunk)
        {
            currentCenterChunk = playerChunk;
            UpdateChunks();
        }
    }


    void UpdateChunks()
    {
        List<Vector2Int> coordsToRemove = new List<Vector2Int>();

        foreach (var kvp in activeChunks)
        {
            if (GetChebyshevDistance(kvp.Key, currentCenterChunk) > renderDistance)
            {
                coordsToRemove.Add(kvp.Key);
            }
        }

        foreach (var coord in coordsToRemove)
        {
            OnChunkUnloaded?.Invoke(coord);
            ReturnChunk(activeChunks[coord]);
            activeChunks.Remove(coord);
        }

        bool isMapChanged = false;

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                Vector2Int targetCoord = new Vector2Int(currentCenterChunk.x + x, currentCenterChunk.y + y);
                int dist = GetChebyshevDistance(targetCoord, currentCenterChunk);

                if (!activeChunks.ContainsKey(targetCoord))
                {
                    ChunkController newChunk = GetChunkFromPool(targetCoord);

                    newChunk.transform.position = new Vector3(targetCoord.x * chunkSize, 0, targetCoord.y * chunkSize);
                    newChunk.Setup(targetCoord);

                    activeChunks.Add(targetCoord, newChunk);
                    OnChunkLoaded?.Invoke(newChunk, targetCoord);

                    isMapChanged = true;
                }

                bool enablePhysics = dist <= physicsDistance;
                activeChunks[targetCoord].SetPhysicsState(enablePhysics);
            }
        }

        if (isMapChanged)
        {
            RefreshAllLinks();
        }
    }

    void RefreshAllLinks()
    {
        StopCoroutine("RefreshRoutine");
        StartCoroutine("RefreshRoutine");
    }

    System.Collections.IEnumerator RefreshRoutine()
    {
        yield return null;

        foreach (var kvp in activeChunks)
        {
            if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
            {
                kvp.Value.RefreshNavMeshLinks();
            }
        }
    }

    ChunkController GetChunkFromPool(Vector2Int coord)
    {
        ChunkController chunk;

        if (chunkPool.Count > 0)
        {
            chunk = chunkPool.Dequeue();
            chunk.gameObject.SetActive(true);
        }
        else
        {
            ChunkController prefab = chunkPrefabs[UnityEngine.Random.Range(0, chunkPrefabs.Length)];
            GameObject obj = Instantiate(prefab.gameObject, environmentRoot);
            chunk = obj.GetComponent<ChunkController>();
        }
        return chunk;
    }

    void ReturnChunk(ChunkController chunk)
    {
        chunk.gameObject.SetActive(false);
        chunkPool.Enqueue(chunk);
    }

    Vector2Int GetChunkCoord(Vector3 pos)
    {
        // Corner-based chunk calculation
        return new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));

        // Center-based chunk calculation
        //return new Vector2Int(Mathf.RoundToInt(pos.x / chunkSize), Mathf.RoundToInt(pos.z / chunkSize));
    }

    int GetChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}