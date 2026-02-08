using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyPathManager : MonoBehaviour
{
    public static EnemyPathManager Instance { get; private set; }

    [Header("Optimization Settings")]
    [SerializeField] private int maxPathCalculationsPerFrame = 5; 
    

    private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    
    private NavMeshPath _sharedPath;

    private struct PathRequest
    {
        public Vector3 startPos;
        public System.Action<Vector3> callback;
        public PathRequest(Vector3 start, System.Action<Vector3> cb) { startPos = start; callback = cb; }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        _sharedPath = new NavMeshPath();
    }

    private void Update()
    {
        ProcessPathRequests();
    }

    public void RequestNextWaypoint(Vector3 startPos, System.Action<Vector3> onNewWaypointFound)
    {
        pathRequestQueue.Enqueue(new PathRequest(startPos, onNewWaypointFound));
    }

    private void ProcessPathRequests()
    {
        if (PlayerManager.Instance == null || PlayerManager.Instance.AllActivePlayers.Count == 0) return;

        int processedCount = 0;
        while (pathRequestQueue.Count > 0 && processedCount < maxPathCalculationsPerFrame)
        {
            PathRequest request = pathRequestQueue.Dequeue();
            processedCount++;

            Transform targetPlayer = GetNearestPlayer(request.startPos);
            if (targetPlayer == null) continue;

            _sharedPath.ClearCorners(); // 경로 재사용 전 초기화

            if (NavMesh.CalculatePath(request.startPos, targetPlayer.position, NavMesh.AllAreas, _sharedPath))
            {
                if (_sharedPath.corners.Length > 1)
                    request.callback?.Invoke(_sharedPath.corners[1]);
                else
                    request.callback?.Invoke(targetPlayer.position);
            }
            else
            {
                request.callback?.Invoke(targetPlayer.position);
            }
        }
    }

    private Transform GetNearestPlayer(Vector3 fromPos)
    {
        Transform bestTarget = null;
        float closestDistSqr = Mathf.Infinity;
        var players = PlayerManager.Instance.AllActivePlayers;

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;
            
            float dSqr = (players[i].transform.position - fromPos).sqrMagnitude;
            if (dSqr < closestDistSqr)
            {
                closestDistSqr = dSqr;
                bestTarget = players[i].transform;
            }
        }
        return bestTarget;
    }
}