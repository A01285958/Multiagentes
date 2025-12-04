using UnityEngine;

public class TurnZone : MonoBehaviour
{
    [Header("Salidas desde NorthToSouth (entra por el norte, baja)")]
    public Transform N_straightExit;
    public Transform N_leftExit;
    public Transform N_rightExit;

    [Header("Salidas desde SouthToNorth (entra por el sur, sube)")]
    public Transform S_straightExit;
    public Transform S_leftExit;
    public Transform S_rightExit;

    [Header("Salidas desde WestToEast (entra por el oeste, va a la derecha)")]
    public Transform W_straightExit;
    public Transform W_leftExit;
    public Transform W_rightExit;

    [Header("Salidas desde EastToWest (entra por el este, va a la izquierda)")]
    public Transform E_straightExit;
    public Transform E_leftExit;
    public Transform E_rightExit;

    private void Reset()
    {
        // Asegura que el collider sea trigger
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // El collider puede estar en un hijo del coche
        CarAgent car = other.GetComponent<CarAgent>();
        if (car == null)
            car = other.GetComponentInParent<CarAgent>();

        if (car == null) return;

        // Dirección antes del giro
        var oldDir = car.direction;

        // Deja que el CarAgent decida izquierda / derecha / recto
        car.DecideAndApplyTurn();

        // Dirección después del giro
        var newDir = car.direction;

        // Elegimos el Empty correcto según de dónde viene y hacia dónde va
        Transform exitPoint = GetExitFor(oldDir, newDir);

        Debug.Log($"[TurnZone] {car.name} entró. oldDir={oldDir}, newDir={newDir}");

        if (exitPoint != null)
        {
            // Encarrilar: solo movemos el eje del carril
            Vector3 pos = car.transform.position;

            switch (newDir)
            {
                case CarAgent.MoveDirection.NorthToSouth:
                case CarAgent.MoveDirection.SouthToNorth:
                    // Carril vertical: alineamos X
                    pos.x = exitPoint.position.x;
                    break;

                case CarAgent.MoveDirection.EastToWest:
                case CarAgent.MoveDirection.WestToEast:
                    // Carril horizontal: alineamos Z
                    pos.z = exitPoint.position.z;
                    break;
            }

            car.transform.position = pos;
        }
    }

    Transform GetExitFor(CarAgent.MoveDirection oldDir, CarAgent.MoveDirection newDir)
    {
        switch (oldDir)
        {
            case CarAgent.MoveDirection.NorthToSouth:
                if (newDir == CarAgent.MoveDirection.NorthToSouth) return N_straightExit;
                if (newDir == CarAgent.MoveDirection.WestToEast)   return N_rightExit;
                if (newDir == CarAgent.MoveDirection.EastToWest)   return N_leftExit;
                break;

            case CarAgent.MoveDirection.SouthToNorth:
                if (newDir == CarAgent.MoveDirection.SouthToNorth) return S_straightExit;
                if (newDir == CarAgent.MoveDirection.EastToWest)   return S_rightExit;
                if (newDir == CarAgent.MoveDirection.WestToEast)   return S_leftExit;
                break;

            case CarAgent.MoveDirection.WestToEast:
                if (newDir == CarAgent.MoveDirection.WestToEast)   return W_straightExit;
                if (newDir == CarAgent.MoveDirection.SouthToNorth) return W_rightExit;
                if (newDir == CarAgent.MoveDirection.NorthToSouth) return W_leftExit;
                break;

            case CarAgent.MoveDirection.EastToWest:
                if (newDir == CarAgent.MoveDirection.EastToWest)   return E_straightExit;
                if (newDir == CarAgent.MoveDirection.NorthToSouth) return E_rightExit;
                if (newDir == CarAgent.MoveDirection.SouthToNorth) return E_leftExit;
                break;
        }

        // Por si falta algo en el inspector, devolvemos null
        return null;
    }
}