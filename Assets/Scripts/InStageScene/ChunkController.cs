using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    [Header("Optimization Targets")]
    [Tooltip("정적 물체의 콜라이더 그룹 (멀어지면 끔)")]
    public GameObject staticCollidersRoot;

    [Tooltip("적 스폰 좌표 리스트")]
    public GameObject spawnPosRoot;
    private List<Transform> cachedSpawnPoints;

    [Tooltip("동적 소품들의 부모 (멀어지면 끔)")]
    public GameObject propsRoot;

    [Tooltip("청크의 NavLink")]
    public NavMeshLink[] myLinks;



    [SerializeField] private DestructibleProp[] props;

    private Vector2Int currentCoord;

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

    void Reset()
    {
        if (propsRoot != null)
        {
            props = propsRoot.GetComponentsInChildren<DestructibleProp>(true);
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

    public void Setup(Vector2Int coord)
    {
        currentCoord = coord;

        if (props != null)
        {
            for (int i = 0; i < props.Length; i++)
            {
                props[i].InitProp(currentCoord, i);

                if (WorldObjectDataManager.Instance.IsPropDestroyed(currentCoord, i))
                {
                    props[i].SetDestroyedState();
                }
                else
                {
                    props[i].ResetState();
                }
            }
        }
    }

    public List<Transform> GetSpawnPoints()
    {
        if (cachedSpawnPoints == null)
        {
            cachedSpawnPoints = new List<Transform>();

            if (spawnPosRoot != null)
            {
                foreach (Transform child in spawnPosRoot.transform)
                {
                    cachedSpawnPoints.Add(child);
                }
            }
            else
            {
                Debug.LogWarning($"Chunk {gameObject.name} : spawnPosRoot가 연결되지 않았습니다!");
            }
        }

        return cachedSpawnPoints;
    }

    public void SetPhysicsState(bool enablePhysics)
    {
        if (staticCollidersRoot != null)
        {
            foreach (Transform child in staticCollidersRoot.transform)
            {
                if (child.gameObject.activeSelf != enablePhysics)
                {
                    child.gameObject.SetActive(enablePhysics);
                }
            }
        }

        if (propsRoot != null && propsRoot.activeSelf != enablePhysics)
        {
            propsRoot.SetActive(enablePhysics);
        }
    }
}