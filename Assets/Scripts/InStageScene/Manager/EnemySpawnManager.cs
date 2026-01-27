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

    [Header("Distance Settings")]
    public float activationDistance = 200f;
    public float deactivationDistance = 300f;
    public float enemyCullDistance = 500f;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Dictionary<string, int> enemyTypeCounts = new Dictionary<string, int>();

    private GameObject[] cachedPlayers;

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
        StopAllCoroutines();
        StartCoroutine(ManageEnemyLifecycleRoutine());
        Debug.Log("EnemySpawnManager Initialized");
    }


    IEnumerator ManageEnemyLifecycleRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        while (true)
        {
            yield return wait;

            if (PlayerManager.Instance != null)
            {
                List<GameObject> allPlayers = new List<GameObject>();

                if (PlayerManager.Instance.LocalPlayer != null)
                    allPlayers.Add(PlayerManager.Instance.LocalPlayer.gameObject);

                var remotes = PlayerManager.Instance.RemotePlayers;
                for (int i = 0; i < remotes.Count; i++)
                {
                    if (remotes[i] != null)
                        allPlayers.Add(remotes[i].gameObject);
                }

                cachedPlayers = allPlayers.ToArray();

                if (cachedPlayers.Length > 0)
                {
                    ManageActiveEnemies();
                    SpawnMissingEnemies();
                }
            }
        }
    }

    private void ManageActiveEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];

            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            float minDist = GetDistanceToNearestPlayer(enemy.transform.position);

            if (minDist > enemyCullDistance)
            {
                RemoveEnemy(enemy, i);
                continue;
            }

            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                if (minDist <= activationDistance)
                {
                    if (!agent.enabled) agent.enabled = true;
                }
                else if (minDist > deactivationDistance)
                {
                    if (agent.enabled) agent.enabled = false;
                }
            }
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            int index = activeEnemies.IndexOf(enemy);
            RemoveEnemy(enemy, index);
        }
        else
        {
            Destroy(enemy);
        }
    }

    private void RemoveEnemy(GameObject enemy, int index)
    {
        string typeName = enemy.name;
        
        if (enemyTypeCounts.ContainsKey(typeName))
        {
            enemyTypeCounts[typeName]--;

            if (enemyTypeCounts[typeName] < 0) 
                enemyTypeCounts[typeName] = 0;
        }

        Destroy(enemy);
        activeEnemies.RemoveAt(index);
    }

private void SpawnMissingEnemies()
{
    if (DifficultyManager.Instance == null) {
        Debug.LogError("[SpawnDebug] DifficultyManager Instance is NULL!");
        return;
    }

    int currentMaxGlobalEnemies = DifficultyManager.Instance.GetCurrentMaxEnemies();
    int currentCount = activeEnemies.Count;

    if (currentCount >= currentMaxGlobalEnemies) {
        return;
    }
    
    List<ChunkController> activeChunks = DynamicChunkManager.Instance.GetActiveChunks().ToList();
    if (activeChunks.Count == 0) {
        Debug.LogWarning("[SpawnDebug] No active chunks found from DynamicChunkManager!");
        return;
    }

    int needed = currentMaxGlobalEnemies - currentCount;
    int spawnLoopCount = Mathf.Min(needed, maxSpawnPerFrame);

    for (int i = 0; i < spawnLoopCount; i++)
    {
        ChunkController randomChunk = activeChunks[UnityEngine.Random.Range(0, activeChunks.Count)];
        
        List<Transform> spawnPoints = randomChunk.GetEnemySpawnPoints();
        if (spawnPoints == null || spawnPoints.Count == 0) {
            Debug.LogWarning($"[SpawnDebug] Chunk {randomChunk.name} has NO spawn points!");
            continue;
        }
        
        EnemySpawnConfig configToSpawn = DifficultyManager.Instance.PickEnemyToSpawn(enemyTypeCounts);
        if (configToSpawn == null || configToSpawn.prefab == null) {
            continue;
        }
        
        Transform targetPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        NavMeshQueryFilter filter = new NavMeshQueryFilter { areaMask = NavMesh.AllAreas };
        CreateEnemyAt(configToSpawn, targetPoint, filter);
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

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPoint.position, out hit, 10.0f, filter))
            {
                enemy.transform.position = hit.position;
                agent.Warp(hit.position);

                RegisterEnemy(enemy, config.enemyName);
            }
            else
            {
                Debug.LogError($"[SpawnDebug] NavMesh SamplePosition FAILED for {config.enemyName} at {targetPoint.position}. Is NavMesh baked?");
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
        activeEnemies.Add(enemy);
        
        if (!enemyTypeCounts.ContainsKey(typeName))
        {
            enemyTypeCounts[typeName] = 0;
        }
        enemyTypeCounts[typeName]++;
    }

    private float GetDistanceToNearestPlayer(Vector3 position)
    {
        float minDistance = float.MaxValue;
        
        foreach (var player in cachedPlayers)
        {
            if (player == null || !player.activeInHierarchy) continue;
            float d = Vector3.Distance(position, player.transform.position);
            if (d < minDistance) minDistance = d;
        }
        return minDistance;
    }
}