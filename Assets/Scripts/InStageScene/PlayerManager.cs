using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerManager : MonoBehaviour
{
    public static event Action<CarController> OnLocalPlayerCreated;
    public event Action OnLocalPlayerDeath;

    [Header("Dependencies")]
    public DynamicChunkManager chunkManager;

    [Header("Data Management")]
    public CarDataTable carDataTable;

    [Header("Player Container")]
    public Transform playerListContainer;

    [Header("Scene UI References")]
    public SteeringWheelUI sceneSteeringWheel;
    public GearBoxUI sceneGearBox;
    public HoldPressInput sceneAccelPedal;
    public HoldPressInput sceneBrakePedal;

    private CarController localPlayerInstance;
    private List<CarController> remotePlayerInstances = new List<CarController>();

    public void Initialize()
    {
        if (chunkManager != null)
        {
            Transform spawnPoint = chunkManager.GetMainSpawnPoint();
            GameObject localPlayer = CreatePlayer(spawnPoint, true);
            
            if (localPlayer != null)
            {
                chunkManager.RegisterPlayer(localPlayer.transform);
            }
        }
        else
        {
            Debug.LogError("[PlayerManager] DynamicChunkManager reference is missing.");
        }
        
        Debug.Log("PlayerManager Initialized");
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
            Debug.LogError($"[PlayerManager] Failed to load car prefab for ID {targetId}. Check CarDataTable.");
            return null;
        }

        if (playerListContainer == null)
        {
            GameObject container = new GameObject("Player_Container");
            playerListContainer = container.transform;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        GameObject newCarObj = Instantiate(prefabToSpawn, pos, rot, playerListContainer);
        newCarObj.tag = "Player";

        CarController carController = newCarObj.GetComponent<CarController>();

        if (carController != null)
        {
            if (isLocal)
            {
                newCarObj.name = $"Local_Player_{targetId}";
                localPlayerInstance = carController;

                CarInputManager carInput = newCarObj.GetComponent<CarInputManager>();
                if (carInput != null)
                {
                    carInput.steeringWheelUI = sceneSteeringWheel;
                    carInput.gearBoxUI = sceneGearBox;
                    carInput.accelPedalUI = sceneAccelPedal;
                    carInput.brakePedalUI = sceneBrakePedal;
                }

                localPlayerInstance.OnDeath += HandleLocalPlayerDeath;
                OnLocalPlayerCreated?.Invoke(carController);
                SetupCamera(newCarObj.transform);
            }
            else
            {
                newCarObj.name = $"Remote_Player_{targetId}";
                remotePlayerInstances.Add(carController);

                CarInputManager carInput = newCarObj.GetComponent<CarInputManager>();
                if (carInput != null) Destroy(carInput);

                carController.OnDeath += () => HandleRemotePlayerDeath(carController);
                
                if (chunkManager != null)
                {
                    chunkManager.RegisterPlayer(newCarObj.transform);
                }
            }
        }

        return newCarObj;
    }

    private void HandleLocalPlayerDeath()
    {
        OnLocalPlayerDeath?.Invoke();

        if (localPlayerInstance != null)
            localPlayerInstance.OnDeath -= HandleLocalPlayerDeath;
    }

    private void HandleRemotePlayerDeath(CarController deadPlayer)
    {
        if (chunkManager != null)
        {
            chunkManager.UnregisterPlayer(deadPlayer.transform);
        }
        remotePlayerInstances.Remove(deadPlayer);
    }

    private void SetupCamera(Transform targetTransform)
    {
        if (Camera.main != null)
        {
            var camScript = Camera.main.GetComponent<CarCamera>();
            if (camScript != null) camScript.target = targetTransform;
        }
    }

    private void OnDestroy()
    {
        if (localPlayerInstance != null)
            localPlayerInstance.OnDeath -= HandleLocalPlayerDeath;
    }
}