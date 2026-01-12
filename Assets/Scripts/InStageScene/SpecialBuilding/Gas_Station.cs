using UnityEngine;

public class Gas_Station : MonoBehaviour
{

    [Header("Settings")]
    public float fuelPerSecond = 20f;

    void OnTriggerStay(Collider other)
    {
        CarController car = other.GetComponentInParent<CarController>();
        if (car != null)
        {
            car.AddFuel(fuelPerSecond * Time.deltaTime);
        }
    }
}
