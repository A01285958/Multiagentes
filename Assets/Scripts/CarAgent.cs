using UnityEngine;

public class CarAgent : MonoBehaviour
{
    public enum MoveDirection
    {
        NorthToSouth,
        SouthToNorth,
        WestToEast,
        EastToWest,
    }

    [Header("Movimiento")]
    public MoveDirection direction;
    public float speed = 3f;

    [Header("Detección de parada")]
    public Rigidbody rb;
    public float stopThreshold = 0.1f;

    [Header("Evitar colisiones")]
    public float safeDistance = 3f;      // hasta dónde mira al frente
    public float collisionRadius = 0.2f; // radio del “cono” de detección
    public LayerMask obstacleMask;       // capa de carros (opcional)

    // La pone StopZone (trigger)
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
            rb = GetComponent<Rigidbody>();

        // por si el rigidbody está en un hijo del prefab
        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 dir = GetMoveVector();
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // 1) Consultar semáforo (esto ya te funcionaba)
        var controller = QLearningTrafficLightController.Instance;
        QLearningTrafficLightController.LightColor light =
            controller != null ? controller.GetLightForDirection(direction)
                               : QLearningTrafficLightController.LightColor.Green;

        if (!HasPassedStopZone && InStopZone &&
            light == QLearningTrafficLightController.LightColor.Red)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // 2) Evitar colisiones con OTROS CarAgent (no por capa solamente)
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

        // 3) Movimiento normal
        rb.linearVelocity = dir.normalized * speed;
    }

    Vector3 GetMoveVector()
    {
        // Norte = +Z, Sur = -Z, Este = +X, Oeste = -X
        switch (direction)
        {
            case MoveDirection.NorthToSouth:
                return Vector3.back;    // de +Z a -Z
            case MoveDirection.SouthToNorth:
                return Vector3.forward; // de -Z a +Z
            case MoveDirection.WestToEast:
                return Vector3.right;   // de -X a +X
            case MoveDirection.EastToWest:
                return Vector3.left;    // de +X a -X
        }
        return Vector3.zero;
    }
}