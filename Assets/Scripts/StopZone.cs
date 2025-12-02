using UnityEngine;

public class StopZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponent<CarAgent>() ?? other.GetComponentInParent<CarAgent>();
        if (car != null)
        {
            car.InStopZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var car = other.GetComponent<CarAgent>() ?? other.GetComponentInParent<CarAgent>();
        if (car != null)
        {
            car.InStopZone = false;

            // NUEVO: marcamos que ya cruzó definitivamente su línea de alto
            car.HasPassedStopZone = true;
        }
    }
}
