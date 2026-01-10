using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("References")]
    public DynamicChunkManager chunkManager;
    public Transform globalEnemyRoot;

    [Header("Global Settings")]
    public GameObject[] enemyPrefabs;
    public int maxGlobalEnemies = 20;
    public int maxSpawnPerFrame = 2;

    [Header("Distance Settings")]
    public float activationDistance = 200f;
    public float deactivationDistance = 300f;
    public float enemyCullDistance = 500f;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private int carAgentTypeID;

    private GameObject[] cachedPlayers;

    void Awake()
    {
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            var agent = enemyPrefabs[0].GetComponent<NavMeshAgent>();
            if (agent != null) carAgentTypeID = agent.agentTypeID;
        }
    }

    void Start()
    {
        StartCoroutine(ManageEnemyLifecycleRoutine());
    }

    IEnumerator ManageEnemyLifecycleRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        while (true)
        {
            yield return wait;
            
            cachedPlayers = GameObject.FindGameObjectsWithTag("Player");

            if (cachedPlayers != null && cachedPlayers.Length > 0)
            {
                ManageActiveEnemies();
                SpawnMissingEnemies();
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
                Destroy(enemy);
                activeEnemies.RemoveAt(i);
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

    private void SpawnMissingEnemies()
    {
        int currentCount = activeEnemies.Count;
        if (currentCount >= maxGlobalEnemies) return;

        int needed = maxGlobalEnemies - currentCount;
        int spawnLoopCount = Mathf.Min(needed, maxSpawnPerFrame);

        List<ChunkController> activeChunks = chunkManager.GetActiveChunks().ToList();
        if (activeChunks.Count == 0) return;

        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = carAgentTypeID;
        filter.areaMask = NavMesh.AllAreas;

        for (int i = 0; i < spawnLoopCount; i++)
        {
            ChunkController randomChunk = activeChunks[Random.Range(0, activeChunks.Count)];
            if (randomChunk == null || !randomChunk.gameObject.activeInHierarchy) continue;

            List<Transform> spawnPoints = randomChunk.GetEnemySpawnPoints();
            if (spawnPoints == null || spawnPoints.Count == 0) continue;

            Transform targetPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            CreateEnemyAt(targetPoint, filter);
        }
    }

    private void CreateEnemyAt(Transform targetPoint, NavMeshQueryFilter filter)
    {
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject enemy = Instantiate(prefab, targetPoint.position, targetPoint.rotation, globalEnemyRoot);
        NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.enabled = false;

            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPoint.position, out hit, 10.0f, filter))
            {
                enemy.transform.position = hit.position;
                agent.Warp(hit.position);
                agent.agentTypeID = carAgentTypeID;

                activeEnemies.Add(enemy);
            }
            else
            {
                Destroy(enemy);
            }
        }
        else
        {
            activeEnemies.Add(enemy);
        }
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