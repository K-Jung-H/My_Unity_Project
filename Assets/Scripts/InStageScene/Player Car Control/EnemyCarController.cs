using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public enum EnemyState
{
    CHASE,
    CHARGE,
    PRESS_ATTACK,
    AIRBORNE
}

public class EnemyCarController : MonoBehaviour
{
    [Header("Configuration")]
    public EnemyStatProfile stats;

    [Header("Target & Layers")]
    public Transform targetTransform;
    public string roadAreaName = "Road";
    public LayerMask groundLayer;
    public LayerMask obstacleLayer;

    [Header("State Info (Read Only)")]
    public EnemyState currentState = EnemyState.CHASE;
    public bool isDirectChasing = false;
    
    private float lastObstacleDetectTime = -999f;
    private const float OBSTACLE_AVOIDANCE_COOLDOWN = 2.0f; 
    private const float MAX_HEIGHT_DIFF_FOR_DIRECT_CHASE = 3.0f;

    private NavMeshAgent agent;
    private Rigidbody rb;
    private int roadAreaMask;
    
    private float lastAttackTime = -999f;
    private bool isCharging = false;

    private Vector3 lastTargetPosition;
    private const float TARGET_MOVE_THRESHOLD = 0.5f;

    [SerializeField] private float groundedTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (rb != null && stats != null)
        {
            rb.mass = stats.mass;
            rb.linearDamping = stats.drag;
            rb.angularDamping = stats.angularDrag;
            
            rb.isKinematic = true; 
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (agent != null && stats != null)
        {
            agent.updatePosition = true; 
            agent.updateRotation = false; 
            agent.updateUpAxis = false;
            agent.autoBraking = false;
            agent.stoppingDistance = 0.5f;
            agent.acceleration = stats.acceleration;
        }

        int roadIndex = NavMesh.GetAreaFromName(roadAreaName);
        if (roadIndex != -1) roadAreaMask = 1 << roadIndex;

        if (groundLayer == 0) groundLayer = -1;
    }

    void OnEnable()
    {
        if (agent != null)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            agent.velocity = Vector3.zero;
        }
        
        if (targetTransform == null) FindNearestPlayer();
        if (targetTransform != null) lastTargetPosition = Vector3.zero;

        groundedTimer = 0f;
        if (CheckIfAirborneOrFlipped()) SwitchState(EnemyState.AIRBORNE);
        else SwitchState(EnemyState.CHASE);

        StartCoroutine(ThinkRoutine());
    }

    void Update()
    {
        if (targetTransform == null || stats == null) return;

        bool isUnstable = CheckIfAirborneOrFlipped();

        if (isUnstable)
        {
            groundedTimer = 0f; 

            if (currentState != EnemyState.AIRBORNE) SwitchState(EnemyState.AIRBORNE);
            
            HandleAirborneRecovery();
            return; 
        }
        else if (currentState == EnemyState.AIRBORNE)
        {
            groundedTimer += Time.deltaTime;
            
            HandleAirborneRecovery();

            if (groundedTimer >= stats.recoveryDelay)
            {
                SwitchState(EnemyState.CHASE);
            }
            return;
        }

        switch (currentState)
        {
            case EnemyState.CHASE:
                HandleChase();
                break;

            case EnemyState.CHARGE:
                HandleChargeRotation();
                break;

            case EnemyState.PRESS_ATTACK:
                HandlePressAttack();
                RotateTowardsDirectly(targetTransform.position);
                break;
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
            
            if (targetTransform != null && agent.enabled && agent.isOnNavMesh && currentState == EnemyState.CHASE && !isDirectChasing)
            {
                if (Vector3.SqrMagnitude(targetTransform.position - lastTargetPosition) > (TARGET_MOVE_THRESHOLD * TARGET_MOVE_THRESHOLD))
                {
                    agent.SetDestination(targetTransform.position);
                    lastTargetPosition = targetTransform.position;
                }
            }
            yield return wait;
        }
    }

    private void HandleChase()
    {
        float currentMaxSpeed = IsOnRoad() ? stats.roadSpeed : stats.normalSpeed;
        agent.speed = currentMaxSpeed;
        agent.acceleration = stats.acceleration;

        float dist = Vector3.Distance(transform.position, targetTransform.position);
        float heightDiff = Mathf.Abs(targetTransform.position.y - transform.position.y);
        
        Vector3 horizontalDelta = targetTransform.position - transform.position;
        horizontalDelta.y = 0;
        float horizontalDist = horizontalDelta.magnitude;

        if (heightDiff > MAX_HEIGHT_DIFF_FOR_DIRECT_CHASE && horizontalDist < 2.0f)
        {
            agent.velocity = Vector3.zero;
            return;
        }
        
        bool isInAvoidanceCooldown = Time.time < lastObstacleDetectTime + OBSTACLE_AVOIDANCE_COOLDOWN;
        bool hasObstacle = HasObstacleInPath();

        if (hasObstacle) lastObstacleDetectTime = Time.time;

        if (!isInAvoidanceCooldown && 
            dist <= stats.directChaseDistance && 
            !hasObstacle && 
            heightDiff < MAX_HEIGHT_DIFF_FOR_DIRECT_CHASE)
        {
            isDirectChasing = true;
            HandleDirectChaseMovement(currentMaxSpeed);
        }
        else
        {
            isDirectChasing = false;
            HandleMovementAndRotationOld(); 
        }

        if (dist <= stats.attackTriggerRange && 
            heightDiff < MAX_HEIGHT_DIFF_FOR_DIRECT_CHASE &&
            Time.time >= lastAttackTime + stats.chargeCooldown && 
            !isCharging)
        {
            StartCoroutine(ChargeSequence());
        }
    }

    private void HandleDirectChaseMovement(float speed)
    {
        Vector3 directionToTarget = (targetTransform.position - transform.position);
        directionToTarget.y = 0;
        
        if (directionToTarget.sqrMagnitude > 0.1f)
        {
            directionToTarget.Normalize();

            RaycastHit hit;
            Vector3 groundNormal = Vector3.up;
            if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5.0f, groundLayer))
            {
                groundNormal = hit.normal;
            }

            Vector3 projectedForward = Vector3.ProjectOnPlane(directionToTarget, groundNormal).normalized;
            if (projectedForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime);
            }
            agent.velocity = transform.forward * speed; 
        }
        else
        {
            agent.velocity = transform.forward * speed; 
        }
    }

    private void HandleMovementAndRotationOld()
    {
        if (!agent.hasPath) return;

        RaycastHit hit;
        Vector3 groundNormal = Vector3.up;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5.0f, groundLayer))
        {
            groundNormal = hit.normal;
        }

        Vector3 nextTarget = agent.steeringTarget;
        Vector3 directionToTarget = (nextTarget - transform.position);
        float distanceToSteer = directionToTarget.magnitude;

        directionToTarget.y = 0; 
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            directionToTarget.Normalize();

            if (distanceToSteer > 1.5f) 
            {
                Vector3 projectedForward = Vector3.ProjectOnPlane(directionToTarget, groundNormal).normalized;
                if (projectedForward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime);
                    
                    float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
                    if (angleDiff > 5.0f)
                    {
                        Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
                        transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, stats.slopeDamping * Time.deltaTime);
                    }
                }
            }
            else
            {
                Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
                transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, stats.slopeDamping * Time.deltaTime);
            }
        }
    }

    private bool HasObstacleInPath()
    {
        if (targetTransform == null) return false;
        
        Vector3 dir = targetTransform.position - transform.position;
        float dist = dir.magnitude;
        float checkDist = Mathf.Min(dist, stats.directChaseDistance + 2.0f);

        return Physics.Raycast(transform.position + Vector3.up, dir, checkDist, obstacleLayer);
    }

    private void RotateTowardsDirectly(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position);
        dir.y = 0;
        if (dir.sqrMagnitude > 0.1f)
        {
            dir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
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
        float dist = Vector3.Distance(transform.position, targetTransform.position);

        if (dist > stats.disengageDistance)
        {
            SwitchState(EnemyState.CHASE);
            return;
        }

        if (Time.time >= lastAttackTime + stats.chargeCooldown && !isCharging)
        {
            StartCoroutine(ChargeSequence());
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
        
        if (currentState != EnemyState.AIRBORNE)
        {
            float dist = Vector3.Distance(transform.position, targetTransform.position);
            if (dist <= stats.disengageDistance) SwitchState(EnemyState.PRESS_ATTACK);
            else SwitchState(EnemyState.CHASE);
        }
    }

    private void SwitchState(EnemyState newState)
    {
        if (currentState == newState) return;

        if (currentState == EnemyState.CHASE)
        {
            SafeSetStopped(true);
            agent.updatePosition = false;
        }

        currentState = newState;

        switch (currentState)
        {
            case EnemyState.CHASE:
                rb.isKinematic = true; 
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                
                if (agent.isOnNavMesh || agent.Warp(transform.position)) 
                {
                    agent.updatePosition = true;
                    SafeSetStopped(false);
                }
                break;

            case EnemyState.CHARGE:
            case EnemyState.PRESS_ATTACK:
            case EnemyState.AIRBORNE:
                SafeSetStopped(true);
                agent.updatePosition = false;
                rb.isKinematic = false; 
                
                if (currentState != EnemyState.AIRBORNE)
                    rb.linearVelocity = agent.velocity;
                break;
        }
    }

    private void SafeSetStopped(bool isStopped)
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = isStopped;
        }
    }

    private bool CheckIfAirborneOrFlipped()
    {
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, stats.airborneCheckDist, groundLayer);
        bool isUpright = Vector3.Dot(transform.up, Vector3.up) > 0.5f;

        return !isGrounded || !isUpright;
    }

    private void HandleAirborneRecovery()
    {
        Quaternion targetRot = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.uprightSpeed * Time.deltaTime);

        if (Physics.Raycast(transform.position, Vector3.down, 1.0f, groundLayer))
        {
            rb.linearVelocity += Vector3.up * 5.0f * Time.deltaTime; 
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