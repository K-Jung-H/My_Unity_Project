using System;
using System.Collections.Generic;
using UnityEngine;

public class DynamicChunkManager : MonoBehaviour
{
    [Header("Managers")]
    public PlayerManager playerManager;

    [Header("References")]
    public Transform environmentRoot;
    public ChunkController[] chunkPrefabs;

    public event Action<ChunkController, Vector2Int> OnChunkLoaded;
    public event Action<Vector2Int> OnChunkUnloaded;

    [Header("Settings")]
    public float chunkSize = 300f;
    [Range(3, 16)]
    public int renderDistance = 3;
    [Range(1, 5)]
    public int physicsDistance = 1;

    private Dictionary<Vector2Int, ChunkController> activeChunks = new Dictionary<Vector2Int, ChunkController>();
    private List<Transform> trackedPlayers = new List<Transform>();

    private ChunkController[] defaultChunkPrefabs;
    private bool isInitialized = false;

    void Awake()
    {
        if (chunkPrefabs != null)
        {
            defaultChunkPrefabs = (ChunkController[])chunkPrefabs.Clone();
        }
    }

    public void Initialize()
    {
        ApplyMapSettings();
        InitializeGameSequence();
    }

    public Transform GetMainSpawnPoint()
    {
        if (activeChunks.ContainsKey(Vector2Int.zero))
        {
            return activeChunks[Vector2Int.zero].GetRandomPlayerSpawnPoint();
        }

        ChunkController startChunk = CreateChunk(Vector2Int.zero);
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

        int randomIndex = UnityEngine.Random.Range(0, loadedChunks.Count);
        ChunkController randomChunk = loadedChunks[randomIndex];

        return randomChunk.GetRandomPlayerSpawnPoint();
    }

    public void RegisterPlayer(Transform playerTransform)
    {
        if (!trackedPlayers.Contains(playerTransform))
        {
            trackedPlayers.Add(playerTransform);
            UpdateChunks();
        }
    }

    public void UnregisterPlayer(Transform playerTransform)
    {
        if (trackedPlayers.Contains(playerTransform))
        {
            trackedPlayers.Remove(playerTransform);
        }
    }

    private void ApplyMapSettings()
    {
        if (GameData.gameMode == GameMode.Default)
        {
            if (defaultChunkPrefabs != null && defaultChunkPrefabs.Length > 0)
                chunkPrefabs = defaultChunkPrefabs;
            else
                Debug.LogError("[DynamicChunkManager] Default Mode chunks missing.");
        }
        else
        {
            if (GameData.selectedChunks != null && GameData.selectedChunks.Count > 0)
            {
                List<ChunkController> selectedControllers = new List<ChunkController>();
                foreach (ChunkData data in GameData.selectedChunks)
                {
                    if (data != null && data.chunkPrefab != null)
                    {
                        ChunkController controller = data.chunkPrefab.GetComponent<ChunkController>();
                        if (controller != null) selectedControllers.Add(controller);
                    }
                }
                chunkPrefabs = selectedControllers.Count > 0 ? selectedControllers.ToArray() : defaultChunkPrefabs;
            }
            else
            {
                chunkPrefabs = defaultChunkPrefabs;
            }
        }
    }

    private void InitializeGameSequence()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError("[Critical] No chunk prefabs available.");
            return;
        }

        Transform spawnPoint = GetMainSpawnPoint();

        if (playerManager != null)
        {
            GameObject playerObj = playerManager.CreatePlayer(spawnPoint, true, 0);
            if (playerObj != null)
            {
                RegisterPlayer(playerObj.transform);
            }
        }

        isInitialized = true;
        UpdateChunks();
    }

    void Update()
    {
        if (!isInitialized || trackedPlayers.Count == 0) return;
        UpdateChunks();
    }

    void UpdateChunks()
    {
        if (trackedPlayers.Count == 0) return;

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

        List<Vector2Int> currentActiveCoords = new List<Vector2Int>(activeChunks.Keys);
        foreach (var coord in currentActiveCoords)
        {
            if (!requiredChunks.Contains(coord))
            {
                if (activeChunks.TryGetValue(coord, out ChunkController chunkToRemove))
                {
                    OnChunkUnloaded?.Invoke(coord);
                    Destroy(chunkToRemove.gameObject);
                    activeChunks.Remove(coord);
                }
            }
        }

        bool isMapChanged = false;

        foreach (var coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                ChunkController newChunk = CreateChunk(coord);
                activeChunks.Add(coord, newChunk);
                OnChunkLoaded?.Invoke(newChunk, coord);
                isMapChanged = true;
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
    }

    ChunkController CreateChunk(Vector2Int coord)
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
            chunkPrefabs = defaultChunkPrefabs;

        ChunkController prefab = chunkPrefabs[UnityEngine.Random.Range(0, chunkPrefabs.Length)];

        Vector3 spawnPos = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        
        GameObject obj = Instantiate(prefab.gameObject, spawnPos, Quaternion.identity, environmentRoot);
        
        ChunkController chunk = obj.GetComponent<ChunkController>();
        chunk.Setup(coord);

        return chunk;
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