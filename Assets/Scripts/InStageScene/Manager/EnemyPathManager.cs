using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyPathManager : MonoBehaviour
{
    public static EnemyPathManager Instance { get; private set; }

    [Header("Optimization Settings")]
    [SerializeField] private int maxPathCalculationsPerFrame = 5; 
    [SerializeField] private float playerSearchInterval = 0.5f; 
    [SerializeField] private string playerTag = "Player";

    private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
    
    private List<Transform> players = new List<Transform>();
    private float lastPlayerSearchTime;

    private struct PathRequest
    {
        public Vector3 startPos;
        public System.Action<Vector3> callback;

        public PathRequest(Vector3 start, System.Action<Vector3> cb)
        {
            startPos = start;
            callback = cb;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        RefreshPlayerList();
        ProcessPathRequests();
    }

    private void RefreshPlayerList()
    {
        if (Time.time - lastPlayerSearchTime > playerSearchInterval)
        {
            players.Clear();
            GameObject[] playerObjs = GameObject.FindGameObjectsWithTag(playerTag);
            foreach (var obj in playerObjs)
            {
                players.Add(obj.transform);
            }
            lastPlayerSearchTime = Time.time;
        }
    }

    public void RequestNextWaypoint(Vector3 startPos, System.Action<Vector3> onNewWaypointFound)
    {
        pathRequestQueue.Enqueue(new PathRequest(startPos, onNewWaypointFound));
    }

    private void ProcessPathRequests()
    {
        if (players.Count == 0) return;

        int processedCount = 0;
        while (pathRequestQueue.Count > 0 && processedCount < maxPathCalculationsPerFrame)
        {
            PathRequest request = pathRequestQueue.Dequeue();
            processedCount++;

            Transform targetPlayer = GetNearestPlayer(request.startPos);
            
            if (targetPlayer == null) continue;

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(request.startPos, targetPlayer.position, NavMesh.AllAreas, path))
            {

                if (path.corners.Length > 1)
                {
                    request.callback?.Invoke(path.corners[1]);
                }
                else
                {
                    request.callback?.Invoke(targetPlayer.position);
                }
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

        foreach (Transform player in players)
        {
            if (player == null) continue;

            Vector3 directionToTarget = player.position - fromPos;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            
            if (dSqrToTarget < closestDistSqr)
            {
                closestDistSqr = dSqrToTarget;
                bestTarget = player;
            }
        }

        return bestTarget;
    }
}