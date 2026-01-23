using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    [HideInInspector] public string originalChunkName;

    [Header("Spawn Settings")]
    public List<Transform> playerSpawnPoints = new List<Transform>();
    public List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("Optimization Targets")]
    public GameObject physicsRoot;
    public GameObject visualRoot;
    public GameObject propsRoot;
    public NavMeshLink[] myLinks;

    public Vector2Int Coord { get; private set; }

    [SerializeField] private DestructibleProp[] props;

    void Awake()
    {
        if ((props == null || props.Length == 0) && propsRoot != null)
        {
            props = propsRoot.GetComponentsInChildren<DestructibleProp>(true);
        }

        if (myLinks == null || myLinks.Length == 0)
        {
            myLinks = GetComponentsInChildren<NavMeshLink>();
        }
    }

    public void Setup(Vector2Int coord)
    {
        this.Coord = coord;
        this.name = $"Chunk_{coord.x}_{coord.y}"; 

        if (visualRoot != null) visualRoot.SetActive(true);

        if (props != null)
        {
            for (int i = 0; i < props.Length; i++)
            {
                props[i].ResetState(); 
                props[i].InitProp(this.Coord, i);

                if (WorldObjectDataManager.Instance != null)
                {
                    if (WorldObjectDataManager.Instance.IsPropDestroyed(this.Coord, i))
                    {
                        props[i].SetDestroyedState();
                    }
                }
            }
        }
    }

    public void RefreshNavMeshLinks()
    {
        foreach (var link in myLinks)
        {
            if (link != null && link.gameObject.activeInHierarchy)
            {
                link.enabled = false;
                link.enabled = true;
            }
        }
    }

    public Transform GetRandomPlayerSpawnPoint()
    {
        if (playerSpawnPoints != null && playerSpawnPoints.Count > 0)
        {
            return playerSpawnPoints[Random.Range(0, playerSpawnPoints.Count)];
        }
        return this.transform;
    }

    public List<Transform> GetEnemySpawnPoints()
    {
        if (enemySpawnPoints == null)
        {
            enemySpawnPoints = new List<Transform>();
        }
        return enemySpawnPoints;
    }

    public void SetPhysicsState(bool enablePhysics)
    {
        if (physicsRoot != null && physicsRoot.activeSelf != enablePhysics)
        {
            physicsRoot.SetActive(enablePhysics);
        }

        if (propsRoot != null && propsRoot.activeSelf != enablePhysics)
        {
            propsRoot.SetActive(enablePhysics);
        }
    }
}