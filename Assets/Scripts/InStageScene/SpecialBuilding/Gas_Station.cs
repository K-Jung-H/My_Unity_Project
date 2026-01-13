using UnityEngine;
using System.Collections.Generic;

public class Gas_Station : MonoBehaviour
{
    private Dictionary<Collider, CarController> carCache = new Dictionary<Collider, CarController>();

    [Header("Settings")]
    public float fuelPerSecond = 20f;

    private void OnTriggerEnter(Collider other)
    {
        if (carCache.ContainsKey(other)) return;

        CarController controller = other.GetComponentInParent<CarController>();
        
        if (controller != null)
        {
            carCache.Add(other, controller);

            if (controller.currentGear == GearState.N || controller.currentGear == GearState.P)
            {
                controller.SetFuelCharging(true);
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (carCache.TryGetValue(other, out CarController car))
        {
            bool isCorrectGear = (car.currentGear == GearState.N || car.currentGear == GearState.P);

            if (isCorrectGear)
            {
                car.SetFuelCharging(true);
                car.AddFuel(fuelPerSecond * Time.deltaTime);
            }
            else
            {
                car.SetFuelCharging(false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (carCache.TryGetValue(other, out CarController controller))
        {
            controller.SetFuelCharging(false);
            
            carCache.Remove(other);
        }
    }

}
