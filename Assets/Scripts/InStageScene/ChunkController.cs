using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class ChunkController : MonoBehaviour
{
    [HideInInspector] public string originalChunkName;

    [Header("Spawn Settings")]
    [SerializeField] private Transform playerSpawnRoot;
    [SerializeField] private Transform enemySpawnRoot;
    public List<Transform> playerSpawnPoints = new List<Transform>();
    public List<Transform> enemySpawnPoints = new List<Transform>();

    [Header("Optimization Targets")]
    public GameObject physicsRoot;
    public GameObject visualRoot;
    public GameObject propsRoot;
    
    private Transform deadEnemyRoot; 

    public NavMeshLink[] myLinks;

    public Vector2Int Coord { get; private set; }

    [SerializeField] public DestructibleProp[] props;

    public bool IsSetupDone { get; private set; } = false;

    private void OnValidate()
    {
        UpdateSpawnPoints(playerSpawnRoot, playerSpawnPoints);
        UpdateSpawnPoints(enemySpawnRoot, enemySpawnPoints);
    }

    private void UpdateSpawnPoints(Transform root, List<Transform> list)
    {
        if (root == null) return;
        list.Clear();
        foreach (Transform child in root) list.Add(child);
    }

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

    public IEnumerator SetupRoutine(Vector2Int coord, int propsBatchSize = 15)
    {
        IsSetupDone = false;
        
        CleanupDeadEnemies();

        this.Coord = coord;
        this.name = $"Chunk_{coord.x}_{coord.y}";

        if (visualRoot != null && !visualRoot.activeSelf)
        {
            visualRoot.SetActive(true);
        }
        
        if (WorldObjectDataManager.Instance != null)
        {
            RestoreDeadEnemies(coord);
        }

        yield return null;

        if (props != null)
        {
            for (int i = 0; i < props.Length; i++)
            {
                props[i].ResetState(); 
                props[i].InitProp(this.Coord, i);

                if (WorldObjectDataManager.Instance != null && WorldObjectDataManager.Instance.IsPropDestroyed(this.Coord, i))
                {
                    props[i].SetDestroyedState();
                }

                if ((i + 1) % propsBatchSize == 0)
                {
                    yield return null;
                }
            }
        }
        
        IsSetupDone = true;
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
            return playerSpawnPoints[Random.Range(0, playerSpawnPoints.Count)];
        return this.transform;
    }

    public List<Transform> GetEnemySpawnPoints()
    {
        if (enemySpawnPoints == null) enemySpawnPoints = new List<Transform>();
        return enemySpawnPoints;
    }

    public void SetPhysicsState(bool enablePhysics)
    {
        if (!IsSetupDone && enablePhysics) return;

        if (physicsRoot != null && physicsRoot.activeSelf != enablePhysics)
            physicsRoot.SetActive(enablePhysics);

        if (propsRoot != null && propsRoot.activeSelf != enablePhysics)
            propsRoot.SetActive(enablePhysics);
    }

    private void CleanupDeadEnemies()
    {
        if (deadEnemyRoot != null)
        {
            foreach (Transform child in deadEnemyRoot) Destroy(child.gameObject);
        }
        else
        {
            GameObject rootObj = new GameObject("DeadEnemy_Root");
            rootObj.transform.SetParent(this.transform);
            rootObj.transform.localPosition = Vector3.zero;
            deadEnemyRoot = rootObj.transform;
        }
    }

    private void RestoreDeadEnemies(Vector2Int coord)
    {
        var deadList = WorldObjectDataManager.Instance.GetDeadEnemies(coord);
        if (deadList == null) return;

        foreach (var data in deadList)
        {
            GameObject prefab = GetDeadEnemyPrefab(data.enemyID); 

            if (prefab != null)
            {
                Vector3 spawnPos = transform.TransformPoint(data.localPosition);
                Quaternion spawnRot = transform.rotation * data.localRotation;

                GameObject obj = Instantiate(prefab, spawnPos, spawnRot, deadEnemyRoot);
                
                var controller = obj.GetComponent<EnemyCarController>();
                if (controller != null)
                {
                    controller.SetAsDeadState();
                }
            }
        }
    }

    private GameObject GetDeadEnemyPrefab(string enemyID)
    {
        if (DifficultyManager.Instance != null && DifficultyManager.Instance.difficultyDataTable != null)
        {
             return DifficultyManager.Instance.difficultyDataTable.FindEnemyPrefabByID(enemyID);
        }
        return null; 
    }
}