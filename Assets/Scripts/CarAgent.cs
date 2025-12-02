using UnityEngine;

public class CarAgent : MonoBehaviour
{
    public enum MoveDirection
    {
        NorthToSouth,
        SouthToNorth,
        WestToEast,
        EastToWest
    }

    [Header("Movimiento")]
    public MoveDirection direction;
    public float speed = 3f;

    [Header("Detección de parada")]
    public Rigidbody rb;
    public float stopThreshold = 0.1f;

    [Header("Evitar colisiones")]
    public float safeDistance = 3f;
    public LayerMask obstacleMask;    // capa de los carros

    // La pone StopZone (trigger)
    public bool InStopZone = false;

    // NUEVO: indica que ya cruzó definitivamente su línea de alto
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
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 dir = GetMoveVector();
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }

        // 1) Consultar semáforo
        var controller = QLearningTrafficLightController.Instance;
        QLearningTrafficLightController.LightColor light =
            controller != null ? controller.GetLightForDirection(direction)
                               : QLearningTrafficLightController.LightColor.Green;

        // IMPORTANTE:
        // Solo nos detenemos si:
        // - estamos en la StopZone
        // - el semáforo está en rojo
        // - AÚN NO hemos cruzado esa StopZone
        if (!HasPassedStopZone && InStopZone &&
            light == QLearningTrafficLightController.LightColor.Red)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        // 2) Evitar colisión: raycast al frente
        LayerMask maskToUse = (obstacleMask.value != 0) ? obstacleMask : Physics.AllLayers;

        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                            dir, out hit, safeDistance, maskToUse))
        {
            if (hit.rigidbody == null || hit.rigidbody != rb)
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