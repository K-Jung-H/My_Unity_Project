using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lobby_CarSelectManager : MonoBehaviour
{
    [Header("Car Prefabs")]
    public GameObject[] Car_Objects;

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
        for (int i = 0; i < Car_Objects.Length; i++)
        {
            GameObject obj = Instantiate(Car_Objects[i], Start_Pos.position, Quaternion.identity);
            obj.SetActive(false);
            carPool.Add(obj);
        }
    }

    private void ShowFirstCar()
    {
        if (lobbyManager != null && lobbyManager.CurrentState != LobbyState.Selection_Car) return;

        currentIndex = 0;
        GameObject firstCar = carPool[currentIndex];

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
}