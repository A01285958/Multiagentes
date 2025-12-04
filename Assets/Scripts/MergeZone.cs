using UnityEngine;

public class MergeZone : MonoBehaviour
{
    [Header("Punto al que se integran")]
    public Transform mergePoint;

    [Header("Dirección que debe llevar al integrarse")]
    public CarAgent.MoveDirection newDirection = CarAgent.MoveDirection.SouthToNorth;

    private void OnTriggerEnter(Collider other)
    {
        // Buscamos el CarAgent en el objeto o en el padre
        CarAgent car = other.GetComponent<CarAgent>() ?? other.GetComponentInParent<CarAgent>();
        if (car == null) return;

        // Solo queremos mover coches que vienen de la calle lateral.
        // Si quieres limitarlo más, puedes checar car.direction aquí.
        // if (car.direction != CarAgent.MoveDirection.EastToWest) return;

        // Nueva posición en el punto verde
        Vector3 pos = mergePoint.position;
        // Conserva la altura del coche por si tu terreno no está exactamente a la misma Y
        car.transform.position = pos;

        // Actualizamos la dirección para que siga subiendo por la avenida
        car.direction = newDirection;

        // Reinicializa la velocidad para evitar comportamientos raros
        if (car.rb != null)
            car.rb.linearVelocity = Vector3.zero;
    }
}
