using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car_Animation : MonoBehaviour
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

    void Start()
    {
        InitializeCarPool();

        if (carPool.Count > 0)
        {
            ShowFirstCar();
        }
    }

    void Update()
    {
        HandleCenterRotation();
    }

    public void Change_Rotate()
    {
        Rotate = !Rotate;
    }

    public void Change_Car_Prev()
    {
        if (isAnimating || carPool.Count == 0) return;

        int prevIndex = (currentIndex - 1 + carPool.Count) % carPool.Count;
        StartCoroutine(ChangeCarSequence(prevIndex));
    }

    public void Change_Car_Next()
    {
        if (isAnimating || carPool.Count == 0) return;

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
        currentIndex = 0;
        GameObject firstCar = carPool[currentIndex];

        StartCoroutine(MoveCar(firstCar, Start_Pos.position, Center_Pos.position, true));
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
            currentCar.SetActive(false);
        }

        currentIndex = targetIndex;
        GameObject nextCar = carPool[currentIndex];

        yield return StartCoroutine(MoveCar(nextCar, Start_Pos.position, Center_Pos.position, true));

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