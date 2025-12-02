using UnityEngine;

public class DespawnZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Buscamos el CarAgent en el objeto o en sus padres
        var car = other.GetComponent<CarAgent>() ?? other.GetComponentInParent<CarAgent>();

        if (car != null)
        {
            // Destruye TODO el coche
            Destroy(car.gameObject);
        }
    }
}
