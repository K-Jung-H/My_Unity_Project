using UnityEngine;
using UnityEngine.AI;

public class EnemyCarAnimation : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetTransform;

    [Header("Car Physics Simulation")]
    public float accelerationTime = 3.0f;
    public float turnSpeed = 120.0f;
    public float stopDistance = 2.0f;

    private NavMeshAgent agent;
    private float targetSpeed;
    private float updateTimer = 0f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = true;

            targetSpeed = agent.speed;
        }
    }

    void OnEnable()
    {
        if (agent != null)
        {
            agent.speed = 0f;
            agent.velocity = Vector3.zero;
        }
    }

    void Update()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (agent.speed < targetSpeed)
        {
            agent.speed = Mathf.MoveTowards(agent.speed, targetSpeed, (targetSpeed / accelerationTime) * Time.deltaTime);
        }

        if (targetTransform != null)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer > 0.1f)
            {
                float dist = Vector3.Distance(transform.position, targetTransform.position);
                if (dist > stopDistance && !agent.pathPending)
                {
                    agent.SetDestination(targetTransform.position);
                }
                updateTimer = 0f;
            }
        }

        if (agent.hasPath)
        {
            Vector3 dirToTarget = (agent.steeringTarget - transform.position).normalized;

            if (dirToTarget != Vector3.zero && agent.velocity.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(dirToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }
        }
    }
}