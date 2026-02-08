using UnityEngine;

[RequireComponent(typeof(EnemyCarMovement))]
[RequireComponent(typeof(ContextSteering))]
[RequireComponent(typeof(HealthSystem))]
public class EnemyCarController : MonoBehaviour
{
    [Header("Settings Data")]
    [SerializeField] private EnemyStatProfile enemyProfile;

    public EnemyStatProfile EnemyProfile => enemyProfile;
    public HealthSystem Health { get; private set; }

    private enum AIState 
    { 
        ChaseDirect,    
        ChasePath,      
        Escaping,
        Death
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

    private int deadLayer; 

    private void Awake()
    {
        movement = GetComponent<EnemyCarMovement>();
        steeringSensor = GetComponent<ContextSteering>();
        myRb = GetComponent<Rigidbody>(); 
        Health = GetComponent<HealthSystem>();
        
        deadLayer = LayerMask.NameToLayer("Prop"); 

        if (enemyProfile != null)
        {
            movement.InitializeFromProfile(enemyProfile);
            Health.InitializeHealth(enemyProfile.Health);
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} : EnemyStatProfile is missing!");
        }

        Health.OnDeath += OnDeathHandler;
    }

    private void OnDestroy()
    {
        if (Health != null)
        {
            Health.OnDeath -= OnDeathHandler;
        }
    }

    private void OnDeathHandler()
    {
        if (currentState == AIState.Death) return;

        Debug.Log($"[Death Log] Enemy Died at World Pos: {transform.position}");

        ChangeState(AIState.Death);

        if (EnemySpawnManager.Instance != null)
        {
            EnemySpawnManager.Instance.RetireEnemy(this.gameObject);
        }

        if (WorldObjectDataManager.Instance != null)
        {
            ChunkController currentChunk = GetCurrentChunk(); 
            if (currentChunk != null)
            {
                if (WorldObjectDataManager.Instance != null)
                {
                    string cleanName = this.name.Replace("(Clone)", "");
                    WorldObjectDataManager.Instance.RegisterDeadEnemy(
                        currentChunk.Coord, 
                        cleanName,
                        transform.position, 
                        transform.rotation, 
                        currentChunk.transform
                    );
                }

                currentChunk.RegisterDeadEnemy(transform); 
            }
        }

        SetupDeadPhysics();
    }

    public void SetAsDeadState()
    {
        Debug.Log($"[Restoration Log] Enemy Wreckage Placed at World Pos: {transform.position}");

        if (movement != null) movement.enabled = false;
        if (steeringSensor != null) steeringSensor.enabled = false;
        
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.enabled = false;
        
        currentState = AIState.Death;
        if (Health != null) Health.InitializeHealth(0); 
        
        ChangeMaterialToDead();
        
        SetupDeadPhysics();
    }

    private void SetupDeadPhysics()
    {
        gameObject.tag = "Untagged"; 
        gameObject.layer = deadLayer; 
    }

    private ChunkController GetCurrentChunk()
    {
        if (DynamicChunkManager.Instance != null)
        {
            return DynamicChunkManager.Instance.GetChunkAtPosition(transform.position);
        }
        return null;
    }

    private void Start()
    {
        FindClosestPlayer();
        if (currentState != AIState.Death)
        {
            ChangeState(AIState.ChasePath);
        }
    }

    private void OnEnable()
    {
        isWaitingForPath = false;
        positionCheckTimer = 0f;
        currentEscapeTimer = 0f;
        lastStuckCheckPosition = transform.position;
        
        if (Health != null && !Health.IsDead && currentState != AIState.Death)
        {
            ChangeState(AIState.ChasePath);
        }
        else
        {
            ChangeState(AIState.Death);
            if(currentState == AIState.Death) SetupDeadPhysics();
        }
    }

    private void FixedUpdate()
    {
        if (currentState == AIState.Death) return;

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

            case AIState.Death:
                movement.SetInputs(0f, 0f);
                ChangeMaterialToDead();
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
            case AIState.Death:
                break;
        }
    }

    private void ChangeMaterialToDead()
    {
        if (enemyProfile == null || enemyProfile.DeadMaterial == null) return;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();

        foreach (var rend in renderers)
        {
            Material[] newMaterials = new Material[rend.materials.Length];
            for (int i = 0; i < newMaterials.Length; i++)
            {
                newMaterials[i] = enemyProfile.DeadMaterial;
            }
            rend.materials = newMaterials;
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
        movement.SetInputs(moveDirection.z, moveDirection.x);
    }

    private bool CheckIfStuck()
    {
        positionCheckTimer += Time.fixedDeltaTime;
        if (positionCheckTimer > positionCheckInterval)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastStuckCheckPosition);
            lastStuckCheckPosition = transform.position;
            positionCheckTimer = 0f;
            if (distanceMoved < minDistanceMoved) return true;
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
            if (hit.transform != targetPlayer) return false;
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
}