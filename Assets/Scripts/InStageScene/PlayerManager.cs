using UnityEngine;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    [Header("Car Database")]
    public List<CarData> carDatabase;

    [Header("Player Container")]
    public Transform playerListContainer;

    [Header("Scene UI References")]
    public SteeringWheelUI sceneSteeringWheel;
    public GearBoxUI sceneGearBox;
    public HoldPressInput sceneAccelPedal;
    public HoldPressInput sceneBrakePedal;

    public GameObject CreatePlayer(Transform spawnPoint)
    {
        int targetId = GameData.CarId;
        GameObject prefabToSpawn = null;

        foreach (var data in carDatabase)
        {
            if (data.carID == targetId)
            {
                prefabToSpawn = data.carPrefab;
                break;
            }
        }

        if (prefabToSpawn == null && carDatabase.Count > 0)
            prefabToSpawn = carDatabase[0].carPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogError("PlayerManager: Prefab not found.");
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
        newCarObj.name = $"PlayerCar_{targetId}";
        newCarObj.tag = "Player";

        CarInputManager carInput = newCarObj.GetComponent<CarInputManager>();
        if (carInput != null)
        {
            carInput.steeringWheelUI = sceneSteeringWheel;
            carInput.gearBoxUI = sceneGearBox;
            carInput.accelPedalUI = sceneAccelPedal;
            carInput.brakePedalUI = sceneBrakePedal;
        }

        SetupCamera(newCarObj.transform);

        return newCarObj;
    }

    private void SetupCamera(Transform targetTransform)
    {
        if (Camera.main != null)
        {
            var camScript = Camera.main.GetComponent<CarCamera>(); 
            if (camScript != null)
            {
                camScript.target = targetTransform;
            }
        }
    }
}