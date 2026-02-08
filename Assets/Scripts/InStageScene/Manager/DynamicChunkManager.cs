using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicChunkManager : MonoBehaviour
{
    public static DynamicChunkManager Instance { get; private set; }

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

    [Header("Pooling")]
    public int minPoolPerType = 10;
    public float poolMultiplier = 1.5f;

    [Header("Biome Settings")]
    public List<BiomeType> activeBiomes = new List<BiomeType>();
    public float biomeScale = 0.05f;
    public float biomeInfluence = 10f;
    
    [Header("Seed Settings")]
    public int worldSeed = 0;
    public bool autoRandomizeSeed = true;

    private float noiseOffsetX;
    private float noiseOffsetY;

    private Dictionary<BiomeType, List<ChunkData>> biomeChunksCache = new Dictionary<BiomeType, List<ChunkData>>();
    private List<ChunkData> currentSessionChunks = new List<ChunkData>();
    
    private Dictionary<Vector2Int, ChunkController> activeChunks = new Dictionary<Vector2Int, ChunkController>();
    private Dictionary<string, Queue<ChunkController>> chunkPool = new Dictionary<string, Queue<ChunkController>>();
    
    private List<Transform> trackedPlayers = new List<Transform>();
    private Queue<ChunkController> disableQueue = new Queue<ChunkController>();

    private bool isInitialized = false;
    private Coroutine updateRoutine;

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
        StopUpdateRoutine();
        CleanupAllChunks();
        
        isInitialized = false;
        trackedPlayers.Clear();
        activeChunks.Clear();
        chunkPool.Clear();
        disableQueue.Clear();

        if (globalChunkTable != null) globalChunkTable.Initialize();
        if (WorldObjectDataManager.Instance != null) WorldObjectDataManager.Instance.Initialize();

        if (GameData.activeBiomes != null && GameData.activeBiomes.Count > 0)
        {
            this.activeBiomes = new List<BiomeType>(GameData.activeBiomes);
            Debug.Log($"[DynamicChunkManager] Loaded Biomes from GameData: {string.Join(", ", this.activeBiomes)}");
        }
        else
        {
            Debug.LogWarning("[DynamicChunkManager] No Biomes in GameData. Using Inspector settings.");
        }

        if (autoRandomizeSeed)
        {
            worldSeed = UnityEngine.Random.Range(-10000, 10000);
        }
        
        System.Random prng = new System.Random(worldSeed);
        noiseOffsetX = prng.Next(-100000, 100000);
        noiseOffsetY = prng.Next(-100000, 100000);

        ApplyMapSettings();
        InitializePool();
        InitializeGameSequence();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            StopUpdateRoutine();
            Instance = null;
        }
    }

    private void StopUpdateRoutine()
    {
        if (updateRoutine != null)
        {
            StopCoroutine(updateRoutine);
            updateRoutine = null;
        }
    }

    private void CleanupAllChunks()
    {
        foreach (var chunk in activeChunks.Values) if (chunk != null) Destroy(chunk.gameObject);
        foreach (var queue in chunkPool.Values) foreach (var chunk in queue) if (chunk != null) Destroy(chunk.gameObject);
        activeChunks.Clear();
        chunkPool.Clear();
        disableQueue.Clear();
    }

    private void ApplyMapSettings()
    {
        currentSessionChunks.Clear();
        biomeChunksCache.Clear();

        if (globalChunkTable == null) return;

        foreach (var chunk in globalChunkTable.chunkList)
        {
            if (activeBiomes.Contains(chunk.biomeType))
            {
                currentSessionChunks.Add(chunk);

                if (!biomeChunksCache.ContainsKey(chunk.biomeType))
                {
                    biomeChunksCache[chunk.biomeType] = new List<ChunkData>();
                }
                biomeChunksCache[chunk.biomeType].Add(chunk);
            }
        }

        if (currentSessionChunks.Count == 0)
        {
            Debug.LogWarning("Activated biome chunks are not available! Please check the settings.");
        }
    }

    private void InitializePool()
    {
        int totalNeeded = (renderDistance * 2 + 1) * (renderDistance * 2 + 1);
        float totalWeight = 0f;
        foreach (var data in currentSessionChunks) totalWeight += data.spawnWeight;

        foreach (var data in currentSessionChunks)
        {
            if (data == null || data.chunkPrefab == null) continue;
            string key = data.chunkName;
            if (!chunkPool.ContainsKey(key)) chunkPool[key] = new Queue<ChunkController>();

            float ratio = data.spawnWeight / totalWeight;
            int count = Mathf.Max(minPoolPerType, Mathf.CeilToInt(totalNeeded * ratio * poolMultiplier));

            for (int i = 0; i < count; i++) CreateNewChunkInstance(data);
        }
    }

    private ChunkController CreateNewChunkInstance(ChunkData data)
    {
        GameObject obj = Instantiate(data.chunkPrefab, environmentRoot);
        ChunkController chunk = obj.GetComponent<ChunkController>();
        chunk.originalChunkName = data.chunkName;
        
        if (chunk.physicsRoot != null) chunk.physicsRoot.SetActive(false);
        if (chunk.propsRoot != null) chunk.propsRoot.SetActive(false);
        if (chunk.visualRoot != null) chunk.visualRoot.SetActive(false);
        
        obj.SetActive(false);
        chunkPool[data.chunkName].Enqueue(chunk);
        return chunk;
    }

    private ChunkData GetDeterministicChunkData(Vector2Int coord)
    {
        if (currentSessionChunks.Count == 0) return null;

        float xCoord = (coord.x * biomeScale) + noiseOffsetX;
        float yCoord = (coord.y * biomeScale) + noiseOffsetY;
        float noiseValue = Mathf.Clamp01(Mathf.PerlinNoise(xCoord, yCoord));

        int biomeIndex = Mathf.FloorToInt(noiseValue * activeBiomes.Count);
        biomeIndex = Mathf.Clamp(biomeIndex, 0, activeBiomes.Count - 1);
        BiomeType targetBiome = activeBiomes[biomeIndex];

        uint seed = (uint)(coord.x * 73856093 ^ coord.y * 19349663);
        seed ^= (uint)worldSeed;
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        
        double randomValue = (seed % 10000) / 10000.0;
        
        float totalWeight = 0f;
        List<float> adjustedWeights = new List<float>();

        for (int i = 0; i < currentSessionChunks.Count; i++)
        {
            ChunkData data = currentSessionChunks[i];
            float weight = data.spawnWeight;

            if (data.biomeType == targetBiome)
            {
                weight *= biomeInfluence; 
            }

            adjustedWeights.Add(weight);
            totalWeight += weight;
        }

        double targetValue = randomValue * totalWeight;
        for (int i = 0; i < currentSessionChunks.Count; i++)
        {
            if (targetValue <= adjustedWeights[i])
            {
                return currentSessionChunks[i];
            }
            targetValue -= adjustedWeights[i];
        }

        return currentSessionChunks[currentSessionChunks.Count - 1];
    }

    private ChunkController GetChunkFromPool(Vector2Int coord)
    {
        ChunkData selectedData = GetDeterministicChunkData(coord);
        string key = selectedData.chunkName;
        ChunkController chunk = null;

        if (chunkPool.ContainsKey(key) && chunkPool[key].Count > 0)
            chunk = chunkPool[key].Dequeue();
        else
            chunk = CreateNewChunkInstance(selectedData);

        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        
        if (chunk.physicsRoot != null) chunk.physicsRoot.SetActive(false);
        if (chunk.propsRoot != null) chunk.propsRoot.SetActive(false);
        if (chunk.visualRoot != null) chunk.visualRoot.SetActive(false);

        chunk.gameObject.SetActive(true); 
        return chunk;
    }

    private void ReturnChunkToPool(Vector2Int coord, ChunkController chunk)
    {
        if (chunk == null) return;

        if (chunk.physicsRoot != null) chunk.physicsRoot.SetActive(false);
        if (chunk.propsRoot != null) chunk.propsRoot.SetActive(false);
        if (chunk.visualRoot != null) chunk.visualRoot.SetActive(false);

        chunk.transform.position = new Vector3(0, -5000, 0);

        disableQueue.Enqueue(chunk);

        string key = chunk.originalChunkName;
        chunkPool[key].Enqueue(chunk);
    }

    public Transform GetMainSpawnPoint() => GetRandomActiveSpawnPoint();
    public Transform GetRandomActiveSpawnPoint() 
    {
        if (activeChunks.Count == 0 && currentSessionChunks.Count > 0)
        {
            ChunkController startChunk = GetChunkFromPool(Vector2Int.zero);
            activeChunks.Add(Vector2Int.zero, startChunk);
            startChunk.StartCoroutine(startChunk.SetupRoutine(Vector2Int.zero)); 
            return startChunk.GetRandomPlayerSpawnPoint();
        }
        
        var list = new List<ChunkController>(activeChunks.Values);
        if (list.Count > 0) return list[UnityEngine.Random.Range(0, list.Count)].GetRandomPlayerSpawnPoint();
        return environmentRoot;
    }

    private void InitializeGameSequence()
    {
        if (currentSessionChunks.Count == 0) return;
        isInitialized = true;
        StopUpdateRoutine();
        updateRoutine = StartCoroutine(UpdateChunksRoutine());
        
        if(activeChunks.Count == 0) GetMainSpawnPoint();
    }

    public void RegisterPlayer(Transform p) { if (!trackedPlayers.Contains(p)) trackedPlayers.Add(p); }
    public void UnregisterPlayer(Transform p) { trackedPlayers.Remove(p); }

    IEnumerator UpdateChunksRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);
        
        HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();
        List<Vector2Int> toRemove = new List<Vector2Int>();
        Vector2Int lastPlayerChunkCoord = new Vector2Int(int.MinValue, int.MinValue);

        while (true)
        {
            if (!isInitialized || trackedPlayers.Count == 0) { yield return wait; continue; }

            Vector2Int currentPlayerChunkCoord = GetChunkCoord(trackedPlayers[0].position);

            if (currentPlayerChunkCoord != lastPlayerChunkCoord)
            {
                lastPlayerChunkCoord = currentPlayerChunkCoord;
                requiredChunks.Clear();
                toRemove.Clear();

                foreach (var playerT in trackedPlayers)
                {
                    if (playerT == null) continue;
                    Vector2Int center = GetChunkCoord(playerT.position);
                    for (int x = -renderDistance; x <= renderDistance; x++)
                    {
                        for (int y = -renderDistance; y <= renderDistance; y++)
                            requiredChunks.Add(new Vector2Int(center.x + x, center.y + y));
                    }
                }

                foreach (var coord in activeChunks.Keys) if (!requiredChunks.Contains(coord)) toRemove.Add(coord);

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
                foreach (var coord in requiredChunks)
                {
                    if (!activeChunks.ContainsKey(coord))
                    {
                        ChunkController newChunk = GetChunkFromPool(coord);
                        activeChunks.Add(coord, newChunk);
                        
                        StartCoroutine(newChunk.SetupRoutine(coord));
                        OnChunkLoaded?.Invoke(newChunk, coord);
                        
                        if (++processedCount >= maxChunksPerFrame)
                        {
                            processedCount = 0;
                            yield return null;
                        }
                    }

                    if (activeChunks.TryGetValue(coord, out ChunkController chunk))
                    {
                        if (chunk.IsSetupDone)
                        {
                            bool enablePhysics = false;
                            foreach (var playerT in trackedPlayers)
                            {
                                if (playerT != null && GetChebyshevDistance(coord, GetChunkCoord(playerT.position)) <= physicsDistance)
                                {
                                    enablePhysics = true;
                                    break;
                                }
                            }
                            chunk.SetPhysicsState(enablePhysics);
                        }
                    }
                }
            }

            if (disableQueue.Count > 0)
            {
                ChunkController chunkToDisable = disableQueue.Dequeue();
                if (chunkToDisable != null && chunkToDisable.gameObject.activeSelf && chunkToDisable.transform.position.y < -4000)
                {
                    chunkToDisable.gameObject.SetActive(false);
                }
            }

            yield return wait;
        }
    }

    public IEnumerable<ChunkController> GetActiveChunks() => activeChunks.Values;

    public ChunkController GetActiveChunk(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out ChunkController chunk))
        {
            return chunk;
        }
        return null;
    }

    public ChunkController GetChunkAtPosition(Vector3 worldPos)
    {
        Vector2Int coord = GetChunkCoord(worldPos);
        return GetActiveChunk(coord);
    }

    public Vector2Int GetChunkCoord(Vector3 pos) => new Vector2Int(Mathf.FloorToInt(pos.x / chunkSize), Mathf.FloorToInt(pos.z / chunkSize));
    
    int GetChebyshevDistance(Vector2Int a, Vector2Int b) => Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
}