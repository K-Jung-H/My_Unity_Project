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
    private Queue<ChunkController> chunkPool = new Queue<ChunkController>();

    private GameObject[] cachedPlayers;
    private float playerSearchTimer = 0f;
    private Transform mainPlayerTransform;

    void Start()
    {
        InitializeGameSequence();
    }

    private void InitializeGameSequence()
    {
        Vector2Int startCoord = Vector2Int.zero;
        ChunkController startChunk = LoadChunkAt(startCoord);

        Transform spawnPoint = startChunk.GetRandomPlayerSpawnPoint();

        if (playerManager != null)
        {
            GameObject playerObj = playerManager.CreatePlayer(spawnPoint);
            if (playerObj != null)
            {
                mainPlayerTransform = playerObj.transform;
            }
        }

        RefreshPlayerList();
        UpdateChunks();
    }

    void Update()
    {
        if (mainPlayerTransform == null) return;

        playerSearchTimer += Time.deltaTime;
        if (playerSearchTimer > 1.0f)
        {
            RefreshPlayerList();
            playerSearchTimer = 0f;
        }

        UpdateChunks();
    }

    ChunkController LoadChunkAt(Vector2Int coord)
    {
        ChunkController newChunk = GetChunkFromPool(coord);
        newChunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
        newChunk.Setup(coord);

        if (!activeChunks.ContainsKey(coord))
        {
            activeChunks.Add(coord, newChunk);
        }

        OnChunkLoaded?.Invoke(newChunk, coord);
        newChunk.SetPhysicsState(true);

        return newChunk;
    }

    void RefreshPlayerList()
    {
        cachedPlayers = GameObject.FindGameObjectsWithTag("Player");
    }

    void UpdateChunks()
    {
        if (cachedPlayers == null || cachedPlayers.Length == 0) return;

        HashSet<Vector2Int> requiredChunks = new HashSet<Vector2Int>();

        foreach (var playerObj in cachedPlayers)
        {
            if (playerObj == null || !playerObj.activeInHierarchy) continue;

            Vector2Int center = GetChunkCoord(playerObj.transform.position);

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
                OnChunkUnloaded?.Invoke(coord);
                ReturnChunk(activeChunks[coord]);
                activeChunks.Remove(coord);
            }
        }

        bool isMapChanged = false;

        foreach (var coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                ChunkController newChunk = GetChunkFromPool(coord);
                newChunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);
                newChunk.Setup(coord);

                activeChunks.Add(coord, newChunk);
                OnChunkLoaded?.Invoke(newChunk, coord);
                isMapChanged = true;
            }

            bool enablePhysics = false;
            foreach (var playerObj in cachedPlayers)
            {
                if (playerObj == null) continue;
                Vector2Int playerChunk = GetChunkCoord(playerObj.transform.position);
                int dist = GetChebyshevDistance(coord, playerChunk);
                
                if (dist <= physicsDistance)
                {
                    enablePhysics = true;
                    break;
                }
            }
            activeChunks[coord].SetPhysicsState(enablePhysics);
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