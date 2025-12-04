using UnityEngine;

public enum RouteChoice
{
    Straight,
    TurnLeft,
    TurnRight
}
public class CarAgent : MonoBehaviour
{
    public enum MoveDirection
    {
        NorthToSouth,
        SouthToNorth,
        WestToEast,
        EastToWest,
    }

    [Header("Modelo visual")]
    // Arrastra aquí el hijo que tiene la malla del coche
    public Transform visualRoot;

    [Header("Movimiento")]
    public MoveDirection direction;
    public float speed = 3f;

    [Header("Ruta en interseccion")]
    public bool allowTurns = true;
    [Range(0, 1)] public float turnLeftProbability = 0.25f;
    [Range(0, 1)] public float turnRightProbability = 0.25f;

    private RouteChoice route = RouteChoice.Straight;
    private bool hasChosenRoute = false;
    private bool hasTurned = false;


    [Header("Detección de parada")]
    public Rigidbody rb;
    public float stopThreshold = 0.1f;

    [Header("Evitar colisiones")]
    public float safeDistance = 3f;
    public float collisionRadius = 0.2f;
    public LayerMask obstacleMask;

    public bool InStopZone = false;
    public bool HasPassedStopZone = false;

    public bool IsStopped
    {
        get
        {
            if (rb == null) return false;
            return rb.linearVelocity.magnitude < stopThreshold;
        }
    }

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>() ?? GetComponentInChildren<Rigidbody>();

        // Si no asignas nada, usa este mismo objeto
        if (visualRoot == null)
            visualRoot = transform;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 dir = GetMoveVector();

        // Giramos SOLO el modelo visual
        if (dir != Vector3.zero && visualRoot != null)
        {
            visualRoot.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        var controller = QLearningTrafficLightController.Instance;
        var light = controller != null
            ? controller.GetLightForDirection(direction)
            : QLearningTrafficLightController.LightColor.Green;

        if (!HasPassedStopZone && InStopZone &&
            light == QLearningTrafficLightController.LightColor.Red)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        LayerMask maskToUse = (obstacleMask.value != 0) ? obstacleMask : Physics.AllLayers;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        RaycastHit hit;

        if (Physics.SphereCast(origin, collisionRadius,
                               dir, out hit, safeDistance,
                               maskToUse, QueryTriggerInteraction.Ignore))
        {
            var otherCar = hit.collider.GetComponentInParent<CarAgent>();
            if (otherCar != null && otherCar != this)
            {
                rb.linearVelocity = Vector3.zero;
                return;
            }
        }

        rb.linearVelocity = dir.normalized * speed;
    }

    // Llamado por la zona de giro cuando el carro entra a la intersección
    public void DecideAndApplyTurn()
    {
        if (!allowTurns) return;
        if (hasTurned) return;

        // Elegir ruta solo una vez
        if (!hasChosenRoute)
        {
            hasChosenRoute = true;
            float r = Random.value;

            if (r < turnLeftProbability)
                route = RouteChoice.TurnLeft;
            else if (r < turnLeftProbability + turnRightProbability)
                route = RouteChoice.TurnRight;
            else
                route = RouteChoice.Straight;
            // Ver que decision tomo cada coche que colisiona con TurnZone
            Debug.Log($"[CarAgent] {name} entra a TurnZone. Dir original={direction}, ruta elegida={route}");
        }

        // Recto, no hay cambio de dirección
        if (route == RouteChoice.Straight) return;

        // Mapeo explícito por dirección
        switch (direction)
        {
            case MoveDirection.NorthToSouth:
                // Va hacia abajo
                if (route == RouteChoice.TurnRight)
                    direction = MoveDirection.WestToEast;  // gira a su derecha, hacia el oeste
                else
                    direction = MoveDirection.EastToWest;  // gira a su izquierda, hacia el este
                break;

            case MoveDirection.SouthToNorth:
                // Va hacia arriba
                if (route == RouteChoice.TurnRight)
                    direction = MoveDirection.EastToWest;  // derecha: hacia el este
                else
                    direction = MoveDirection.WestToEast;  // izquierda: hacia el oeste (el caso que marcaste)
                break;

            case MoveDirection.EastToWest:
                // Va hacia la izquierda
                if (route == RouteChoice.TurnRight)
                    direction = MoveDirection.SouthToNorth; // derecha: baja
                else
                    direction = MoveDirection.NorthToSouth; // izquierda: sube
                break;

            case MoveDirection.WestToEast:
                // Va hacia la derecha
                if (route == RouteChoice.TurnRight)
                    direction = MoveDirection.NorthToSouth; // derecha: sube
                else
                    direction = MoveDirection.SouthToNorth; // izquierda: baja
                break;
        }

        hasTurned = true;
        Debug.Log($"[CarAgent] {name} nueva dirección={direction}");
    }

    Vector3 GetMoveVector()
    {
        // Norte = +Z, Sur = -Z, Este = +X, Oeste = -X
        switch (direction)
        {
            case MoveDirection.NorthToSouth: return Vector3.back;
            case MoveDirection.SouthToNorth: return Vector3.forward;
            case MoveDirection.WestToEast:   return Vector3.right;
            case MoveDirection.EastToWest:   return Vector3.left;
        }
        return Vector3.zero;
    }
}