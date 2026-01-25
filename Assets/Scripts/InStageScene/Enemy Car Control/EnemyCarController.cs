using UnityEngine;

[RequireComponent(typeof(EnemyCarMovement))]
[RequireComponent(typeof(ContextSteering))]
public class EnemyCarController : MonoBehaviour
{
    private enum AIState { ChaseDirect, ChasePath }

    [Header("Targeting")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask viewBlockingLayers;
    [SerializeField] private float sightCheckInterval = 0.2f;
    [SerializeField] private float predictionTime = 1.0f;

    [Header("Pathfinding")]
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private float reachThreshold = 3.0f;

    private EnemyCarMovement movement;
    private ContextSteering steeringSensor;
    private Transform targetPlayer;
    private Rigidbody targetRb;

    private AIState currentState;
    private Vector3 currentNavTarget;
    private float lastSightCheckTime;
    private float lastPathRequestTime;
    private bool isWaitingForPath;

    private void Awake()
    {
        movement = GetComponent<EnemyCarMovement>();
        steeringSensor = GetComponent<ContextSteering>();
    }

    private void Start()
    {
        FindClosestPlayer();
    }

    private void OnEnable()
    {
        isWaitingForPath = false;
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null)
        {
            movement.SetInputs(0f, 0f);
            if (Time.frameCount % 60 == 0) FindClosestPlayer();
            return;
        }

        UpdateState();
        MoveVehicle();
    }

    private void UpdateState()
    {
        if (Time.time - lastSightCheckTime < sightCheckInterval) return;
        lastSightCheckTime = Time.time;

        if (CheckLineOfSight())
        {
            currentState = AIState.ChaseDirect;
        }
        else
        {
            currentState = AIState.ChasePath;
        }
    }

    private void MoveVehicle()
    {
        Vector3 finalDestination = Vector3.zero;

        if (currentState == AIState.ChaseDirect)
        {
            finalDestination = GetPredictedTargetPosition();
        }
        else
        {
            HandlePathFinding();
            finalDestination = currentNavTarget;
        }

        Vector3 moveDirection = steeringSensor.GetDirectionToMove(finalDestination);
        
        float steerInput = moveDirection.x;
        float throttleInput = moveDirection.z;

        if (throttleInput < 0 && currentState == AIState.ChaseDirect)
        {
             steerInput = -moveDirection.x;
        }

        movement.SetInputs(throttleInput, steerInput);
    }

    private bool CheckLineOfSight()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 direction = targetPlayer.position - transform.position;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, viewBlockingLayers))
        {
            if (hit.transform != targetPlayer)
            {
                return false;
            }
        }
        return true;
    }

    private Vector3 GetPredictedTargetPosition()
    {
        if (targetRb != null)
        {
            return targetPlayer.position + (targetRb.linearVelocity * predictionTime);
        }
        return targetPlayer.position;
    }

    private void HandlePathFinding()
    {
        if (Time.time - lastPathRequestTime > pathUpdateInterval && !isWaitingForPath)
        {
            RequestNewPath();
        }

        if (Vector3.Distance(transform.position, currentNavTarget) < reachThreshold)
        {
            RequestNewPath();
        }
    }

    private void RequestNewPath()
    {
        isWaitingForPath = true;
        lastPathRequestTime = Time.time;

        if (EnemyPathManager.Instance != null)
        {
            EnemyPathManager.Instance.RequestNextWaypoint(transform.position, OnPathReceived);
        }
    }

    private void OnPathReceived(Vector3 nextCorner)
    {
        isWaitingForPath = false;
        currentNavTarget = nextCorner;
    }

    private void FindClosestPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
            targetRb = playerObj.GetComponent<Rigidbody>();
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (currentState == AIState.ChaseDirect)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, GetPredictedTargetPosition());
            }
            else
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, currentNavTarget);
                Gizmos.DrawWireSphere(currentNavTarget, 1f);
            }
        }
    }
}