using UnityEngine;
using System.Collections; 

[System.Serializable]
public class ApproachData
{
    public string name;
    // Vehiculos en la hora maxima demanda para esa calle
    public float vehiculosperh = 500f;

    // Probilidades de giro relativa a la calle (escala de 0 a 1)
    [Range(0,1)] public float straightPct = 0.7f;
    [Range(0,1)] public float leftPct = 0.2f;
    [Range(0,1)] public float rightPct = 0.7f;
}
public class TrafficSimulator : MonoBehaviour
{
    [Header("Prefab de carros")]
    public GameObject[] carPrefabs; //bus, ambulancia, coche, etc.


    [Header("Puntos de aparición")]
    public Transform northSpawn; //Arriba (Norte -> Sur)
    public Transform southSpawn; //Abajo (Sur -> Norte)
    public Transform eastSpawn; //Derecha (Este -> Oeste)
    public Transform westSpawn; //Izquierda (Oeste -> Este)

    [Header("Datos de demanda por calle")]
    public ApproachData northData;
    public ApproachData southData;
    public ApproachData eastData;
    public ApproachData westData;

    [Header("Duracion de la simulacion en horas de reloj")]
    public float simulatedHours = 3f;


    [Header("Parametros de spawn")]
    public float spawnRate = 0.4f; //Coches por segund aprox.
    public LayerMask carLayer; //Capa carro para que no aparezca encima de otro
    public float spawnCheckRadius = 2f;

    void Start()
    {
        // Arrancamos una corrutina por cada aproximación
        if (northSpawn != null && northData != null)
            StartCoroutine(SpawnFromApproach(northSpawn, CarAgent.MoveDirection.NorthToSouth, northData));

        if (southSpawn != null && southData != null)
            StartCoroutine(SpawnFromApproach(southSpawn, CarAgent.MoveDirection.SouthToNorth, southData));

        if (eastSpawn != null && eastData != null)
            StartCoroutine(SpawnFromApproach(eastSpawn, CarAgent.MoveDirection.EastToWest, eastData));

        if (westSpawn != null && westData != null)
            StartCoroutine(SpawnFromApproach(westSpawn, CarAgent.MoveDirection.WestToEast, westData));
    }

    IEnumerator SpawnFromApproach(
        Transform spawnPoint,
        CarAgent.MoveDirection dir,
        ApproachData data)
    {
        if (spawnPoint == null || data == null)
            yield break;

        float vehiclesPerSecond = data.vehiculosperh / (simulatedHours * 3600f);
        float timeScaleFactor = 60f;      // 1 s juego = 1 min real
        float effectiveRate = vehiclesPerSecond * timeScaleFactor;

        if (effectiveRate <= 0f)
            effectiveRate = 0.1f;

        while (true)
        {
            // tiempo hasta la siguiente llegada
            float wait = -Mathf.Log(Random.value) / effectiveRate;
            yield return new WaitForSeconds(wait);

            // ahora esperamos hasta que haya hueco en la entrada
            yield return StartCoroutine(SpawnCarWhenFree(spawnPoint, dir, data));
        }
    }

    // Esta corrutina se queda "checando" hasta que el radio esté libre
    IEnumerator SpawnCarWhenFree(Transform spawnPoint, CarAgent.MoveDirection dir, ApproachData data)
    {
        // posición donde checamos (un poco adelante del spawn)
        Vector3 checkPos = spawnPoint.position + spawnPoint.forward * 1f;

        while (true)
        {
            bool occupied = Physics.CheckSphere(
                checkPos,
                spawnCheckRadius,
                carLayer,
                QueryTriggerInteraction.Ignore
            );

            if (!occupied)
            {
                SpawnCar(spawnPoint, dir, data);
                yield break;   // listo, salimos de esta corrutina
            }

            // si está ocupado, esperamos un ratito y volvemos a checar
            yield return new WaitForSeconds(0.2f);
        }
    }

    void SpawnCar(Transform spawnPoint,
                  CarAgent.MoveDirection dir,
                  ApproachData data)
    {
        if (carPrefabs == null || carPrefabs.Length == 0) return;

        var prefab = carPrefabs[Random.Range(0, carPrefabs.Length)];
        var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        var agent = go.GetComponent<CarAgent>();
        if (agent != null)
        {
            agent.direction = dir;

            // estos valores vienen del estudio de volúmenes
            agent.allowTurns = true;
            agent.turnLeftProbability  = data.leftPct;
            agent.turnRightProbability = data.rightPct;
            // lo que falte será ir derecho
        }
    }
    void OnDrawGizmosSelected()
    {
         // para ver en la escena la zona donde se checa si ya hay carro
        Gizmos.color = Color.cyan;
        if (northSpawn != null) Gizmos.DrawWireSphere(northSpawn.position, spawnCheckRadius);
        if (southSpawn != null) Gizmos.DrawWireSphere(southSpawn.position, spawnCheckRadius);
        if (eastSpawn  != null) Gizmos.DrawWireSphere(eastSpawn.position,  spawnCheckRadius);
        if (westSpawn  != null) Gizmos.DrawWireSphere(westSpawn.position,  spawnCheckRadius);
    }
}
