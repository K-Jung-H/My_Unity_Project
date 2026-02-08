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
    
    [Tooltip("이 높이 아래로 떨어지면 즉시 제거합니다.")]
    public float minWorldHeight = -50f;

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
        // 역순 순회 (삭제 안전성)
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];

            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            // [최적화] Transform 접근 캐싱
            Vector3 enemyPos = enemy.transform.position;

            // 1. 낙하 감지: 맵 아래로 떨어진 적 제거
            if (enemyPos.y < minWorldHeight)
            {
                RemoveEnemy(enemy, i); // 완전 제거 (파괴)
                continue;
            }

            // 2. 플레이어와의 거리 계산
            float minDist = GetDistanceToNearestPlayer(enemyPos);

            // 3. 거리 기반 컬링 (너무 멀면 삭제)
            if (minDist > enemyCullDistance)
            {
                RemoveEnemy(enemy, i); // 완전 제거 (파괴)
                continue;
            }

            // 4. AI 활성화/비활성화 (LOD)
            if (enemy.TryGetComponent(out NavMeshAgent agent))
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

    // [중요] EnemyCarController에서 호출하는 함수
    // 적을 파괴하지 않고, 관리 리스트에서만 뺍니다. (잔해로 남기기 위함)
    public void RetireEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            // 타입 카운트 감소
            string typeName = enemy.name;
            if (enemyTypeCounts.ContainsKey(typeName))
            {
                enemyTypeCounts[typeName]--;
                if (enemyTypeCounts[typeName] < 0) enemyTypeCounts[typeName] = 0;
            }

            // 리스트에서 제거 (Destroy는 하지 않음!)
            activeEnemies.Remove(enemy);
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

    // 내부적으로 적을 완전히 파괴하고 리스트에서 제거하는 함수
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
            // Debug.LogWarning("[SpawnDebug] No active chunks found!");
            return;
        }

        int needed = currentMaxGlobalEnemies - currentCount;
        int spawnLoopCount = Mathf.Min(needed, maxSpawnPerFrame);

        for (int i = 0; i < spawnLoopCount; i++)
        {
            ChunkController randomChunk = activeChunks[UnityEngine.Random.Range(0, activeChunks.Count)];
            
            List<Transform> spawnPoints = randomChunk.GetEnemySpawnPoints();
            if (spawnPoints == null || spawnPoints.Count == 0) {
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

        // 이름을 설정해두어야 나중에 RetireEnemy나 RemoveEnemy에서 카운트를 줄일 수 있음
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
                Debug.LogError($"[SpawnDebug] NavMesh SamplePosition FAILED for {config.enemyName}.");
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