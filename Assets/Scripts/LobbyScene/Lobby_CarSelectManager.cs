using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby_CarSelectManager : MonoBehaviour
{
    [Header("Car Database")]
    public List<CarData> carDatabase;

    [Header("Position Transforms")]
    public Transform Start_Pos;
    public Transform Center_Pos;
    public Transform End_Pos;

    [Header("Settings")]
    public float moveDuration = 1.0f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float rotateSpeed = 30.0f;
    public bool Rotate = true;

    private List<GameObject> carPool = new List<GameObject>();
    private GameObject currentCar;
    private int currentIndex = 0;
    private bool isAnimating = false;
    private LobbyManager lobbyManager;
    private LobbyState previousState;

    void Start()
    {
        lobbyManager = FindObjectOfType<LobbyManager>();
        InitializeCarPool();

        if (lobbyManager != null)
        {
            previousState = lobbyManager.CurrentState;
            CheckStateAndUpdateCars();
        }
    }

    void Update()
    {
        if (lobbyManager != null)
        {
            LobbyState currentState = lobbyManager.CurrentState;
            
            if (currentState != previousState)
            {
                previousState = currentState;
                CheckStateAndUpdateCars();
            }

            if (currentState == LobbyState.Selection_Car)
            {
                HandleCenterRotation();
            }
        }
    }

    public void Change_Rotate()
    {
        Rotate = !Rotate;
    }

    public void Change_Car_Prev()
    {
        if (isAnimating || carPool.Count == 0) return;
        if (lobbyManager != null && lobbyManager.CurrentState != LobbyState.Selection_Car) return;

        int prevIndex = (currentIndex - 1 + carPool.Count) % carPool.Count;
        StartCoroutine(ChangeCarSequence(prevIndex));
    }

    public void Change_Car_Next()
    {
        if (isAnimating || carPool.Count == 0) return;
        if (lobbyManager != null && lobbyManager.CurrentState != LobbyState.Selection_Car) return;

        int nextIndex = (currentIndex + 1) % carPool.Count;
        StartCoroutine(ChangeCarSequence(nextIndex));
    }

    private void InitializeCarPool()
    {
        if (carDatabase == null)
            carDatabase = new List<CarData>();

        GameObject container = new GameObject("SelectCarList");
        container.transform.position = Vector3.zero;

        for (int i = 0; i < carDatabase.Count; i++)
        {
            if (carDatabase[i].carPrefab != null)
            {
                GameObject obj = Instantiate(carDatabase[i].carPrefab, Start_Pos.position, Quaternion.identity);
                
                obj.transform.SetParent(container.transform);


                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;

                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Camera[] childCameras = obj.GetComponentsInChildren<Camera>();
                foreach (var cam in childCameras)
                {
                    cam.enabled = false; 
                }

                AudioListener[] childListeners = obj.GetComponentsInChildren<AudioListener>();
                foreach (var listener in childListeners)
                {
                    listener.enabled = false;
                }

                obj.SetActive(false);
                carPool.Add(obj);
            }
            else
            {
                Debug.LogWarning($"Index {i}의 CarPrefab이 비어있습니다.");
                GameObject emptyObj = new GameObject($"Empty_Car_{i}");
                
                emptyObj.transform.SetParent(container.transform);
                emptyObj.transform.position = Start_Pos.position;
                
                emptyObj.SetActive(false);
                carPool.Add(emptyObj);
            }
        }
    }

    private void ShowFirstCar()
    {
        if (lobbyManager != null && lobbyManager.CurrentState != LobbyState.Selection_Car) return;
        if (carPool.Count == 0) return;

        currentIndex = 0;
        GameObject firstCar = carPool[currentIndex];

        SaveCurrentCarId();

        StartCoroutine(MoveCar(firstCar, Start_Pos.position, Center_Pos.position, true));
    }

    private void CheckStateAndUpdateCars()
    {
        if (lobbyManager == null) return;

        bool isCarSelectionState = lobbyManager.CurrentState == LobbyState.Selection_Car;

        if (isCarSelectionState)
        {
            if (currentCar == null && carPool.Count > 0)
            {
                ShowFirstCar();
            }
        }
        else
        {
            HideAllCars();
        }
    }

    private void HideAllCars()
    {
        if (currentCar != null)
        {
            currentCar.SetActive(false);
            currentCar = null;
        }

        foreach (GameObject car in carPool)
        {
            if (car != null)
            {
                car.SetActive(false);
            }
        }
    }

    private void HandleCenterRotation()
    {
        if (isAnimating || currentCar == null) return;

        float direction = Rotate ? 1.0f : -1.0f;
        currentCar.transform.Rotate(Vector3.up * rotateSpeed * direction * Time.deltaTime, Space.World);
    }

    private IEnumerator ChangeCarSequence(int targetIndex)
    {
        isAnimating = true;

        if (currentCar != null)
        {
            yield return StartCoroutine(MoveCar(currentCar, Center_Pos.position, End_Pos.position, false));

            if (currentCar != null)
            {
                currentCar.SetActive(false);
            }
        }

        currentIndex = targetIndex;
        
        SaveCurrentCarId();

        if (carPool != null && targetIndex >= 0 && targetIndex < carPool.Count)
        {
            GameObject nextCar = carPool[currentIndex];

            if (nextCar != null)
            {
                yield return StartCoroutine(MoveCar(nextCar, Start_Pos.position, Center_Pos.position, true));
            }
        }

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
            currentCar = obj;
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