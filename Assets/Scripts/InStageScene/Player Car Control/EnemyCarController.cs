using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyState
{
    NAV_CHASE,
    DIRECT_CHASE,
    CHARGE,
    PRESS_ATTACK
}

public class EnemyCarController : MonoBehaviour
{
    [Header("Configuration")]
    public EnemyStatProfile stats;

    [Header("Target & Layers")]
    public Transform targetTransform;
    public string roadAreaName = "Road";
    
    public LayerMask groundLayer;

    [Header("Life Cycle Settings")]
    public float deathTime = 10.0f;
    public float deathRange = 5.0f;

    [Header("State Info (Read Only)")]
    public EnemyState currentState = EnemyState.NAV_CHASE; 
    
    private float pressTimer = 0f;
    private const float PRESS_DURATION = 2.0f;
    
    private NavMeshAgent agent;
    private Rigidbody rb;
    private int roadAreaMask;
    
    // [신규] 매니저 참조 변수
    private EnemySpawnManager spawnManager;

    private float lastAttackTime = -999f;
    private bool isCharging = false;
    private bool isUnderPlayer = false;

    private Vector3 lastTargetPosition;
    private const float TARGET_MOVE_THRESHOLD = 0.5f;

    [Header("Slope Settings")]
    public float maxSlopeChaseHeight = 10.0f; 

    private float currentAgentSpeed = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        groundLayer = LayerMask.GetMask("Track", "Structure_Static");
        
        if (rb != null && stats != null)
        {
            rb.mass = stats.mass;
            rb.linearDamping = stats.drag;
            rb.angularDamping = stats.angularDrag;
            
            rb.isKinematic = true; 
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.sleepThreshold = 0f; 
        }

        if (agent != null && stats != null)
        {
            agent.updatePosition = true; 
            agent.updateRotation = false; 
            agent.updateUpAxis = false;
            agent.autoBraking = false; 
            agent.stoppingDistance = 1.0f; 
            agent.acceleration = stats.acceleration;
        }

        int roadIndex = NavMesh.GetAreaFromName(roadAreaName);
        if (roadIndex != -1) roadAreaMask = 1 << roadIndex;
    }

    void Start()
    {
        // [신규] 씬에 있는 매니저 찾기 (싱글톤이 없다면 FindObjectOfType 사용)
        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<EnemySpawnManager>();
        }
    }

    void OnEnable()
    {
        if (agent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            agent.velocity = Vector3.zero;
            currentAgentSpeed = 0f;
        }
        
        if (targetTransform == null) FindNearestPlayer();
        if (targetTransform != null) lastTargetPosition = Vector3.zero;

        float dist = Vector3.Distance(transform.position, targetTransform.position);
        if (dist <= stats.directChaseDistance) SwitchState(EnemyState.DIRECT_CHASE);
        else SwitchState(EnemyState.NAV_CHASE);

        StartCoroutine(ThinkRoutine());
        StartCoroutine(CheckActivityRoutine());
    }

    void Update()
    {
        if (targetTransform == null || stats == null) return;

        if (currentState == EnemyState.CHARGE || currentState == EnemyState.PRESS_ATTACK)
        {
            SyncAgentToPhysics();
        }

        switch (currentState)
        {
            case EnemyState.NAV_CHASE:
                HandleNavChase();
                break;
            
            case EnemyState.DIRECT_CHASE:
                HandleDirectChase();
                break;

            case EnemyState.CHARGE:
                HandleChargeRotation(); 
                break;

            case EnemyState.PRESS_ATTACK:
                HandlePressAttack();
                break;
        }
    }

    private void SyncAgentToPhysics()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.nextPosition = transform.position;
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        if (targetTransform != null && collision.gameObject == targetTransform.gameObject)
        {
            if (targetTransform.position.y > transform.position.y + 0.8f) 
            {
                isUnderPlayer = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            else
            {
                isUnderPlayer = false;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (targetTransform != null && collision.gameObject == targetTransform.gameObject)
        {
            isUnderPlayer = false;
        }
    }

    private IEnumerator ThinkRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(stats.pathUpdateInterval);

        while (true)
        {
            if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
            {
                FindNearestPlayer();
            }
            
            if (targetTransform != null && agent.enabled && agent.isOnNavMesh && currentState == EnemyState.NAV_CHASE)
            {
                NavMeshHit hit;
                Vector3 targetPos = targetTransform.position;

                if (NavMesh.SamplePosition(targetTransform.position, out hit, 2.0f, NavMesh.AllAreas))
                {
                    targetPos = hit.position;
                }

                if (Vector3.SqrMagnitude(targetPos - lastTargetPosition) > (TARGET_MOVE_THRESHOLD * TARGET_MOVE_THRESHOLD))
                {
                    agent.SetDestination(targetPos);
                    lastTargetPosition = targetPos;
                }
            }
            yield return wait;
        }
    }

    private IEnumerator CheckActivityRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(deathTime);
        float sqrDeathRange = deathRange * deathRange;

        while (true)
        {
            Vector3 startPos = transform.position;
            yield return wait;

            if (gameObject == null) yield break;

            float movedDistSqr = (transform.position - startPos).sqrMagnitude;

            // [수정] 일정 범위 내에서 움직임이 없으면 삭제 요청
            if (movedDistSqr < sqrDeathRange)
            {
                if (spawnManager != null)
                {
                    // 매니저에게 나를 명단에서 빼고 카운트 줄여달라고 요청 (SpawnManager가 Destroy까지 처리함)
                    spawnManager.UnregisterEnemy(this.gameObject);
                }
                else
                {
                    // 매니저가 없으면 그냥 자폭
                    Destroy(gameObject);
                }
                yield break;
            }
        }
    }

    private void HandleNavChase()
    {
        float targetSpeed = IsOnRoad() ? stats.roadSpeed : stats.normalSpeed;
        currentAgentSpeed = Mathf.Lerp(currentAgentSpeed, targetSpeed, Time.deltaTime * 5.0f);
        agent.speed = currentAgentSpeed;
        
        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);
        
        if (distToTarget <= stats.directChaseDistance) 
        {
            SwitchState(EnemyState.DIRECT_CHASE);
            return;
        }

        if (agent.pathPending) return;
        if (!agent.hasPath && agent.isOnNavMesh) agent.SetDestination(targetTransform.position);
        
        MoveAndRotate(agent.steeringTarget, currentAgentSpeed, false);
    }

    private void HandleDirectChase()
    {
        float targetSpeed = IsOnRoad() ? stats.roadSpeed : stats.normalSpeed;
        currentAgentSpeed = Mathf.Lerp(currentAgentSpeed, targetSpeed, Time.deltaTime * 5.0f);
        
        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);

        if (distToTarget > stats.directChaseDistance + 2.0f)
        {
            SwitchState(EnemyState.NAV_CHASE);
            return;
        }

        if (distToTarget <= stats.attackTriggerRange && 
            Time.time >= lastAttackTime + stats.chargeCooldown && 
            !isCharging)
        {
            StartCoroutine(ChargeSequence());
            return;
        }

        MoveAndRotate(targetTransform.position, currentAgentSpeed, true);
    }

    private void MoveAndRotate(Vector3 desiredDestination, float maxSpeed, bool isDirect)
    {
        Vector3 directionToTarget = (desiredDestination - transform.position);
        directionToTarget.y = 0; 

        if (directionToTarget.sqrMagnitude > 0.1f)
        {
            directionToTarget.Normalize();
        }
        else
        {
            directionToTarget = transform.forward;
        }

        Vector3 groundNormal = Vector3.up;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5.0f, groundLayer))
        {
            groundNormal = hit.normal;
        }

        Vector3 projectedForward = Vector3.ProjectOnPlane(directionToTarget, groundNormal).normalized;

        if (projectedForward != Vector3.zero)
        {
            float angleToTarget = Vector3.Angle(transform.forward, projectedForward);
            float corneringFactor = Mathf.Clamp(1.0f - (angleToTarget / 180.0f), 0.5f, 1.0f); 
            
            float finalSpeed = maxSpeed * corneringFactor;
            
            if (!isDirect) agent.speed = finalSpeed;

            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime * 5.0f); 
            
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, stats.slopeDamping * Time.deltaTime);

            if (isDirect)
            {
                agent.velocity = transform.forward * finalSpeed;
            }
        }
        else if (isDirect)
        {
            agent.velocity = Vector3.zero;
        }
    }

    private void HandleChargeRotation()
    {
        Vector3 velocityDir = rb.linearVelocity;
        velocityDir.y = 0;
        if (velocityDir.sqrMagnitude > 1.0f) 
        {
            velocityDir.Normalize();
            Quaternion lookRot = Quaternion.LookRotation(velocityDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void HandlePressAttack()
    {
        if (isUnderPlayer) return;
        
        pressTimer += Time.deltaTime;
        if (pressTimer > PRESS_DURATION)
        {
            SwitchState(EnemyState.NAV_CHASE);
            return;
        }
        
        float dist = Vector3.Distance(transform.position, targetTransform.position);
        if (dist > stats.disengageDistance)
        {
            SwitchState(EnemyState.NAV_CHASE);
            return;
        }
        
        Vector3 dir = (targetTransform.position - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.1f)
        {
            dir.Normalize();
            rb.AddForce(dir * stats.pressForce, ForceMode.Force);
        }
    }

    private IEnumerator ChargeSequence()
    {
        isCharging = true;
        lastAttackTime = Time.time;
        SwitchState(EnemyState.CHARGE);
        
        Vector3 dir = (targetTransform.position - transform.position);
        dir.y = 0;
        
        if (dir.sqrMagnitude > 0.1f)
        {
            dir.Normalize();
            rb.AddForce(dir * stats.chargeForce, ForceMode.Impulse);
        }
        else
        {
            rb.AddForce(transform.forward * stats.chargeForce, ForceMode.Impulse);
        }
        
        yield return new WaitForSeconds(stats.chargeDuration);
        
        isCharging = false;
        SwitchState(EnemyState.PRESS_ATTACK);
    }

    private void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.NAV_CHASE:
            case EnemyState.DIRECT_CHASE:
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; 
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 10.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position); 
                }
                
                if (agent.isOnNavMesh) 
                {
                    agent.updatePosition = true;
                    agent.isStopped = false;
                    agent.ResetPath();
                }
                break;

            case EnemyState.CHARGE:
                agent.updatePosition = false; 
                rb.isKinematic = false; 
                rb.linearVelocity = agent.velocity;
                if (agent.isOnNavMesh) agent.ResetPath();
                break;
                
            case EnemyState.PRESS_ATTACK:
                pressTimer = 0f;
                agent.updatePosition = false;
                rb.isKinematic = false; 
                rb.linearVelocity = agent.velocity;
                if (agent.isOnNavMesh) agent.ResetPath();
                break;
        }
    }

    private bool IsOnRoad()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 0.5f, NavMesh.AllAreas))
        {
            return (hit.mask & roadAreaMask) != 0;
        }
        return false;
    }

    private void FindNearestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float min = float.MaxValue;
        foreach (var p in players)
        {
            if (!p.activeInHierarchy) continue;
            float d = Vector3.Distance(transform.position, p.transform.position);
            if (d < min) { min = d; targetTransform = p.transform; }
        }
        if (targetTransform != null) lastTargetPosition = Vector3.zero;
    }
}