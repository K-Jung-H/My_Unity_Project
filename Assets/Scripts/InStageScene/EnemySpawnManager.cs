using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class EnemySpawnManager : MonoBehaviour
{
    [Header("References")]
    public DynamicChunkManager chunkManager;
    public Transform player;
    public Transform globalEnemyRoot;

    [Header("Global Settings")]
    public GameObject[] enemyPrefabs;
    [Tooltip("맵 전체에 유지할 최대 적의 수")]
    public int maxGlobalEnemies = 20;
    [Tooltip("프레임 드랍 방지를 위해 한 번에 생성할 최대 적 수")]
    public int maxSpawnPerFrame = 2;

    [Header("Distance Settings")]
    public float activationDistance = 200f;   // AI 활성화 거리
    public float deactivationDistance = 300f; // AI 비활성화 거리
    public float enemyCullDistance = 500f;    // 적 삭제 거리

    private List<GameObject> activeEnemies = new List<GameObject>();
    private int carAgentTypeID;

    void Awake()
    {
        // 프리팹에서 'ZeroMarginCar' Agent Type ID 미리 캐싱
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            var agent = enemyPrefabs[0].GetComponent<NavMeshAgent>();
            if (agent != null) carAgentTypeID = agent.agentTypeID;
        }
    }

    void Start()
    {
        // 별도의 이벤트 구독 없이 능동적 루프 실행
        StartCoroutine(ManageEnemyLifecycleRoutine());
    }

    IEnumerator ManageEnemyLifecycleRoutine()
    {
        // 0.5초마다 검사 (성능 최적화)
        WaitForSeconds wait = new WaitForSeconds(0.5f);

        while (true)
        {
            yield return wait;

            // 1. 기존 적 관리 (거리 체크, 활성/비활성, 삭제)
            ManageActiveEnemies();

            // 2. 부족한 적 채우기 (랜덤 스폰)
            SpawnMissingEnemies();
        }
    }

    private void ManageActiveEnemies()
    {
        if (player == null) return;
        Vector3 playerPos = player.position;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];

            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(playerPos, enemy.transform.position);

            // A. 플레이어와 너무 멀어지면 삭제 (Culling) -> 빈자리 발생
            if (dist > enemyCullDistance)
            {
                Destroy(enemy);
                activeEnemies.RemoveAt(i);
                continue;
            }

            // B. 거리 기반 AI 끄고 켜기 (최적화)
            NavMeshAgent agent = enemy.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                if (dist <= activationDistance)
                {
                    if (!agent.enabled) agent.enabled = true;
                }
                else if (dist > deactivationDistance)
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

        // [중요] DynamicChunkManager에서 추가한 메서드 호출
        List<ChunkController> activeChunks = chunkManager.GetActiveChunks().ToList();

        if (activeChunks.Count == 0) return;

        NavMeshQueryFilter filter = new NavMeshQueryFilter();
        filter.agentTypeID = carAgentTypeID;
        filter.areaMask = NavMesh.AllAreas;

        for (int i = 0; i < spawnLoopCount; i++)
        {
            // 1. 랜덤 청크 선택
            ChunkController randomChunk = activeChunks[Random.Range(0, activeChunks.Count)];

            // 아직 로딩 중이거나 비활성 상태면 패스
            if (randomChunk == null || !randomChunk.gameObject.activeInHierarchy) continue;

            // 2. 해당 청크의 스폰 포인트 가져오기
            List<Transform> spawnPoints = randomChunk.GetSpawnPoints();
            if (spawnPoints == null || spawnPoints.Count == 0) continue;

            Transform targetPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

            // 3. 적 생성 시도
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
            agent.enabled = false; // 위치 잡을 때까지 끄기

            NavMeshHit hit;
            // Agent Radius가 3이므로, SamplePosition 범위를 10.0f 정도로 넉넉하게 잡음
            if (NavMesh.SamplePosition(targetPoint.position, out hit, 10.0f, filter))
            {
                enemy.transform.position = hit.position;
                agent.Warp(hit.position);
                agent.agentTypeID = carAgentTypeID; // Agent Type ID 강제 주입

                // 플레이어 타겟 설정
                var anim = enemy.GetComponent<EnemyCarAnimation>(); // 혹은 EnemyCarMovement
                if (anim != null) anim.targetTransform = player;

                // 이동 스크립트가 있다면 타겟 설정
                var move = enemy.GetComponent<EnemyCarAnimation>();
                if (move != null) move.targetTransform = player;

                activeEnemies.Add(enemy);
                // agent.enabled = true; 는 위쪽 ManageActiveEnemies에서 거리 체크 후 켜짐
            }
            else
            {
                // NavMesh 위치를 못 찾으면 삭제 (안전장치)
                Destroy(enemy);
            }
        }
        else
        {
            activeEnemies.Add(enemy);
        }
    }
}