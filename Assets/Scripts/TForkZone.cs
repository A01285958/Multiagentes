using UnityEngine;

public class TForkZone : MonoBehaviour
{
    [Header("Probabilidades")]
    [Range(0,1)] public float leftProbability = 0.5f;

    [Header("Configuración de direcciones")]
    // Dirección con la que ENTRAN a esta intersección
    public CarAgent.MoveDirection incomingDirection;

    // Dirección que debe llevar si gira a la izquierda / derecha
    public CarAgent.MoveDirection leftDirection;
    public CarAgent.MoveDirection rightDirection;

    [Header("Puntos donde debe caer el coche después del giro")]
    public Transform leftExit;
    public Transform rightExit;

    private void OnTriggerEnter(Collider other)
    {
        var car = other.GetComponentInParent<CarAgent>();
        if (car == null) return;

        // Solo actuamos sobre los coches que vienen en la dirección esperada
        if (car.direction != incomingDirection) return;

        bool goLeft = Random.value < leftProbability;

        if (goLeft)
        {
            if (leftExit != null)
                car.transform.position = leftExit.position;

            car.direction = leftDirection;
        }
        else
        {
            if (rightExit != null)
                car.transform.position = rightExit.position;

            car.direction = rightDirection;
        }
    }
}
