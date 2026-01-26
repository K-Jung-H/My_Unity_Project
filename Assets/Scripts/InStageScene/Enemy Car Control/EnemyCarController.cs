using UnityEngine;

[RequireComponent(typeof(EnemyCarMovement))]
[RequireComponent(typeof(ContextSteering))]
public class EnemyCarController : MonoBehaviour
{
    [Header("Settings Data")]
    [SerializeField] private EnemyStatProfile enemyProfile;

    private enum AIState 
    { 
        ChaseDirect,
        ChasePath,
        Escaping
    } 

    [Header("Targeting")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask viewBlockingLayers;
    [SerializeField] private float sightCheckInterval = 0.2f;
    [SerializeField] private float predictionTime = 1.0f;

    [Header("Pathfinding")]
    [SerializeField] private float pathUpdateInterval = 0.5f;
    [SerializeField] private float reachThreshold = 3.0f;

    [Header("Stuck Detection")]
    [SerializeField] private float positionCheckInterval = 2.0f;
    [SerializeField] private float minDistanceMoved = 1.5f;
    [SerializeField] private float escapeDuration = 1.5f;
    [SerializeField] private LayerMask obstacleMask;

    private EnemyCarMovement movement;
    private ContextSteering steeringSensor;
    private Rigidbody myRb; 
    private Transform targetPlayer;
    private Rigidbody targetRb;

    [SerializeField, Header("Debug State")] 
    private AIState currentState;
    
    private Vector3 currentNavTarget;
    private float lastSightCheckTime;
    private float lastPathRequestTime;
    private bool isWaitingForPath;

    private Vector3 lastStuckCheckPosition;
    private float positionCheckTimer;
    private float currentEscapeTimer;
    private float escapeSteerDirection;

    private void Awake()
    {
        movement = GetComponent<EnemyCarMovement>();
        steeringSensor = GetComponent<ContextSteering>();
        myRb = GetComponent<Rigidbody>(); 
        
        if (enemyProfile != null)
        {
            movement.InitializeFromProfile(enemyProfile);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} : EnemyStatProfile is missing!");
        }
    }

    private void Start()
    {
        FindClosestPlayer();
        ChangeState(AIState.ChasePath); 
    }

    private void OnEnable()
    {
        isWaitingForPath = false;
        positionCheckTimer = 0f;
        currentEscapeTimer = 0f;
        lastStuckCheckPosition = transform.position;
        ChangeState(AIState.ChasePath);
    }

    private void FixedUpdate()
    {
        if (targetPlayer == null)
        {
            movement.SetInputs(0f, 0f);
            if (Time.frameCount % 60 == 0) FindClosestPlayer();
            return;
        }

        ExecuteCurrentState();
    }


    private void ChangeState(AIState newState)
    {
        currentState = newState;
        
        switch (currentState)
        {
            case AIState.ChaseDirect:
            case AIState.ChasePath:
                lastStuckCheckPosition = transform.position;
                positionCheckTimer = 0f;
                break;

            case AIState.Escaping:
                InitializeEscape();
                break;
        }
    }

    private void ExecuteCurrentState()
    {
        switch (currentState)
        {
            case AIState.ChaseDirect:
                UpdateChaseDirect();
                break;
            case AIState.ChasePath:
                UpdateChasePath();
                break;
            case AIState.Escaping:
                UpdateEscaping();
                break;
        }
    }

    private void UpdateChaseDirect()
    {
        if (CheckIfStuck()) 
        {
            ChangeState(AIState.Escaping);
            return;
        }

        if (!CheckLineOfSightWithInterval())
        {
            ChangeState(AIState.ChasePath);
            return;
        }

        Vector3 predictedTarget = GetPredictedTargetPosition();
        DriveToTarget(predictedTarget);
    }

    private void UpdateChasePath()
    {
        if (CheckIfStuck())
        {
            ChangeState(AIState.Escaping);
            return;
        }

        if (CheckLineOfSightWithInterval())
        {
            ChangeState(AIState.ChaseDirect);
            return;
        }

        HandlePathFinding();
        DriveToTarget(currentNavTarget);
    }

    private void UpdateEscaping()
    {
        currentEscapeTimer -= Time.fixedDeltaTime;
        movement.SetInputs(-1f, escapeSteerDirection);

        if (currentEscapeTimer <= 0)
        {
            if (CheckLineOfSight())
                ChangeState(AIState.ChaseDirect);
            else
                ChangeState(AIState.ChasePath);
        }
    }


    private void DriveToTarget(Vector3 targetPos)
    {
        Vector3 moveDirection = steeringSensor.GetDirectionToMove(targetPos, targetPlayer);
        
        float steerInput = moveDirection.x;
        float throttleInput = moveDirection.z;

        if (Mathf.Abs(steerInput) > 0.4f)
        {
            float speedLimitFactor = Mathf.Lerp(1.0f, 0.2f, Mathf.Abs(steerInput));
            throttleInput = Mathf.Min(throttleInput, speedLimitFactor);
        }

        if (throttleInput < -0.1f) 
        {
            throttleInput = 1.0f; 

            if (Mathf.Abs(steerInput) < 0.1f)
            {
                steerInput = (Random.value > 0.5f) ? 1f : -1f;
            }
            else
            {
                steerInput = Mathf.Sign(steerInput); 
            }
        }

        movement.SetInputs(throttleInput, steerInput);
    }

    private bool CheckIfStuck()
    {
        positionCheckTimer += Time.fixedDeltaTime;

        if (positionCheckTimer > positionCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);
            lastStuckCheckPosition = transform.position;
            positionCheckTimer = 0f;

            if (distanceMoved < minDistanceMoved)
            {
                return true;
            }
        }
        return false;
    }

    private void InitializeEscape()
    {
        currentEscapeTimer = escapeDuration;
        
        bool hitRight = Physics.Raycast(transform.position, transform.right, 2.0f, obstacleMask);
        bool hitLeft = Physics.Raycast(transform.position, -transform.right, 2.0f, obstacleMask);

        if (hitRight) escapeSteerDirection = 1f; 
        else if (hitLeft) escapeSteerDirection = -1f; 
        else escapeSteerDirection = (Random.value > 0.5f) ? 1f : -1f;
    }

    private bool CheckLineOfSightWithInterval()
    {
        if (Time.time - lastSightCheckTime < sightCheckInterval) 
        {
            return currentState == AIState.ChaseDirect; 
        }
        
        lastSightCheckTime = Time.time;
        return CheckLineOfSight();
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
            else if (currentState == AIState.ChasePath)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, currentNavTarget);
                Gizmos.DrawWireSphere(currentNavTarget, 1f);
            }
            else if (currentState == AIState.Escaping)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(transform.position, 2f);
                Gizmos.DrawRay(transform.position, -transform.forward * 2f);
            }
        }
    }
}