using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    public static event Action<CarController> OnLocalPlayerCreated;
    public event Action OnLocalPlayerDeath;

    [Header("Dependencies")]
    public DynamicChunkManager chunkManager;
    public CarDataTable carDataTable;
    public Transform playerListContainer;

    private CarController localPlayerInstance;
    private List<CarController> remotePlayerInstances = new List<CarController>();

    public CarController LocalPlayer => localPlayerInstance;
    public IReadOnlyList<CarController> RemotePlayers => remotePlayerInstances;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

    public void Initialize()
    {

        if (chunkManager != null)
        {
            Transform spawnPoint = chunkManager.GetMainSpawnPoint();
            CreatePlayer(spawnPoint, true);
        }
    }

    public GameObject CreatePlayer(Transform spawnPoint, bool isLocal, int carId = -1)
    {
        int targetId = (carId == -1) ? GameData.CarId : carId;
        GameObject prefabToSpawn = null;

        if (carDataTable != null)
        {
            prefabToSpawn = carDataTable.GetCarPrefab(targetId);
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[PlayerManager] Failed to load car prefab for ID {targetId}.");
            return null;
        }

        if (playerListContainer == null)
        {
            GameObject container = new GameObject("Player_Container");
            playerListContainer = container.transform;
        }

        GameObject newCarObj = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation, playerListContainer);
        CarController controller = newCarObj.GetComponent<CarController>();

        if (DynamicChunkManager.Instance != null)
        {
            DynamicChunkManager.Instance.RegisterPlayer(newCarObj.transform);
        }

        if (isLocal)
        {
            newCarObj.name = $"Local_Player_{targetId}";
            localPlayerInstance = controller;
            SetupCamera(newCarObj.transform);
            
            if (PlayerUIManager.Instance != null)
            {
                PlayerUIManager.Instance.SetupPlayerUI(localPlayerInstance);
            }

            OnLocalPlayerCreated?.Invoke(localPlayerInstance);
            localPlayerInstance.OnDeath += HandleLocalPlayerDeath;
        }
        else
        {
            newCarObj.name = $"Remote_Player_{targetId}";
            remotePlayerInstances.Add(controller);
        }

        return newCarObj;
    }

    private void HandleLocalPlayerDeath()
    {
        OnLocalPlayerDeath?.Invoke();
        if (localPlayerInstance != null)
            localPlayerInstance.OnDeath -= HandleLocalPlayerDeath;
    }

    private void SetupCamera(Transform targetTransform)
    {
        if (Camera.main != null)
        {
            var camScript = Camera.main.GetComponent<CarCamera>();
            if (camScript != null) camScript.target = targetTransform;
        }
    }
}