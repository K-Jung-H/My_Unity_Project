using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawnManager : MonoBehaviour
{
    public static EnemySpawnManager Instance { get; private set; }

    [Header("References")]
    public Transform globalEnemyRoot;

    [Header("Global Settings")]
    public int maxSpawnPerFrame = 2;

    [Header("Culling & Lifecycle")]
    public float activationDistance = 200f;
    public float deactivationDistance = 300f;
    public float enemyCullDistance = 500f;
    public float minWorldHeight = -50f;

    private float sqrActivationDist;
    private float sqrDeactivationDist;
    private float sqrCullDist;

    private class EnemyHandle
    {
        public GameObject gameObject;
        public Transform transform;
        public NavMeshAgent agent;
        public string typeName;

        public EnemyHandle(GameObject go, string name)
        {
            gameObject = go;
            transform = go.transform;
            agent = go.GetComponent<NavMeshAgent>();
            typeName = name;
        }
    }

    private List<EnemyHandle> activeEnemies = new List<EnemyHandle>();
    private Dictionary<string, int> enemyTypeCounts = new Dictionary<string, int>();

    private List<ChunkController> _cachedActiveChunks = new List<ChunkController>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        sqrActivationDist = activationDistance * activationDistance;
        sqrDeactivationDist = deactivationDistance * deactivationDistance;
        sqrCullDist = enemyCullDistance * enemyCullDistance;
    }   

    public void Initialize()
    {
        StopAllCoroutines();
        
        if (DynamicChunkManager.Instance != null)
        {
            DynamicChunkManager.Instance.OnChunkLoaded -= OnChunkLoaded;
            DynamicChunkManager.Instance.OnChunkLoaded += OnChunkLoaded;
            DynamicChunkManager.Instance.OnChunkUnloaded -= OnChunkUnloaded;
            DynamicChunkManager.Instance.OnChunkUnloaded += OnChunkUnloaded;
            
            _cachedActiveChunks = DynamicChunkManager.Instance.GetActiveChunks().ToList();
        }

        StartCoroutine(ManageEnemyLifecycleRoutine());
        Debug.Log("EnemySpawnManager Initialized");
    }

    private void OnDestroy()
    {
        if (DynamicChunkManager.Instance != null)
        {
            DynamicChunkManager.Instance.OnChunkLoaded -= OnChunkLoaded;
            DynamicChunkManager.Instance.OnChunkUnloaded -= OnChunkUnloaded;
        }
    }

    private void OnChunkLoaded(ChunkController chunk, Vector2Int coord) => _cachedActiveChunks.Add(chunk);
    private void OnChunkUnloaded(Vector2Int coord) 
    {
        _cachedActiveChunks.RemoveAll(c => c.Coord == coord);
    }

    IEnumerator ManageEnemyLifecycleRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (true)
        {
            yield return wait;

            if (PlayerManager.Instance != null && PlayerManager.Instance.AllActivePlayers.Count > 0)
            {
                ManageActiveEnemies();
                SpawnMissingEnemies();
            }
        }
    }

    private void ManageActiveEnemies()
    {
        var players = PlayerManager.Instance.AllActivePlayers;
        
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyHandle handle = activeEnemies[i];
            
            if (handle.gameObject == null) 
            { 
                activeEnemies.RemoveAt(i); 
                continue; 
            }

            Vector3 enemyPos = handle.transform.position;

            if (enemyPos.y < minWorldHeight)
            {
                RemoveEnemy(handle, i); continue;
            }

            float minSqrDist = GetSqrDistanceToNearestPlayer(enemyPos, players);

            if (minSqrDist > sqrCullDist)
            {
                RemoveEnemy(handle, i); continue;
            }

            if (handle.agent != null)
            {
                if (minSqrDist <= sqrActivationDist)
                {
                    if (!handle.agent.enabled) handle.agent.enabled = true;
                }
                else if (minSqrDist > sqrDeactivationDist)
                {
                    if (handle.agent.enabled) handle.agent.enabled = false;
                }
            }
        }
    }

    private float GetSqrDistanceToNearestPlayer(Vector3 position, List<CarController> players)
    {
        float minSqrDist = float.MaxValue;
        int count = players.Count;
        for (int i = 0; i < count; i++)
        {
            if (players[i] == null) continue;
            float d = (position - players[i].transform.position).sqrMagnitude;
            if (d < minSqrDist) minSqrDist = d;
        }
        return minSqrDist;
    }

    public void RetireEnemy(GameObject enemy)
    {
        int index = activeEnemies.FindIndex(x => x.gameObject == enemy);
        if (index != -1)
        {
            RemoveEnemy(activeEnemies[index], index, false);
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        int index = activeEnemies.FindIndex(x => x.gameObject == enemy);
        if (index != -1)
        {
            RemoveEnemy(activeEnemies[index], index);
        }
        else 
        {
            Destroy(enemy);
        }
    }

    private void RemoveEnemy(EnemyHandle handle, int index, bool destroyObject = true)
    {
        string typeName = handle.typeName;
        if (enemyTypeCounts.ContainsKey(typeName))
        {
            enemyTypeCounts[typeName]--;
            if (enemyTypeCounts[typeName] < 0) enemyTypeCounts[typeName] = 0;
        }

        if (destroyObject && handle.gameObject != null)
        {
            Destroy(handle.gameObject);
        }
        
        activeEnemies.RemoveAt(index);
    }

    private void SpawnMissingEnemies()
    {
        if (DifficultyManager.Instance == null) return;

        int currentMax = DifficultyManager.Instance.GetCurrentMaxEnemies();
        int currentCount = activeEnemies.Count;
        if (currentCount >= currentMax) return;

        if (_cachedActiveChunks.Count == 0) return;

        int needed = currentMax - currentCount;
        int spawnLoopCount = Mathf.Min(needed, maxSpawnPerFrame);

        for (int i = 0; i < spawnLoopCount; i++)
        {
            ChunkController randomChunk = _cachedActiveChunks[UnityEngine.Random.Range(0, _cachedActiveChunks.Count)];
            List<Transform> spawnPoints = randomChunk.GetEnemySpawnPoints();
            
            if (spawnPoints == null || spawnPoints.Count == 0) continue;
            
            EnemySpawnConfig config = DifficultyManager.Instance.PickEnemyToSpawn(enemyTypeCounts);
            if (config == null || config.prefab == null) continue;
            
            Transform targetPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
            NavMeshQueryFilter filter = new NavMeshQueryFilter { areaMask = NavMesh.AllAreas };
            CreateEnemyAt(config, targetPoint, filter);
        }
    }

    private void CreateEnemyAt(EnemySpawnConfig config, Transform targetPoint, NavMeshQueryFilter filter)
    {
        GameObject enemy = Instantiate(config.prefab, targetPoint.position, targetPoint.rotation, globalEnemyRoot);
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
        enemy.name = config.enemyName;

        if (agent != null)
        {
            agent.enabled = false;
            filter.agentTypeID = agent.agentTypeID;
            if (NavMesh.SamplePosition(targetPoint.position, out NavMeshHit hit, 10.0f, filter))
            {
                enemy.transform.position = hit.position;
                agent.Warp(hit.position);
                RegisterEnemy(enemy, config.enemyName);
            }
            else
            {
                Destroy(enemy);
            }
        }
        else
        {
            RegisterEnemy(enemy, config.enemyName);
        }
    }

    private void RegisterEnemy(GameObject enemy, string typeName)
    {
        EnemyHandle handle = new EnemyHandle(enemy, typeName);
        activeEnemies.Add(handle);

        if (!enemyTypeCounts.ContainsKey(typeName)) enemyTypeCounts[typeName] = 0;
        enemyTypeCounts[typeName]++;
    }
}