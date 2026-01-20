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
    
    // [수정] 인스펙터에서 보이지만, Awake에서 코드로 덮어씌워질 변수
    public LayerMask groundLayer; 
    public LayerMask obstacleLayer;

    [Header("State Info (Read Only)")]
    public EnemyState currentState = EnemyState.CHASE;
    public bool isDirectChasing = false;
    
    private float pressTimer = 0f;
    private const float PRESS_DURATION = 2.0f;
    
    private float lastObstacleDetectTime = -999f;
    private const float OBSTACLE_AVOIDANCE_COOLDOWN = 2.0f; 

    private NavMeshAgent agent;
    private Rigidbody rb;
    private int roadAreaMask;
    
    private float lastAttackTime = -999f;
    private bool isCharging = false;
    private bool isUnderPlayer = false;

    private Vector3 lastTargetPosition;
    private const float TARGET_MOVE_THRESHOLD = 0.5f;

    [SerializeField] private float groundedTimer = 0f;
    
    [Header("Slope Settings")]
    public float maxSlopeChaseHeight = 10.0f; 

    private float currentAgentSpeed = 0f;
    private float stuckTimer = 0f;

    private const float RECOVERY_WAIT_TIME = 3.0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        // [핵심 수정] 요청하신 두 레이어를 코드로 강제 할당 (대소문자 정확해야 함)
        // Unity 에디터의 Layers 설정에 "Track"과 "Structure_Static"이 존재해야 합니다.
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
            currentAgentSpeed = 0f;
            stuckTimer = 0f;
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

        // 디버깅용 레이 그리기 (Scene 뷰에서 초록색 선 확인 가능)
        Debug.DrawRay(transform.position + Vector3.up * 0.5f, Vector3.down * stats.airborneCheckDist, isUnstable ? Color.red : Color.green);

        if (isUnstable && currentState != EnemyState.AIRBORNE)
        {
            SwitchState(EnemyState.AIRBORNE);
            return;
        }

        if (currentState == EnemyState.AIRBORNE)
        {
            HandleAirborneState();
            return;
        }
        
        if (currentState == EnemyState.CHASE && agent.enabled && !agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
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
                break;
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // 바닥 레이어와 충돌한 경우는 무시 (땅에 닿았다고 Airborne 되는 것 방지)
        if (((1 << collision.gameObject.layer) & groundLayer) != 0) return;

        if (currentState == EnemyState.CHASE)
        {
            foreach (ContactPoint contact in collision.contacts)
            {
                if (contact.point.y < transform.position.y + 0.3f) continue;

                if (collision.relativeVelocity.magnitude > 5.0f || ((1 << collision.gameObject.layer) & obstacleLayer) != 0)
                {
                    SwitchState(EnemyState.AIRBORNE);
                    Vector3 bounceDir = contact.normal;
                    rb.AddForce(bounceDir * stats.mass * 2.0f, ForceMode.Impulse);
                    return; 
                }
            }
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
            
            if (targetTransform != null && agent.enabled && agent.isOnNavMesh && currentState == EnemyState.CHASE && !isDirectChasing)
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

    private void HandleChase()
    {
        float targetSpeed = IsOnRoad() ? stats.roadSpeed : stats.normalSpeed;
        currentAgentSpeed = Mathf.Lerp(currentAgentSpeed, targetSpeed, Time.deltaTime * 5.0f);
        agent.speed = currentAgentSpeed;
        
        float distToTarget = Vector3.Distance(transform.position, targetTransform.position);
        
        bool isInAvoidanceCooldown = Time.time < lastObstacleDetectTime + OBSTACLE_AVOIDANCE_COOLDOWN;
        bool hasObstacle = HasObstacleInPath();

        if (hasObstacle) lastObstacleDetectTime = Time.time;

        if (!isInAvoidanceCooldown && 
            distToTarget <= stats.directChaseDistance && 
            !hasObstacle) 
        {
            isDirectChasing = true;
            agent.updatePosition = true; 
            stuckTimer = 0f;
            MoveAndRotate(targetTransform.position, currentAgentSpeed, true);
        }
        else
        {
            isDirectChasing = false;
            
            if (agent.pathPending)
            {
                stuckTimer = 0f;
                return;
            }
            
            if (!agent.hasPath && agent.isOnNavMesh)
            {
                agent.SetDestination(targetTransform.position);
            }
            else if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance)
            {
                if (agent.velocity.sqrMagnitude < 0.1f)
                {
                    stuckTimer += Time.deltaTime;
                    if (stuckTimer > 1.0f)
                    {
                        SwitchState(EnemyState.AIRBORNE);
                        stuckTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }
            }

            MoveAndRotate(agent.steeringTarget, currentAgentSpeed, false);
        }

        if (distToTarget <= stats.attackTriggerRange && 
            Time.time >= lastAttackTime + stats.chargeCooldown && 
            !isCharging)
        {
            StartCoroutine(ChargeSequence());
        }
    }

    private void MoveAndRotate(Vector3 desiredDestination, float maxSpeed, bool isDirect)
    {
        float distToDest = Vector3.Distance(transform.position, desiredDestination);
        
        if (!isDirect && distToDest <= agent.stoppingDistance)
        {
            agent.velocity = Vector3.zero;
            return; 
        }

        Vector3 directionToTarget = (desiredDestination - transform.position);
        directionToTarget.y = 0; 

        if (directionToTarget.sqrMagnitude < 0.1f) return;

        directionToTarget.Normalize();

        Vector3 groundNormal = Vector3.up;
        RaycastHit hit;
        
        // [수정] MoveAndRotate에서도 동일한 groundLayer 사용
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 5.0f, groundLayer))
        {
            groundNormal = hit.normal;
        }

        Vector3 projectedForward = Vector3.ProjectOnPlane(directionToTarget, groundNormal).normalized;

        if (projectedForward != Vector3.zero)
        {
            float angleToTarget = Vector3.Angle(transform.forward, projectedForward);
            float corneringFactor = Mathf.Clamp(1.0f - (angleToTarget / 180.0f), 0.5f, 1.0f); 
            
            agent.speed = maxSpeed * corneringFactor;

            Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, stats.turnSpeed * Time.deltaTime * 5.0f); 
            
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, stats.slopeDamping * Time.deltaTime);
        }

        if (isDirect)
        {
            agent.velocity = transform.forward * agent.speed;
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
            SwitchState(EnemyState.CHASE);
            return;
        }
        float dist = Vector3.Distance(transform.position, targetTransform.position);
        if (dist > stats.disengageDistance)
        {
            SwitchState(EnemyState.CHASE);
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
            SwitchState(EnemyState.PRESS_ATTACK);
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
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; 
                
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 3.0f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                }

                if (agent.isOnNavMesh) 
                {
                    agent.updatePosition = true;
                    SafeSetStopped(false);
                    currentAgentSpeed = 0f;
                    stuckTimer = 0f;
                }
                break;

            case EnemyState.CHARGE:
                SafeSetStopped(true);
                agent.updatePosition = false;
                rb.isKinematic = false; 
                rb.linearVelocity = agent.velocity;
                break;
                
            case EnemyState.PRESS_ATTACK:
                pressTimer = 0f;
                SafeSetStopped(true);
                agent.updatePosition = false;
                rb.isKinematic = false; 
                rb.linearVelocity = agent.velocity;
                break;

            case EnemyState.AIRBORNE:
                groundedTimer = 0f;
                SafeSetStopped(true);
                agent.updatePosition = false;
                rb.isKinematic = false; 
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

    // [핵심 수정] isGround 판단 로직 개선
    private bool CheckIfAirborneOrFlipped()
    {
        // SphereCast를 사용하여 바닥 감지 범위를 넓힘 (Radius 0.5f)
        // transform.position + Vector3.up * 1.0f 위치에서 아래로 쏠 때, 차 바닥면 근처를 훑게 됨
        bool isGrounded = Physics.CheckSphere(transform.position + Vector3.down * 0.1f, 0.5f, groundLayer);
        
        // SphereCast가 너무 넓으면 아래 Raycast로 이중 체크 (선택 사항이나, 정확도 위해 둘 다 사용 가능)
        // 여기서는 SphereCast가 닿았거나, Raycast가 닿았으면 Ground로 판정
        if (!isGrounded)
        {
             isGrounded = Physics.Raycast(transform.position + Vector3.up, Vector3.down, stats.airborneCheckDist, groundLayer);
        }

        bool isUpright = Vector3.Dot(transform.up, Vector3.up) > 0.5f;
        
        // 땅에 안 닿았거나, 뒤집혀 있다면 Unstable(Airborne)
        return !isGrounded || !isUpright;
    }

    private void HandleAirborneState()
    {
        // [수정] 여기서도 동일한 groundLayer 마스크 사용
        bool isTouchingGround = Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, 2.0f, groundLayer);

        if (isTouchingGround)
        {
            groundedTimer += Time.deltaTime;
        }
        else
        {
            groundedTimer = 0f;
        }

        if (groundedTimer > RECOVERY_WAIT_TIME)
        {
            bool recovered = HandleAirborneRecovery();
            if (recovered)
            {
                SwitchState(EnemyState.CHASE);
            }
        }
    }

    private bool HandleAirborneRecovery()
    {
        RaycastHit hit;
        // [수정] Recovery 시에도 동일한 groundLayer 사용
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10.0f, groundLayer))
        {
            Vector3 targetPos = hit.point + Vector3.up * 1.0f;
            
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 5.0f);
            transform.position = Vector3.Lerp(transform.position, targetPos, stats.uprightSpeed * Time.deltaTime);

            Vector3 projectedForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (projectedForward == Vector3.zero) projectedForward = transform.forward;

            Quaternion targetRot = Quaternion.LookRotation(projectedForward, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, stats.uprightSpeed * Time.deltaTime);

            if (Vector3.Dot(transform.up, Vector3.up) > 0.9f && Vector3.Distance(transform.position, targetPos) < 0.2f)
            {
                return true;
            }
        }
        return false;
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