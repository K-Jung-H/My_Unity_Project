using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby_CarSelectManager : MonoBehaviour
{
    [Header("Dependencies")]
    public LobbyManager lobbyManager;

    [Header("Car Database")]
    public List<CarData> carDatabase;

    [Header("Position Transforms")]
    public Transform Start_Pos;
    public Transform Center_Pos;
    public Transform End_Pos;

    [Header("Settings")]
    public float moveDuration = 0.5f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float rotateSpeed = 30.0f;
    public bool canRotate = true;

    private List<GameObject> carPool = new List<GameObject>();
    private GameObject currentCar;
    private int currentIndex = 0;
    private bool isAnimating = false;

    void Start()
    {
        InitializeCarPool();

        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged += HandleStateChange;
            HandleStateChange(lobbyManager.CurrentState);
        }
    }

    void OnDestroy()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnStateChanged -= HandleStateChange;
        }
    }

    void Update()
    {
        if (lobbyManager != null && lobbyManager.CurrentState == LobbyState.Selection_Car)
        {
            HandleCenterRotation();
        }
    }

    private void HandleStateChange(LobbyState state)
    {
        StopAllCoroutines();
        isAnimating = false;

        if (state == LobbyState.Selection_Car)
        {
            if (currentCar == null && carPool.Count > 0)
            {
                ShowCarImmediate(currentIndex);
            }
            else if (currentCar != null)
            {
                currentCar.SetActive(true);
            }
        }
        else
        {
            HideAllCars();
        }
    }

    public void Change_Rotate()
    {
        canRotate = !canRotate;
    }

    public void Change_Car_Prev()
    {
        if (isAnimating || carPool.Count == 0) return;
        if (Start_Pos == null || End_Pos == null) return;

        int prevIndex = (currentIndex - 1 + carPool.Count) % carPool.Count;
        StartCoroutine(ChangeCarSequence(prevIndex, -1)); 
    }

    public void Change_Car_Next()
    {
        if (isAnimating || carPool.Count == 0) return;
        if (Start_Pos == null || End_Pos == null) return;

        int nextIndex = (currentIndex + 1) % carPool.Count;
        StartCoroutine(ChangeCarSequence(nextIndex, 1));
    }

    private void InitializeCarPool()
    {
        if (carDatabase == null) carDatabase = new List<CarData>();

        GameObject container = new GameObject("SelectCarList");
        container.transform.position = Vector3.zero;

        for (int i = 0; i < carDatabase.Count; i++)
        {
            GameObject prefab = carDatabase[i].carPrefab;
            if (prefab == null) continue;

            GameObject obj = Instantiate(prefab, container.transform);
            
            CleanupCarComponents(obj);

            obj.SetActive(false);
            carPool.Add(obj);
        }
    }

    private void CleanupCarComponents(GameObject obj)
    {
        var controller = obj.GetComponent<CarController>();
        if (controller != null)
        {
            controller.StopAllCoroutines();
            controller.enabled = false;  
        }

        var rb = obj.GetComponent<Rigidbody>();
        if (rb) Destroy(rb);

        Transform effectContainer = obj.transform.Find("Effect");
        if (effectContainer != null)
        {
            effectContainer.gameObject.SetActive(false);
        }

        foreach (var cam in obj.GetComponentsInChildren<Camera>()) cam.enabled = false;
        foreach (var list in obj.GetComponentsInChildren<AudioListener>()) list.enabled = false;
    }

    private void HideAllCars()
    {
        foreach (var car in carPool)
        {
            if (car.activeSelf) car.SetActive(false);
        }
    }

    private void ShowCarImmediate(int index)
    {
        HideAllCars();
        if (index < 0 || index >= carPool.Count) return;

        currentIndex = index;
        currentCar = carPool[currentIndex];
        
        currentCar.transform.position = Center_Pos.position;
        currentCar.transform.rotation = Center_Pos.rotation;
        currentCar.SetActive(true);

        SaveCurrentCarId();
    }

    private void HandleCenterRotation()
    {
        if (isAnimating || currentCar == null || !canRotate) return;
        currentCar.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }

    private IEnumerator ChangeCarSequence(int targetIndex, int direction)
    {
        isAnimating = true;

        Transform exitPos = (direction > 0) ? Start_Pos : End_Pos;
        GameObject outgoingCar = currentCar;

        Transform enterPos = (direction > 0) ? End_Pos : Start_Pos;
        GameObject incomingCar = carPool[targetIndex];

        StartCoroutine(MoveCar(outgoingCar, Center_Pos.position, exitPos.position, false));
        yield return StartCoroutine(MoveCar(incomingCar, enterPos.position, Center_Pos.position, true));

        currentIndex = targetIndex;
        currentCar = incomingCar;
        SaveCurrentCarId();

        isAnimating = false;
    }
    
    private IEnumerator MoveCar(GameObject obj, Vector3 start, Vector3 destination, bool isEntering)
    {
        obj.transform.position = start;
        obj.SetActive(true);
        obj.transform.LookAt(destination);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;
            float curveValue = moveCurve.Evaluate(t);

            obj.transform.position = Vector3.Lerp(start, destination, curveValue);
            yield return null;
        }

        obj.transform.position = destination;

        if (isEntering)
        {
            obj.transform.rotation = Center_Pos.rotation;
        }
        else
        {
            obj.SetActive(false);
        }
    }

    private void SaveCurrentCarId()
    {
        if (carDatabase != null && currentIndex >= 0 && currentIndex < carDatabase.Count)
        {
            GameData.CarId = carDatabase[currentIndex].carID;
        }
    }
}