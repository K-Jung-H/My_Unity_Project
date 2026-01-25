using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicChunkManager : MonoBehaviour
{
    [Header("Managers")]
    public PlayerManager playerManager;
    public ChunkDataTable globalChunkTable;

    [Header("References")]
    public Transform environmentRoot;

    public event Action<ChunkController, Vector2Int> OnChunkLoaded;
    public event Action<Vector2Int> OnChunkUnloaded;

    [Header("Settings")]
    public float chunkSize = 300f;
    [Range(3, 16)] public int renderDistance = 3;
    [Range(1, 5)] public int physicsDistance = 1;
    [Range(1, 10)] public int maxChunksPerFrame = 1;

    private List<ChunkData> currentSessionChunks = new List<ChunkData>();

    private Dictionary<Vector2Int, ChunkController> activeChunks = new Dictionary<Vector2Int, ChunkController>();
    private Dictionary<string, Queue<ChunkController>> chunkPool = new Dictionary<string, Queue<ChunkController>>();

    private List<Transform> trackedPlayers = new List<Transform>();
    private bool isInitialized = false;
    private Coroutine updateRoutine;

    void Awake()
    {
        if (globalChunkTable != null) globalChunkTable.Initialize();
    }

    public void Initialize()
    {
        ApplyMapSettings();
        InitializePool();
        InitializeGameSequence();
    }

    private void ApplyMapSettings()
    {
        currentSessionChunks.Clear();

        if (GameData.gameMode == GameMode.Default)
        {
            currentSessionChunks.AddRange(globalChunkTable.chunkList);
        }
        else
        {
            if (GameData.selectedChunks != null && GameData.selectedChunks.Count > 0)
                currentSessionChunks.AddRange(GameData.selectedChunks);
            else
                currentSessionChunks.AddRange(globalChunkTable.chunkList);
        }
    }

    private void InitializePool()
    {
        chunkPool.Clear();

        int maxVisibleChunks = (renderDistance * 2 + 1) * (renderDistance * 2 + 1);
        int safetyBuffer = 4; 

        float totalWeight = 0f;
        foreach (var data in currentSessionChunks) totalWeight += data.spawnWeight;

        foreach (var data in currentSessionChunks)
        {
            if (data == null || data.chunkPrefab == null) continue;

            string key = data.chunkName;
            if (!chunkPool.ContainsKey(key))
            {
                chunkPool[key] = new Queue<ChunkController>();
            }

            float ratio = data.spawnWeight / totalWeight;
            int spawnCount = Mathf.CeilToInt(maxVisibleChunks * ratio) + safetyBuffer;
            spawnCount = Mathf.Clamp(spawnCount, 2, 25); 

            for (int i = 0; i < spawnCount; i++)
            {
                CreateNewChunkInstance(data);
            }
        }
    }

    private ChunkController CreateNewChunkInstance(ChunkData data)
    {
        GameObject obj = Instantiate(data.chunkPrefab, environmentRoot);
        ChunkController chunk = obj.GetComponent<ChunkController>();
        
        chunk.originalChunkName = data.chunkName; 
        
        obj.SetActive(false);
        chunkPool[data.chunkName].Enqueue(chunk);
        
        return chunk;
    }

    private ChunkData GetRandomChunkData()
    {
        float totalWeight = 0f;
        foreach (var data in currentSessionChunks)
        {
            if (data != null) totalWeight += data.spawnWeight;
        }

        float randomValue = UnityEngine.Random.Range(0, totalWeight);

        foreach (var data in currentSessionChunks)
        {
            if (data == null) continue;
            
            if (randomValue <= data.spawnWeight) return data;
            
            randomValue -= data.spawnWeight;
        }

        return currentSessionChunks[currentSessionChunks.Count - 1];
    }

    private ChunkController GetChunkFromPool(Vector2Int coord)
    {
        ChunkData selectedData = GetRandomChunkData();
        string key = selectedData.chunkName;

        ChunkController chunk = null;

        if (chunkPool.ContainsKey(key) && chunkPool[key].Count > 0)
        {
            chunk = chunkPool[key].Dequeue();
        }
        else
        {
            chunk = CreateNewChunkInstance(selectedData);
            chunk = chunkPool[key].Dequeue(); 
        }

        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        chunk.gameObject.SetActive(true);
        chunk.Setup(coord); 

        return chunk;
    }

    private void ReturnChunkToPool(Vector2Int coord, ChunkController chunk)
    {
        chunk.gameObject.SetActive(false);
        
        string key = chunk.originalChunkName; 
        
        if (!chunkPool.ContainsKey(key))
        {
            chunkPool[key] = new Queue<ChunkController>();
        }
        
        chunkPool[key].Enqueue(chunk);
    }

    public Transform GetMainSpawnPoint()
    {
        if (activeChunks.ContainsKey(Vector2Int.zero))
            return activeChunks[Vector2Int.zero].GetRandomPlayerSpawnPoint();

        ChunkController startChunk = GetChunkFromPool(Vector2Int.zero);
        activeChunks.Add(Vector2Int.zero, startChunk);
        
        OnChunkLoaded?.Invoke(startChunk, Vector2Int.zero);
        startChunk.SetPhysicsState(true);

        return startChunk.GetRandomPlayerSpawnPoint();
    }

    public Transform GetRandomActiveSpawnPoint()
    {
        if (activeChunks.Count == 0)
        {
            return GetMainSpawnPoint();
        }

        List<ChunkController> loadedChunks = new List<ChunkController>(activeChunks.Values);
        
        if (loadedChunks.Count == 0) return GetMainSpawnPoint();

        int randomIndex = UnityEngine.Random.Range(0, loadedChunks.Count);
        ChunkController randomChunk = loadedChunks[randomIndex];

        return randomChunk.GetRandomPlayerSpawnPoint();
    }

    private void InitializeGameSequence()
    {
        if (currentSessionChunks.Count == 0) return;

        Transform spawnPoint = GetMainSpawnPoint();

        // if (playerManager != null)
        // {
        //     GameObject playerObj = playerManager.CreatePlayer(spawnPoint, true, 0);
        //     if (playerObj != null) RegisterPlayer(playerObj.transform);
        // }

        isInitialized = true;
        
        if(updateRoutine != null) StopCoroutine(updateRoutine);
        updateRoutine = StartCoroutine(UpdateChunksRoutine());
    }

    public void RegisterPlayer(Transform playerTransform)
    {
        if (!trackedPlayers.Contains(playerTransform)) trackedPlayers.Add(playerTransform);
    }

    public void UnregisterPlayer(Transform playerTransform)
    {
        if (trackedPlayers.Contains(playerTransform))
        {
            trackedPlayers.Remove(playerTransform);
        }
    }


    IEnumerator UpdateChunksRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            if (!isInitialized || trackedPlayers.Count == 0)
            {
                yield return wait;
                continue;
            }

            HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
            foreach (var playerT in trackedPlayers)
            {
                if (playerT == null || !playerT.gameObject.activeInHierarchy) continue;

                Vector2Int center = GetChunkCoord(playerT.position);
                for (int x = -renderDistance; x <= renderDistance; x++)
                {
                    for (int y = -renderDistance; y <= renderDistance; y++)
                    {
                        requiredChunks.Add(new Vector2Int(center.x + x, center.y + y));
                    }
                }
            }

            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var coord in activeChunks.Keys)
            {
                if (!requiredChunks.Contains(coord)) toRemove.Add(coord);
            }

            foreach (var coord in toRemove)
            {
                if (activeChunks.TryGetValue(coord, out ChunkController chunk))
                {
                    OnChunkUnloaded?.Invoke(coord);
                    ReturnChunkToPool(coord, chunk);
                    activeChunks.Remove(coord);
                }
            }

            int processedCount = 0;
            bool isMapChanged = false;

            foreach (var coord in requiredChunks)
            {
                if (!activeChunks.ContainsKey(coord))
                {
                    ChunkController newChunk = GetChunkFromPool(coord);
                    activeChunks.Add(coord, newChunk);
                    OnChunkLoaded?.Invoke(newChunk, coord);
                    isMapChanged = true;

                    processedCount++;

                    if (processedCount >= maxChunksPerFrame)
                    {
                        processedCount = 0;
                        yield return null; 
                    }
                }

                bool enablePhysics = false;
                foreach (var playerT in trackedPlayers)
                {
                    if (playerT == null) continue;
                    Vector2Int playerChunk = GetChunkCoord(playerT.position);
                    int dist = GetChebyshevDistance(coord, playerChunk);

                    if (dist <= physicsDistance)
                    {
                        enablePhysics = true;
                        break;
                    }
                }

                if (activeChunks.TryGetValue(coord, out ChunkController chunk))
                {
                    chunk.SetPhysicsState(enablePhysics);
                }
            }

            if (isMapChanged)
            {
                RefreshAllLinks();
            }

            yield return wait;
        }
    }

    void RefreshAllLinks()
    {
        foreach (var kvp in activeChunks)
        {
            if (kvp.Value != null && kvp.Value.gameObject.activeInHierarchy)
            {
                kvp.Value.RefreshNavMeshLinks();
            }
        }
    }

    public IEnumerable<ChunkController> GetActiveChunks()
    {
        return activeChunks.Values;
    }


    Vector2Int GetChunkCoord(Vector3 pos)
    {
        return new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));
    }

    int GetChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
}