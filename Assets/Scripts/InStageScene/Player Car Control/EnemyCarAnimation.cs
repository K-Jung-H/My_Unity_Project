using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyCarAnimation : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetTransform;
    public string roadAreaName = "Road";
    public LayerMask groundLayer;

    [Header("Car Movement")]
    public float normalSpeed = 8.0f;
    public float roadSpeed = 20.0f;
    public float acceleration = 50.0f;
    public float turnSpeed = 10.0f; 
    public float slopeDamping = 20.0f; 

    [Header("Attack Settings")]
    public float impactDistance = 1.5f; 
    
    [Header("Optimization")]
    public float pathUpdateInterval = 0.1f; 
    public float targetMoveThreshold = 0.5f;

    private NavMeshAgent agent;
    private int roadAreaMask;
    private Vector3 lastTargetPosition;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false; 
            agent.updateUpAxis = false; 
            agent.angularSpeed = 3000f; 
            agent.acceleration = acceleration;
            
            agent.autoBraking = false; 
            agent.stoppingDistance = 0.1f; 
        }

        int areaIndex = NavMesh.GetAreaFromName(roadAreaName);
        if (areaIndex != -1)
        {
            roadAreaMask = 1 << areaIndex;
        }
        
        if (groundLayer == 0) groundLayer = -1;
    }

    void OnEnable()
    {
        if (agent != null)
        {
            agent.velocity = Vector3.zero;
            if (agent.isOnNavMesh)
            {
                agent.ResetPath();
            }
        }
        
        StartCoroutine(ThinkRoutine());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        float targetMaxSpeed = IsOnRoad() ? roadSpeed : normalSpeed;
        agent.speed = targetMaxSpeed;
        agent.acceleration = acceleration;

        HandleMovementAndRotation();
    }

    IEnumerator ThinkRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(pathUpdateInterval);

        while (true)
        {
            if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
            {
                FindNearestPlayer();
            }

            if (targetTransform != null && agent.isOnNavMesh)
            {

                if (Vector3.SqrMagnitude(targetTransform.position - lastTargetPosition) > (targetMoveThreshold * targetMoveThreshold))
                {
                    agent.SetDestination(targetTransform.position);
                    lastTargetPosition = targetTransform.position;
                }
            }

            yield return wait;
        }
    }

    private void HandleMovementAndRotation()
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
        directionToTarget.Normalize();


        if (distanceToSteer > impactDistance) 
        {
            Vector3 projectedForward = Vector3.ProjectOnPlane(directionToTarget, groundNormal).normalized;

            if (projectedForward != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(projectedForward, groundNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
                
                float angleDiff = Quaternion.Angle(transform.rotation, targetRotation);
                if (angleDiff > 5.0f)
                {
                    Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
                    transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, slopeDamping * Time.deltaTime);
                }
            }
        }
        else
        {
            Quaternion slopeRotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, slopeRotation, slopeDamping * Time.deltaTime);
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
        float minDistance = float.MaxValue;
        Transform nearest = null;

        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            
            float d = (transform.position - p.transform.position).sqrMagnitude;
            if (d < minDistance)
            {
                minDistance = d;
                nearest = p.transform;
            }
        }

        targetTransform = nearest;
        if (nearest != null) lastTargetPosition = Vector3.zero; 
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.down * 2.0f);
        
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        Gizmos.DrawWireSphere(transform.position, impactDistance);
    }
}