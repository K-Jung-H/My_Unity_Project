using UnityEngine;
using UnityEngine.AI;

public class EnemyCarAnimation : MonoBehaviour
{
    [Header("Settings")]
    public Transform targetTransform;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (targetTransform == null)
        {
            Debug.LogWarning("Target Transform이 연결되지 않았습니다. 인스펙터에서 할당해주세요.");
        }
    }

    void Update()
    {
        if (targetTransform != null)
        {
            if (agent.pathPending) return;

            agent.SetDestination(targetTransform.position);
        }

    }
}